﻿#region References

using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scribe.Models.Enumerations;
using Scribe.Models.Views;
using Scribe.Web;
using TestR.Helpers;
using TestR.PowerShell;
using HttpClient = Scribe.Web.HttpClient;

#endregion

namespace Scribe.IntegrationTests
{
	[TestClass]
	public class PageTests : BaseTests
	{
		#region Constants

		private const string TestSite = "http://localhost";

		#endregion

		#region Methods

		[TestMethod]
		public void AddPage()
		{
			ForEachBrowser(browser =>
			{
				using (var database = TestHelper.GetDatabase())
				{
					browser.NavigateTo($"{TestSite}");

					var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
					TestHelper.AddDefaultSettings(database);
					TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
					database.SaveChanges();
				}

				HttpClient.Post(TestSite, "api/Settings/Reload").Dispose();

				browser.NavigateTo($"{TestSite}/Login");
				browser.Elements.TextInputs["userName"].Text = "John Doe";
				browser.Elements.TextInputs["password"].Text = "Password!";
				browser.Elements.Buttons["submit"].Click();
				browser.WaitForNavigation();

				browser.NavigateTo($"{TestSite}/NewPage");
				browser.Elements.TextInputs["pageTitle"].Text = "My Page";
				browser.Elements.TextArea["pageText"].Text = "The quick brown fox jumped over the lazy dog's back.";
				browser.Elements.TextInputs["addTag"].Text = "New Tag";
				browser.Elements.Links["addTagButton"].Click();

				Utility.Wait(() => browser.Elements.Buttons["saveButton"]["disabled"] == "false");

				browser.Elements.Buttons["saveButton"].Click();
				browser.WaitForNavigation();

				Assert.AreEqual($"{TestSite}/Page/1/MyPage", browser.Uri);
				Assert.AreEqual("My Page", browser.Elements["pageTitle"].Text);
			});
		}

		[TestMethod]
		public void EditPage()
		{
			ForEachBrowser(browser =>
			{
				using (var database = TestHelper.GetDatabase())
				{
					browser.NavigateTo($"{TestSite}");

					var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator");
					TestHelper.AddDefaultSettings(database);
					var john = TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
					TestHelper.AddPage(database, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
					database.SaveChanges();
				}

				HttpClient.Post(TestSite, "api/Settings/Reload").Dispose();

				browser.NavigateTo($"{TestSite}/Login");
				browser.Elements.TextInputs["userName"].Text = "John Doe";
				browser.Elements.TextInputs["password"].Text = "Password!";
				browser.Elements.Buttons["submit"].Click();
				browser.WaitForNavigation();

				browser.NavigateTo($"{TestSite}/EditPage/1/ExistingPage");
				browser.Elements.TextInputs["pageTitle"].Text = "My Welcome Page";
				browser.Elements.TextArea["pageText"].Text = "The quick brown fox jumped over the lazy dog's back.";
				browser.Elements.Buttons["saveButton"].Click();
				browser.WaitForNavigation();
			});
		}

		[TestMethod]
		public void PagesForGuest()
		{
			ForEachBrowser(browser =>
			{
				using (var database = TestHelper.GetDatabase())
				{
					browser.NavigateTo($"{TestSite}");

					var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
					TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = true });
					TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
					TestHelper.AddPage(database, "Hello Page", "Hello Internal World", user, ApprovalStatus.None, false, "myTag");
					TestHelper.AddPage(database, "Public Page", "Hello World", user, ApprovalStatus.Approved, true, "myTag");
					database.SaveChanges();
				}

				HttpClient.Post(TestSite, "api/Settings/Reload").Dispose();

				browser.NavigateTo($"{TestSite}/Pages");

				Assert.AreEqual($"{TestSite}/Pages", browser.Uri);
				Assert.AreEqual(1, browser.Elements.TableBodies["pagesBody"].Children.Count);
				Assert.AreEqual("Public Page", browser.Elements.TableBodies["pagesBody"].Children[0].Children[0].Text.Trim());
			});
		}

		[TestMethod]
		public void PagesForGuestWithoutApproval()
		{
			ForEachBrowser(browser =>
			{
				using (var database = TestHelper.GetDatabase())
				{
					browser.NavigateTo($"{TestSite}");

					var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
					TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = false });
					TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
					TestHelper.AddPage(database, "Hello Page", "Hello Internal World", user, ApprovalStatus.None, false, "myTag");
					TestHelper.AddPage(database, "Public Page", "Hello World", user, ApprovalStatus.Approved, true, "myTag");
					database.SaveChanges();
				}

				HttpClient.Post(TestSite, "api/Settings/Reload").Dispose();

				browser.NavigateTo($"{TestSite}/Pages");

				Assert.AreEqual($"{TestSite}/Pages", browser.Uri);
				Assert.AreEqual(2, browser.Elements.TableBodies["pagesBody"].Children.Count);
				Assert.AreEqual("Hello Page", browser.Elements.TableBodies["pagesBody"].Children[0].Children[0].Text.Trim());
				Assert.AreEqual("Public Page", browser.Elements.TableBodies["pagesBody"].Children[1].Children[0].Text.Trim());
			});
		}

		[TestMethod]
		public void PagesForUser()
		{
			ForEachBrowser(browser =>
			{
				using (var database = TestHelper.GetDatabase())
				{
					browser.NavigateTo($"{TestSite}");

					var user = TestHelper.AddUser(database, "Administrator", "Password!", "administrator", "approver", "publisher");
					TestHelper.AddSettings(database, new SettingsView { EnableGuestMode = true });
					TestHelper.AddUser(database, "John Doe", "john.doe@test.com", "Password!");
					TestHelper.AddPage(database, "Hello Page", "Hello Internal World", user, ApprovalStatus.None, false, "myTag");
					TestHelper.AddPage(database, "Public Page", "Hello World", user, ApprovalStatus.Approved, true, "myTag");
					database.SaveChanges();
				}

				HttpClient.Post(TestSite, "api/Settings/Reload").Dispose();

				browser.NavigateTo($"{TestSite}/Login");
				browser.Elements.TextInputs["userName"].Text = "John Doe";
				browser.Elements.TextInputs["password"].Text = "Password!";
				browser.Elements.Buttons["submit"].Click();
				browser.WaitForNavigation();

				browser.NavigateTo($"{TestSite}/Pages");

				Assert.AreEqual($"{TestSite}/Pages", browser.Uri);
				Assert.AreEqual(2, browser.Elements.TableBodies["pagesBody"].Children.Count);
				Assert.AreEqual("Hello Page", browser.Elements.TableBodies["pagesBody"].Children[0].Children[0].Text.Trim());
				Assert.AreEqual("Public Page", browser.Elements.TableBodies["pagesBody"].Children[1].Children[0].Text.Trim());
			});
		}

		#endregion
	}
}