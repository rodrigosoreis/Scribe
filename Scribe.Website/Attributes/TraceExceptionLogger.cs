﻿#region References

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Web.Http.ExceptionHandling;

#endregion

namespace Scribe.Website.Attributes
{
	[ExcludeFromCodeCoverage]
	public class TraceExceptionLogger : ExceptionLogger
	{
		#region Methods

		public override void Log(ExceptionLoggerContext database)
		{
			Trace.TraceError(database.ExceptionContext.Exception.ToString());
		}

		#endregion
	}
}