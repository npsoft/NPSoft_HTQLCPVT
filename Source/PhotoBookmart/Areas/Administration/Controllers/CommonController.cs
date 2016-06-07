using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.Common.Helpers;
using System.Configuration;
using PhotoBookmart.Controllers;
using ServiceStack.ServiceInterface;
using PhotoBookmart.DataLayer.Models.Users_Management;
using ServiceStack.OrmLite;
using PhotoBookmart.DataLayer.Models.Sites;
using PhotoBookmart.ServiceInterface;
using ServiceStack.Common;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    [Authenticate]
    public class CommonController : WebAdminController
    {
        
        //
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult StatisticArea()
        {
            return View();
        }

        /// <summary>
        /// Sidebar left menu area
        /// </summary>
        /// <returns></returns>
        public ActionResult Sidebar()
        {

            // get distributors
            return View();
        }

        public ActionResult TopRight_NotificationArea()
        {
            return View();
        }

        public ActionResult TopRight_Message()
        {
            return View();
        }

        public ActionResult TopRight_UserInfo()
        {
            var model = User_GetByID_ToModel(AuthenticatedUserID);
            if (model == null)
                model = new UserModel();
            return View(model);
        }
    }
}
