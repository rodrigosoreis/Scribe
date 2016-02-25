﻿#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

#endregion

namespace Scribe
{
	public static class Extensions
	{
		#region Methods

		public static string CleanMessage(this Exception ex)
		{
			var offset = ex.Message.IndexOf("\r\nParameter");
			return offset > 0 ? ex.Message.Substring(0, offset) : ex.Message;
		}

		/// <summary>
		/// Converts short unit to the long unit name.
		/// </summary>
		/// <param name="unit"> The unit to convert. </param>
		/// <param name="plural"> The flag to pluralize or not. </param>
		/// <returns> The long unit name for the short unit. </returns>
		public static string ConvertShortUnitToLongUnit(string unit, bool plural)
		{
			var data = new Dictionary<string, string>();
			data.Add("y", "year");
			data.Add("M", "month");
			data.Add("d", "day");
			data.Add("h", "hour");
			data.Add("m", "minute");
			data.Add("s", "second");

			if (data.ContainsKey(unit))
			{
				return plural ? data[unit] + "s" : data[unit];
			}

			return string.Empty;
		}

		/// <summary>
		/// Converts the string to an int. If it cannot be parse it will return the default value.
		/// </summary>
		/// <param name="input"> The string to convert. </param>
		/// <param name="defaultValue"> The default value to return. Defaults to Guid.Empty. </param>
		/// <returns> The int value or the default value. </returns>
		public static Guid ConvertToGuid(this string input, Guid? defaultValue = null)
		{
			Guid response;
			return !Guid.TryParse(input, out response) ? defaultValue ?? Guid.Empty : response;
		}

		/// <summary>
		/// Converts the string to an int. If it cannot be parse it will return the default value.
		/// </summary>
		/// <param name="input"> The string to convert. </param>
		/// <param name="defaultValue"> The default value to return. Defaults to 0. </param>
		/// <returns> The int value or the default value. </returns>
		public static int ConvertToInt(this string input, int defaultValue = 0)
		{
			int response;
			return !int.TryParse(input, out response) ? defaultValue : response;
		}

		/// <summary>
		/// Loop through collection and run action on each item.
		/// </summary>
		/// <typeparam name="T"> The type of the item. </typeparam>
		/// <param name="collection"> The collection to enumerate. </param>
		/// <param name="action"> The action to run on each item. </param>
		public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
		{
			foreach (var item in collection)
			{
				action(item);
			}
		}

		/// <summary>
		/// Deserialize an object from JSON.
		/// </summary>
		/// <typeparam name="T"> The type to be deserialized. </typeparam>
		/// <param name="item"> The JSON data. </param>
		/// <param name="camelCase"> The flag to determine if we should use camel case or not. </param>
		/// <returns> The deserialized object from the provided JSON. </returns>
		public static T FromJson<T>(this string item, bool camelCase = false)
		{
			return JsonConvert.DeserializeObject<T>(item, GetSerializerSettings(camelCase));
		}

		public static string GetDisplayName(this IIdentity identity)
		{
			return !identity.Name.Contains(';') ? string.Empty : identity.Name.Split(';').Last();
		}

		public static int GetId(this IIdentity identity)
		{
			return !identity.Name.Contains(';') ? 0 : identity.Name.Split(';').First().ConvertToInt();
		}

		public static JsonSerializerSettings GetSerializerSettings(bool camelCase = true)
		{
			var response = new JsonSerializerSettings();
			response.Converters.Add(new IsoDateTimeConverter());
			response.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
			response.PreserveReferencesHandling = PreserveReferencesHandling.Objects;

			if (camelCase)
			{
				response.Converters.Add(new StringEnumConverter { CamelCaseText = true });
				response.ContractResolver = new CamelCasePropertyNamesContractResolver();
			}

			return response;
		}

		public static byte[] ReadEmbeddedBinaryFile(this Assembly assembly, string path)
		{
			using (var stream = assembly.GetManifestResourceStream(path))
			{
				if (stream == null)
				{
					throw new Exception("Embedded file not found.");
				}

				var data = new byte[stream.Length];
				stream.Read(data, 0, data.Length);
				return data;
			}
		}

		public static string ReadEmbeddedFile(this Assembly assembly, string path)
		{
			using (var stream = assembly.GetManifestResourceStream(path))
			{
				if (stream == null)
				{
					throw new Exception("Embedded file not found.");
				}

				using (var reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
		}

		public static string ToDetailedString(this Exception ex)
		{
			var builder = new StringBuilder();
			AddExceptionToBuilder(builder, ex);
			return builder.ToString();
		}

		/// <summary>
		/// Converts the item to JSON.
		/// </summary>
		/// <typeparam name="T"> The type of the item to convert. </typeparam>
		/// <param name="item"> The item to convert. </param>
		/// <param name="camelCase"> The flag to determine if we should use camel case or not. </param>
		/// <param name="indented"> The flag to determine if the JSON should be indented or not. </param>
		/// <returns> The JSON value of the item. </returns>
		public static string ToJson<T>(this T item, bool camelCase = true, bool indented = false)
		{
			return JsonConvert.SerializeObject(item, indented ? Formatting.Indented : Formatting.None, GetSerializerSettings(camelCase));
		}

		/// <summary>
		/// Formats the time span into a human readable format.
		/// </summary>
		/// <param name="time"> The time span to convert. </param>
		/// <param name="limited"> The flag to limit to only a single value. </param>
		/// <param name="format"> The format to use to generate the string. </param>
		/// <returns> A human readable format of the time span. </returns>
		public static string ToTimeAgo(this TimeSpan time, bool limited = true, string format = "yMdhms")
		{
			var thresholds = new SortedList<long, string>();
			var secondsPerMinute = 60;
			var secondsPerHour = 60 * secondsPerMinute;
			var secondsPerDay = 24 * secondsPerHour;

			thresholds.Add(secondsPerDay * 365, "y");
			thresholds.Add(secondsPerDay * 30, "M");
			thresholds.Add(secondsPerDay, "d");
			thresholds.Add(secondsPerHour, "h");
			thresholds.Add(secondsPerMinute, "m");
			thresholds.Add(1, "s");

			var builder = new StringBuilder();
			var secondsRemaining = time.TotalSeconds;
			var thresholdsHit = 0;

			for (var i = thresholds.Keys.Count - 1; i >= 0 && thresholdsHit < 2; i--)
			{
				var threshold = thresholds.Keys[i];
				var unit = thresholds[threshold];
				if (!(secondsRemaining >= threshold))
				{
					continue;
				}

				if (!format.Contains(unit))
				{
					continue;
				}

				var count = (int) (secondsRemaining / threshold);
				secondsRemaining %= threshold;

				var unitText = ConvertShortUnitToLongUnit(unit, count > 1);
				builder.AppendFormat(", {0} {1}", count, unitText);
				thresholdsHit++;

				if (limited)
				{
					break;
				}
			}

			if (builder.Length <= 0)
			{
				var firstUnit = format.Last().ToString();
				return "less than a " + ConvertShortUnitToLongUnit(firstUnit, false);
			}

			var response = builder.Remove(0, 2).ToString();
			var lastIndex = response.LastIndexOf(", ");

			if (lastIndex > 0)
			{
				response = response.Remove(lastIndex, 2);
				response = response.Insert(lastIndex, " and ");
			}

			return response;
		}

		private static void AddExceptionToBuilder(StringBuilder builder, Exception ex)
		{
			builder.Append(builder.Length > 0 ? "\r\n" + ex.Message : ex.Message);

			if (ex.InnerException != null)
			{
				AddExceptionToBuilder(builder, ex.InnerException);
			}
		}

		#endregion
	}
}