﻿#region References

using System.Web.Hosting;
using System.Web.Http;
using Scribe.Data;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Services;
using Scribe.Website.Services;

#endregion

namespace Scribe.Website.Controllers.API
{
	public class ServiceController : BaseApiController, IScribeService
	{
		#region Fields

		private readonly ScribeService _service;

		#endregion

		#region Constructors

		public ServiceController(IScribeContext dataContext, IAuthenticationService authenticationService)
			: base(dataContext, authenticationService)
		{
			var path = HostingEnvironment.MapPath("~/App_Data/Indexes");
			var searchService = new SearchService(DataContext, path, GetCurrentUser(false));
			var accountService = new AccountService(dataContext, authenticationService);
			_service = new ScribeService(dataContext, accountService, searchService, GetCurrentUser(false));
		}

		#endregion

		#region Methods

		[HttpPost]
		public void CancelPage(int id)
		{
			_service.CancelPage(id);
		}

		[HttpPost]
		public void DeleteFile(int id)
		{
			_service.DeleteFile(id);
		}

		[HttpPost]
		public void DeletePage(int id)
		{
			_service.DeletePage(id);
		}

		[HttpPost]
		public void DeleteTag(string name)
		{
			_service.DeleteTag(name);
		}

		[AllowAnonymous]
		public FileView GetFile(int id, bool includeData)
		{
			return _service.GetFile(id, includeData);
		}

		[AllowAnonymous]
		public FileView GetFile(string name, bool includeData)
		{
			return _service.GetFile(name, includeData);
		}

		[HttpPost]
		[AllowAnonymous]
		public PagedResults<FileView> GetFiles(PagedRequest request)
		{
			return _service.GetFiles(request);
		}

		[AllowAnonymous]
		public PageView GetPage(int id, bool includeHistory)
		{
			return _service.GetPage(id, includeHistory);
		}

		[HttpPost]
		[AllowAnonymous]
		public PagedResults<PageView> GetPages(PagedRequest request)
		{
			return _service.GetPages(request);
		}

		[HttpPost]
		[AllowAnonymous]
		public PagedResults<TagView> GetTags(PagedRequest request)
		{
			return _service.GetTags(request);
		}

		[Authorize(Roles = "Administrator")]
		public UserView GetUser(int id)
		{
			return _service.GetUser(id);
		}

		[HttpPost]
		[Authorize(Roles = "Administrator")]
		public PagedResults<UserView> GetUsers(PagedRequest request)
		{
			return _service.GetUsers(request);
		}

		[HttpPost]
		[AllowAnonymous]
		public void LogIn(Credentials login)
		{
			_service.LogIn(login);
		}

		[HttpPost]
		public void LogOut()
		{
			_service.LogOut();
		}

		[HttpPost]
		public string Preview(PageView model)
		{
			return _service.Preview(model);
		}

		[HttpPost]
		public void RenameTag(RenameValues values)
		{
			_service.RenameTag(values);
		}

		[HttpPost]
		public int SaveFile(FileView view)
		{
			return _service.SaveFile(view);
		}

		[HttpPost]
		public PageView SavePage(PageView view)
		{
			return _service.SavePage(view);
		}

		[HttpPost]
		[Authorize(Roles = "Administrator")]
		public UserView SaveUser(UserView view)
		{
			return _service.SaveUser(view);
		}

		[HttpPost]
		public PageView UpdatePage(PageUpdate update)
		{
			return _service.UpdatePage(update);
		}

		#endregion
	}
}