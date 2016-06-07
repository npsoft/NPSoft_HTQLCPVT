using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.Controllers;
using PhotoBookmart.DataLayer.Models.Sites;
using ServiceStack.ServiceInterface;
using ServiceStack;
using System.Web.Routing;
using PhotoBookmart.Common;
using PhotoBookmart.DataLayer.Models.ExtraShipping;
using PhotoBookmart.DataLayer.Models.Users_Management;
using ServiceStack.OrmLite;
using PhotoBookmart.Common.Helpers;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    /// <summary>
    /// Based controller for Administration Panel
    /// </summary>
    [Authenticate]
    //[ABNotHaveRoleAttribute(RoleEnum.Customer)]
    [ABRequiresAnyRole(RoleEnum.Admin, RoleEnum.Province, RoleEnum.District, RoleEnum.Village)]
    public class WebAdminController : BaseController
    {
        public override string LoginRedirectUrl
        {
            get { return Url.Action("Logon", "Login", new { redirectTo = "{0}" }); }
        }

        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);
        }

        ~WebAdminController()
        {
            Dispose(true);
        }

        #region Functions for Users in Administration Panel

        #endregion
    }
}