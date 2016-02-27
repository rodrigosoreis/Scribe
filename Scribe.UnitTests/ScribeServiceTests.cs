﻿#region References

using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scribe.Models.Data;
using Scribe.Models.Entities;
using Scribe.Models.Enumerations;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.Website.Services;

#endregion

namespace Scribe.UnitTests
{
	[TestClass]
	public class ScribeServiceTests
	{
		#region Methods

		[TestMethod]
		public void BeginEditingPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, john);
				var actual = service.BeginEditingPage(page.Id);
				var format = "MM/dd/yyyy hh:mm:ss";

				Assert.AreEqual(page.Title, actual.Title);
				Assert.AreEqual("John Doe", actual.EditingBy);
				Assert.AreEqual("less than a second", actual.LastModified);
				Assert.AreEqual(DateTime.UtcNow.ToString(format), page.EditingOn.ToString(format));
			}
		}

		[TestMethod]
		public void DeleteFile()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = false });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var file1 = TestHelper.AddFile(context, john, "File1.png", "image/png", new byte[0]);
				var file2 = TestHelper.AddFile(context, john, "File2.png", "image/png", new byte[0]);
				context.SaveChanges();

				var service = new ScribeService(context, null, null, john);
				service.DeleteFile(file1.Id);
				var actual = service.GetFiles().Results.ToList();

				Assert.AreEqual(1, actual.Count);
				TestHelper.AreEqual(file2.ToView(), actual[0]);
			}
		}

		[TestMethod]
		public void DeleteFileWithInvalidId()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = false });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, john);
				TestHelper.ExpectedException<Exception>(() => { service.DeleteFile(int.MaxValue); }, "Failed to find the file with the provided ID.");
			}
		}

		[TestMethod]
		public void DeleteFileWithSoftDelete()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var file1 = TestHelper.AddFile(context, john, "File1.png", "image/png", new byte[0]);
				var file2 = TestHelper.AddFile(context, john, "File2.png", "image/png", new byte[0]);
				context.SaveChanges();

				var service = new ScribeService(context, null, null, john);
				service.DeleteFile(file1.Id);
				var actual = service.GetFiles();

				Assert.AreEqual(1, actual.Results.Count());
				TestHelper.AreEqual(file2.ToView(), actual.Results.First());
				Assert.AreEqual(2, context.Files.Count());
				Assert.AreEqual(1, context.Files.Count(x => x.IsDeleted));
			}
		}

		[TestMethod]
		public void DeletePage()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = false });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				context.SaveChanges();

				Assert.AreEqual(0, context.Pages.Count(x => x.IsDeleted));
				var service = new ScribeService(context, null, null, john);
				service.DeletePage(page.Id);
			}

			using (var context = provider.GetContext(false))
			{
				Assert.AreEqual(0, context.Pages.Count(x => x.IsDeleted));
			}
		}

		[TestMethod]
		public void DeletePageWithInvalidId()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = false });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, john);
				TestHelper.ExpectedException<Exception>(() => { service.DeletePage(int.MaxValue); }, "Failed to find the page with the provided ID.");
			}
		}

		[TestMethod]
		public void DeletePageWithSoftDelete()
		{
			var provider = TestHelper.GetContextProvider();
			Page page;

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				context.SaveChanges();

				Assert.AreEqual(0, context.Pages.Count(x => x.IsDeleted));
				var service = new ScribeService(context, null, null, john);
				service.DeletePage(page.Id);
			}

			using (var context = provider.GetContext(false))
			{
				Assert.AreEqual(1, context.Pages.Count(x => x.IsDeleted));
				Assert.AreEqual(page.Id, context.Pages.First(x => x.IsDeleted).Id);
			}
		}

		[TestMethod]
		public void DeleteTag()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "Page1", "Hello World", john, ApprovalStatus.None, false, "Tag1", "Tag2", "Tag3");
				TestHelper.AddPage(context, "Page2", "Hello World", john, ApprovalStatus.None, false, "Tag1", "Tag2");
				context.SaveChanges();

				var path = Path.GetTempPath() + "ScribeTests";
				var searchService = new SearchService(context, path, john);
				var service = new ScribeService(context, null, searchService, john);
				service.DeleteTag("Tag2");
			}

			using (var context = provider.GetContext(false))
			{
				var actual = context.Pages.ToList();
				Assert.AreEqual(2, actual.Count);
				Assert.AreEqual(",Tag1,Tag3,", actual[0].Tags);
				Assert.AreEqual(",Tag1,", actual[1].Tags);
			}
		}

		[TestMethod]
		public void DeleteTagEmpty()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var path = Path.GetTempPath() + "ScribeTests";
				var searchService = new SearchService(context, path, null);
				var service = new ScribeService(context, null, searchService, john);
				TestHelper.ExpectedException<Exception>(() => service.DeleteTag(string.Empty), "The tag name must be provided.");
			}
		}

		[TestMethod]
		public void GetFileById()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var file1 = TestHelper.AddFile(context, john, "File1.png", "image/png", new byte[0]);
				TestHelper.AddFile(context, john, "File2.png", "image/png", new byte[0]);
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetFile(file1.Id);

				TestHelper.AreEqual(file1.ToView(), actual);
			}
		}

		[TestMethod]
		public void GetFileByName()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var file1 = TestHelper.AddFile(context, john, "File1.png", "image/png", new byte[0]);
				TestHelper.AddFile(context, john, "File2.png", "image/png", new byte[0]);
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetFile(file1.Name);

				TestHelper.AreEqual(file1.ToView(), actual);
			}
		}

		[TestMethod]
		public void GetFiles()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var file1 = TestHelper.AddFile(context, john, "File1.png", "image/png", new byte[0]);
				var file2 = TestHelper.AddFile(context, john, "File2.png", "image/png", new byte[0]);
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetFiles();

				Assert.AreEqual(2, actual.Results.Count());
				TestHelper.AreEqual(file1.ToView(), actual.Results.First());
				TestHelper.AreEqual(file2.ToView(), actual.Results.Last());
			}
		}

		[TestMethod]
		public void GetFrontPageNonPublic()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { EnablePageApproval = false });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page1 = TestHelper.AddPage(context, "Front Page1", "Hello World1", john, ApprovalStatus.None, false, "homepage");
				TestHelper.AddPage(context, "Front Page2", "Hello World2", john, ApprovalStatus.None, false, "homepage", "public");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetFrontPage();

				Assert.AreEqual(page1.Title, actual.Title);
			}
		}

		[TestMethod]
		public void GetFrontPagePublic()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				TestHelper.AddSettings(context, user, new SettingsView { EnablePageApproval = true });
				TestHelper.AddPage(context, "Front Page1", "Hello World1", user, ApprovalStatus.None, false, "homepage");
				var page2 = TestHelper.AddPage(context, "Front Page2", "Hello World2", user, ApprovalStatus.Approved, true, "homepage");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetFrontPage();

				Assert.AreEqual(page2.Title, actual.Title);
			}
		}

		[TestMethod]
		public void GetPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPage(page.Id);

				Assert.AreEqual(page.Title, actual.Title);
			}
		}

		[TestMethod]
		public void GetPageInvalidId()
		{
			using (var context = TestHelper.GetContext())
			{
				var service = new ScribeService(context, null, null, null);
				TestHelper.ExpectedException<Exception>(() => service.GetPage(int.MaxValue), "Failed to find the page with that ID.");
			}
		}

		[TestMethod]
		public void GetPages()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages();

				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual(page.Title, actual.Results.First().Title);
			}
		}

		[TestMethod]
		public void GetPagesFilterUsingStatusOfApproved()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "Page1", "Hello World", user, ApprovalStatus.None, false, "Tag1", "Tag2", "Tag3");
				TestHelper.AddPage(context, "Page2", "Hello World", user, ApprovalStatus.Pending, false, "Tag1", "Tag2");
				TestHelper.AddPage(context, "Page3", "Hello World", user, ApprovalStatus.Approved, false, "Tag1", "Tag3");
				context.SaveChanges();

				var path = Path.GetTempPath() + "ScribeTests";
				var searchService = new SearchService(context, path, john);
				var service = new ScribeService(context, null, searchService, john);
				var actual = service.GetPages(new PagedRequest("Status=Approved"));

				Assert.AreEqual("Status=Approved", actual.Filter);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("Page3", actual.Results.First().Title);
			}
		}

		[TestMethod]
		public void GetPagesFilterUsingStatusOfPending()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "Page1", "Hello World", user, ApprovalStatus.None, false, "Tag1", "Tag2", "Tag3");
				TestHelper.AddPage(context, "Page2", "Hello World", user, ApprovalStatus.Pending, false, "Tag1", "Tag2");
				TestHelper.AddPage(context, "Page3", "Hello World", user, ApprovalStatus.Approved, false, "Tag1", "Tag3");
				context.SaveChanges();

				var path = Path.GetTempPath() + "ScribeTests";
				var searchService = new SearchService(context, path, john);
				var service = new ScribeService(context, null, searchService, john);
				var actual = service.GetPages(new PagedRequest("Status=Pending"));

				Assert.AreEqual("Status=Pending", actual.Filter);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("Page2", actual.Results.First().Title);
			}
		}

		[TestMethod]
		public void GetPagesFilterUsingTag()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "Page1", "Hello World", john, ApprovalStatus.None, false, "Tag1", "Tag2", "Tag3");
				TestHelper.AddPage(context, "Page2", "Hello World", john, ApprovalStatus.None, false, "Tag1", "Tag2");
				TestHelper.AddPage(context, "Page3", "Hello World", john, ApprovalStatus.None, false, "Tag1", "Tag3");
				context.SaveChanges();

				var path = Path.GetTempPath() + "ScribeTests";
				var searchService = new SearchService(context, path, john);
				var service = new ScribeService(context, null, searchService, john);
				var actual = service.GetPages(new PagedRequest("Tags=Tag3"));

				Assert.AreEqual("Tags=Tag3", actual.Filter);
				Assert.AreEqual(2, actual.Results.Count());
				Assert.AreEqual("Page1", actual.Results.First().Title);
				Assert.AreEqual("Page3", actual.Results.Last().Title);
			}
		}

		[TestMethod]
		public void GetPagesShouldNotReturnDeletedPages()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				page.IsDeleted = true;
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages();

				Assert.AreEqual(1, context.Pages.Count());
				Assert.AreEqual(1, context.Pages.Count(x => x.IsDeleted));
				Assert.AreEqual(0, actual.Results.Count());
			}
		}

		[TestMethod]
		public void GetPagesShouldOnlyReturnPublishedPages()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator", "approver", "publisher");
				var settings = new SettingsView { EnablePageApproval = true, LdapConnectionString = string.Empty };
				TestHelper.AddSettings(context, user, settings);
				TestHelper.AddPage(context, "Hello Page", "Hello World", user, ApprovalStatus.None, false, "myTag");
				TestHelper.AddPage(context, "Public Page", "Hello Real World", user, ApprovalStatus.Approved, true, "myTag");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages();

				Assert.AreEqual(2, context.Pages.Count());
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("Public Page", actual.Results.First().Title);
			}
		}

		[TestMethod]
		public void GetPagesWithFilter()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					TestHelper.AddPage(context, "Hello Page " + i, "Hello World", john, ApprovalStatus.None, false, "Tag" + i);
				}
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest("Page 5"));

				Assert.AreEqual("Page 5", actual.Filter);
				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(20, actual.PerPage);
				Assert.AreEqual(1, actual.TotalCount);
				Assert.AreEqual(1, actual.TotalPages);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("Hello Page 5", actual.Results.First().Title);
			}
		}

		[TestMethod]
		public void GetPagesWithPagedFilter()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					if (i % 2 == 0)
					{
						TestHelper.AddPage(context, "Even Page " + i, "Hello World", john, ApprovalStatus.None, false, "Tag" + i);
					}
					else
					{
						TestHelper.AddPage(context, "Odd Page " + i, "Hello World", john, ApprovalStatus.None, false, "Tag" + i);
					}
				}
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest("Odd", 1, 2));

				Assert.AreEqual("Odd", actual.Filter);
				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(2, actual.PerPage);
				Assert.AreEqual(5, actual.TotalCount);
				Assert.AreEqual(3, actual.TotalPages);
				Assert.AreEqual(2, actual.Results.Count());
				Assert.AreEqual("Odd Page 1", actual.Results.First().Title);
				Assert.AreEqual("Odd Page 3", actual.Results.Last().Title);
			}
		}

		[TestMethod]
		public void GetPagesWithPagedFilterSecondPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					if (i % 2 == 0)
					{
						TestHelper.AddPage(context, "Even Page " + i, "Hello World", john, ApprovalStatus.None, false, "Tag" + i);
					}
					else
					{
						TestHelper.AddPage(context, "Odd Page " + i, "Hello World", john, ApprovalStatus.None, false, "Tag" + i);
					}
				}
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest("Odd", 2, 2));

				Assert.AreEqual("Odd", actual.Filter);
				Assert.AreEqual(2, actual.Page);
				Assert.AreEqual(2, actual.PerPage);
				Assert.AreEqual(5, actual.TotalCount);
				Assert.AreEqual(3, actual.TotalPages);
				Assert.AreEqual(2, actual.Results.Count());
				Assert.AreEqual("Odd Page 5", actual.Results.First().Title);
				Assert.AreEqual("Odd Page 7", actual.Results.Last().Title);
			}
		}

		[TestMethod]
		public void GetPagesWithPageRequestOutOfRange()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					TestHelper.AddPage(context, "Hello Page " + i, "Hello World", john, ApprovalStatus.None, false, "Tag" + i);
				}
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest(page: 30, perPage: 5));

				Assert.AreEqual("", actual.Filter);
				Assert.AreEqual(2, actual.Page);
				Assert.AreEqual(5, actual.PerPage);
				Assert.AreEqual(9, actual.TotalCount);
				Assert.AreEqual(2, actual.TotalPages);
				Assert.AreEqual(4, actual.Results.Count());
				Assert.AreEqual("Hello Page 6", actual.Results.First().Title);
				Assert.AreEqual("Hello Page 9", actual.Results.Last().Title);
			}
		}

		[TestMethod]
		public void GetPagesWithPagingFirstPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					TestHelper.AddPage(context, "Hello Page " + i, "Hello World", john, ApprovalStatus.None, false, "Tag" + i);
				}
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest(page: 1, perPage: 4));

				Assert.AreEqual("", actual.Filter);
				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(4, actual.PerPage);
				Assert.AreEqual(9, actual.TotalCount);
				Assert.AreEqual(3, actual.TotalPages);
				Assert.AreEqual(4, actual.Results.Count());
				Assert.AreEqual("Hello Page 1", actual.Results.First().Title);
				Assert.AreEqual("Hello Page 4", actual.Results.Last().Title);
			}
		}

		[TestMethod]
		public void GetPagesWithPagingLastPartialPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					TestHelper.AddPage(context, "Hello Page " + i, "Hello World", john, ApprovalStatus.None, false, "Tag" + i);
				}
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest(page: 3, perPage: 4));

				Assert.AreEqual("", actual.Filter);
				Assert.AreEqual(3, actual.Page);
				Assert.AreEqual(4, actual.PerPage);
				Assert.AreEqual(9, actual.TotalCount);
				Assert.AreEqual(3, actual.TotalPages);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("Hello Page 9", actual.Results.First().Title);
			}
		}

		[TestMethod]
		public void GetPagesWithPagingSecondPage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				for (var i = 1; i <= 9; i++)
				{
					TestHelper.AddPage(context, "Hello Page " + i, "Hello World", john, ApprovalStatus.None, false, "Tag" + i);
				}
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPages(new PagedRequest(page: 2, perPage: 4));

				Assert.AreEqual("", actual.Filter);
				Assert.AreEqual(2, actual.Page);
				Assert.AreEqual(4, actual.PerPage);
				Assert.AreEqual(9, actual.TotalCount);
				Assert.AreEqual(3, actual.TotalPages);
				Assert.AreEqual(4, actual.Results.Count());
				Assert.AreEqual("Hello Page 5", actual.Results.First().Title);
				Assert.AreEqual("Hello Page 8", actual.Results.Last().Title);
			}
		}

		[TestMethod]
		public void GetPageWithHistory()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				var page = TestHelper.AddPage(context, "Hello Page", "Hello World", john, ApprovalStatus.None, false, "myTag");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, null);
				var actual = service.GetPage(page.Id, true);

				Assert.AreEqual(page.Title, actual.Title);
			}
		}

		[TestMethod]
		public void GetTags()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "Page1", "Hello World", john, ApprovalStatus.None, false, "Tag1", "Tag2", "Tag3");
				TestHelper.AddPage(context, "Page2", "Hello World", john, ApprovalStatus.None, false, "Tag1", "Tag2");
				TestHelper.AddPage(context, "Page3", "Hello World", john, ApprovalStatus.None, false, "Tag1", "Tag3");
				context.SaveChanges();

				var path = Path.GetTempPath() + "ScribeTests";
				var searchService = new SearchService(context, path, john);
				var service = new ScribeService(context, null, searchService, john);
				var actual = service.GetTags(new PagedRequest("Tag3")).Results.Select(x => x.Tag).ToArray();

				Assert.AreEqual(3, actual.Length);
				TestHelper.AreEqual(new[] { "Tag1", "Tag2", "Tag3" }, actual);
			}
		}

		[TestMethod]
		public void GetUsers()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, user);
				var actual = service.GetUsers();

				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(1, actual.TotalCount);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("Administrator", actual.Results.First().UserName);
			}
		}

		[TestMethod]
		public void GetUsersFilterUsingTag()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user1 = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddUser(context, "John Doe", "Password1", "foo");
				TestHelper.AddUser(context, "Jane Smith", "Password2", "bar");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, user1);
				var actual = service.GetUsers(new PagedRequest("Tags=foo"));

				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(1, actual.TotalCount);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("JohnDoe", actual.Results.First().UserName);
			}
		}

		[TestMethod]
		public void GetUsersFilterUsingUserName()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user1 = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddUser(context, "John Doe", "Password1", "foo");
				TestHelper.AddUser(context, "Jane Smith", "Password2", "bar");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, user1);
				var actual = service.GetUsers(new PagedRequest("UserName=Jane"));

				Assert.AreEqual(1, actual.Page);
				Assert.AreEqual(1, actual.TotalCount);
				Assert.AreEqual(1, actual.Results.Count());
				Assert.AreEqual("JaneSmith", actual.Results.First().UserName);
			}
		}

		[TestMethod]
		public void RenameTag()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "Administrator", "Password!", "administrator");
				TestHelper.AddSettings(context, user, new SettingsView { SoftDelete = true });
				var john = TestHelper.AddUser(context, "John Doe", "Password!");
				TestHelper.AddPage(context, "Page1", "Hello World", john, ApprovalStatus.None, false, "Tag1", "Tag2", "Tag3");
				TestHelper.AddPage(context, "Page2", "Hello World", john, ApprovalStatus.None, false, "Tag1", "Tag2");
				context.SaveChanges();

				var path = Path.GetTempPath() + "ScribeTests";
				var searchService = new SearchService(context, path, john);
				var service = new ScribeService(context, null, searchService, john);
				service.RenameTag(new RenameValues { OldName = "Tag2", NewName = "TagTwo" });

				var actual = service.GetTags().Results.Select(x => x.Tag).ToArray();
				Assert.AreEqual(3, actual.Length);
				TestHelper.AreEqual(new[] { "Tag1", "Tag3", "TagTwo" }, actual);
			}
		}

		[TestMethod]
		public void SavePage()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "John Doe", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				context.SaveChanges();

				var input = new PageView { Text = "The quick brown fox jumped over the lazy dogs back.", Title = "Title" };
				var service = new ScribeService(context, null, null, user);
				var actualView = service.SavePage(input);

				Assert.AreEqual(input.Title, actualView.Title);
				Assert.AreEqual(input.Text, actualView.Text);

				var actualEntity = context.Pages.First();
				Assert.AreEqual(input.Title, actualEntity.Title);
				Assert.AreEqual(input.Text, actualEntity.Text);
			}
		}

		[TestMethod]
		public void SavePageForHistory()
		{
			using (var context = TestHelper.GetContext())
			{
				var user = TestHelper.AddUser(context, "John Doe", "Password!", "administrator");
				TestHelper.AddDefaultSettings(context, user);
				context.SaveChanges();

				var input = new PageView { Text = "The quick brown fox jumped over the lazy dogs back.", Title = "Title" };
				var service = new ScribeService(context, null, null, user);
				var actualView = service.SavePage(input);

				Assert.AreEqual(input.Title, actualView.Title);
				Assert.AreEqual(input.Text, actualView.Text);

				var actualEntity = context.Pages.First();
				Assert.AreEqual(input.Title, actualEntity.Title);
				Assert.AreEqual(input.Text, actualEntity.Text);
				Assert.AreEqual(0, actualEntity.History.Count);

				input.Id = actualEntity.Id;
				input.Text = "Boom, nope.";

				actualView = service.SavePage(input);
				Assert.AreEqual(input.Title, actualView.Title);
				Assert.AreEqual(input.Text, actualView.Text);
				Assert.AreEqual(1, actualEntity.History.Count);
			}
		}

		[TestMethod]
		public void SaveUser()
		{
			var provider = TestHelper.GetContextProvider();

			using (var context = provider.GetContext())
			{
				var user = TestHelper.AddUser(context, "John Doe", "Password!", "administrator");
				context.SaveChanges();

				var service = new ScribeService(context, null, null, user);
				var view = service.GetUser(user.Id);

				view.DisplayName = "Jane Smith";
				view.UserName = "JaneSmith";
				view.Tags = new[] { "approver", "publisher" };

				var actual = service.SaveUser(view);

				Assert.AreEqual("Jane Smith", actual.DisplayName);
				Assert.AreEqual("JaneSmith", actual.UserName);
				Assert.AreEqual(2, actual.Tags.Count());
				Assert.AreEqual("approver", actual.Tags.First());
				Assert.AreEqual("publisher", actual.Tags.Last());
			}
		}

		#endregion
	}
}