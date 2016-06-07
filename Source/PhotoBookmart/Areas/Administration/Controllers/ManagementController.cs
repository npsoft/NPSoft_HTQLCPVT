using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ServiceStack.Mvc;
using ServiceStack.OrmLite;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.Common.Json;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.Controllers;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceHost;
using PhotoBookmart.Support;
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    public class ManagementController : WebAdminController
    {
        public ActionResult Index()
        {
            if (User == null || !User.Identity.IsAuthenticated)
            {
                return Redirect("/");
            }

            var u = CurrentUser;
            if (u.HasRole(RoleEnum.Employee) || u.HasRole(RoleEnum.OrderManagement))
            {
                return Redirect(Url.Action("Index", "Order", new { area = "Administration" }));
            }
            else if (u.HasRole(RoleEnum.Administrator))
            {
                //return View();
                return RedirectToAction("Detail", "Website", new { id = 1 });
            }

            return Redirect("/");
        }

        #region Site Security
        ///// <summary>
        ///// Take down the site 
        ///// </summary>
        ///// <param name="file"></param>
        ///// <returns></returns>
        //public string SiteDown(string password)
        //{
        //    return Adz.Common.Security.ABSecurity.TakeDown(encrypt.Encrypt(password, "absoft.vn", true)).ToString();
        //}

        ///// <summary>
        ///// Site up to normal state
        ///// </summary>
        ///// <param name="password"></param>
        ///// <returns></returns>
        //public string SiteUp(string password)
        //{
        //    return CMS.Common.Security.ABSecurity.TakeUp(encrypt.Encrypt(password, "absoft.vn", true)).ToString();
        //}
        #endregion


        /// <summary>
        /// Handle file upload from text editor
        /// </summary>
        /// <param name="file"></param>
        /// <returns>file path</returns>
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult UploadImage(HttpPostedFileBase upload, string CKEditorFuncNum,
                                      string CKEditor, string langCode)
        {
            var filename = UploadFile((long)CurrentUser.Id, CurrentUser.UserName, "_Editor", new List<HttpPostedFileBase>() { upload }, false);

            var url = @"/" + filename;

            // passing message success/failure
            var message = "Image was saved correctly";

            if (url == @"/")
            {
                message = "Could not upload the file you selected";
            }

            // since it is an ajax request it requires this string
            string output = @"<html><body><script>window.parent.CKEDITOR.tools.callFunction(" + CKEditorFuncNum + ", \"" + url + "\", \"" + message + "\");</script></body></html>";
            return Content(output);
        }

    }
}
