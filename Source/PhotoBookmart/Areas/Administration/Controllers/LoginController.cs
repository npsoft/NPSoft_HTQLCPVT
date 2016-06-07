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
    public class LoginController : BaseController
    {
        //
        // GET: /Administration/Management/
        private EncryptAndDecrypt encrypt = new EncryptAndDecrypt();

        public ActionResult Logon()
        {
            return Redirect("/");
        }

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var u = Db.Where<ABUserAuth>(m => (m.UserName.ToLower() == model.UserName.ToLower() || m.Email.ToLower() == model.UserName.ToLower())).FirstOrDefault();
                    
                    if (u == null)
                    {
                        ModelState.AddModelError("", "Could not login");
                    }
                    else
                    {
                        var authResponse = AuthService.Post(new Auth
                        {
                            UserName = u.UserName,
                            Password = model.Password,
                            RememberMe = model.RememberMe,
                            Continue = ""
                        });

                        if (authResponse is IHttpError)
                        {
                            ModelState.AddModelError("", "General error exception");
                        }

                        var typedResponse = authResponse as AuthResponse;
                        if (typedResponse != null || AuthenticatedUserID > 0)
                        {

                            if (u.HasRole(RoleEnum.Administrator) || u.HasRole(RoleEnum.OrderManagement) || u.HasRole(RoleEnum.Employee))
                            {
                                if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                                    && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\") && !returnUrl.EndsWith("Logon"))
                                {
                                    return Redirect(returnUrl);
                                }
                                else
                                {
                                    return RedirectToAction("Index", "Order", new { area = "Administration" });
                                }
                            }
                            else
                            {

                                // logout then go to /
                                AuthService.RemoveSession();
                                return Redirect("/");
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("", "Could not login");
                        }
                    }
                }
                catch
                {

                }
            }
            // If we got this far, something failed, redisplay form
            return View(model);
        }

        public ActionResult LogOff()
        {
            AuthService.RemoveSession();
            return RedirectToAction("Logon", "Login", new { area = "Administration" });
        }
    }
}
