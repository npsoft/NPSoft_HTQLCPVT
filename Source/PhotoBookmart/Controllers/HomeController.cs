using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.OrmLite;
using MvcReCaptcha;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.DataLayer.Models.ExtraShipping;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.System;
using System.Data;

namespace PhotoBookmart.Controllers
{
    public class HomeController : BaseController
    {
        private EncryptAndDecrypt encrypt = new EncryptAndDecrypt();

        [HttpGet]
        public ActionResult JavascriptCommon()
        {
            return PartialView("_JavascriptCommon");
        }

        [HttpGet]
        public ActionResult Index()
        {
            return RedirectToAction("SignIn", "Home", new { });
        }

        //public ActionResult PaymentAndShipping()
        //{
        //    return View();
        //}

        public ActionResult Help()
        {
            return View();
        }

        [HttpGet]
        public ActionResult SignIn(string redirectTo)
        {
            if (User.Identity.IsAuthenticated) { return Redirect("/Administration/WebsiteProduct"); }
            var model = new LoginModel() { RedirectTo = redirectTo };
            return View("SignIn", model);
        }

        [HttpPost]
        public ActionResult SignInSubmit(LoginModel model)
        {
            if (!(string.IsNullOrEmpty(model.UserName) && string.IsNullOrEmpty(model.Pass)))
            {
                var u = Db.Where<ABUserAuth>(m => (m.UserName.ToLower() == model.UserName.ToLower() || m.Email.ToLower() == model.UserName.ToLower())).FirstOrDefault();

                if (u == null)
                {
                    ModelState.AddModelError("", "Could not Sign In.");
                }
                else
                {
                    var authResponse = AuthService.Post(new Auth
                    {
                        UserName = u.UserName,
                        Password = model.Pass,
                        RememberMe = model.CheckRemember,
                        Continue = ""
                    });

                    if (authResponse is IHttpError)
                    {
                        ModelState.AddModelError("", "General error exception.");
                    }

                    var typedResponse = authResponse as AuthResponse;

                    if (typedResponse != null || AuthenticatedUserID > 0)
                    {
                        if (Url.IsLocalUrl(model.RedirectTo) && model.RedirectTo.Length > 1 &&
                            model.RedirectTo.StartsWith("/") && !model.RedirectTo.StartsWith("//") &&
                            !model.RedirectTo.StartsWith("/\\") && !model.RedirectTo.EndsWith("SignIn"))
                        {
                            return Redirect(model.RedirectTo);
                        }
                        else { return Redirect("/Administration/WebsiteProduct"); }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Could not Sign In.");
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(model.UserName))
                {
                    ModelState.AddModelError("", "Please enter username");
                }
                else if (string.IsNullOrEmpty(model.Pass))
                {
                    ModelState.AddModelError("", "Please enter password");
                }
            }

            return View("SignIn", model);
        }
        
        [Authenticate]
        public ActionResult SignOut(string redirectTo)
        {
            AuthService.RemoveSession();

            return Redirect(redirectTo != null ? redirectTo : "/");
        }

        #region Register

        public ActionResult Register(string redirectTo)
        {
            var model = new ABUserAuth() { RedirectTo = redirectTo };

            return View("Register", model);
        }

        [HttpPost]
        public JsonResult GetStatesForMalaysia()
        {
            List<Country_State_ExtraShipping> result = GetExtraShippingByCountryCode();

            return Json(new
            {
                Data = result,
                HasVal = result != null && result.Count != 0 ? true : false
            });
        }

        /// <summary>
        /// Get states by country code (MY, VN, CH, UK)
        /// </summary>
        /// <param name="country"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetCountryStates(string country)
        {
            List<Country_State_ExtraShipping> result = GetExtraShippingByCountryCode(country);

            return Json(new
            {
                Data = result,
                HasVal = result != null && result.Count != 0 ? true : false
            });
        }

        [HttpPost]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult RegisterSubmit(ABUserAuth model)
        {
            if (String.IsNullOrEmpty(model.Email) ||
                String.IsNullOrEmpty(model.Password) ||
                String.IsNullOrEmpty(model.ConfirmPassword) ||
                String.IsNullOrEmpty(model.FirstName) ||
                String.IsNullOrEmpty(model.LastName) ||
                String.IsNullOrEmpty(model.Country) ||
                String.IsNullOrEmpty(model.MaHC) ||
                String.IsNullOrEmpty(model.PostalCode) ||
                String.IsNullOrEmpty(model.Phone))
            {
                ViewBag.Error = "Please enter all required fields.";

                return View("Register", model);
            }

            // get the country
            var c = Db.Select<Country>(x => x.Where(m => m.Code == model.Country).Limit(1)).FirstOrDefault();
            if (c == null)
            {
                ViewBag.Error = "Your selected country is not found";
                return View("Register", model);
            }

            // validate the phone number
            if (!IsValidPhoneByCountry(model.Phone, c.Code, true))
            {
                ViewBag.Error = "We can not validate your phone number with your selected country.";
                return View("Register", model);
            }

            if (!IsValidEmailAddress(model.Email))
            {
                ViewBag.Error = "We can not validate your email address format.";
                return View("Register", model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                ViewBag.Error = "Please enter same Password and Re password fields.";

                return View("Register", model);
            }

            if (!new Regex(@"(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).{8,}$", RegexOptions.Compiled).IsMatch(model.Password))
            {
                ViewBag.Error = "Password must contain at least 8 characters, including uppercase/lowercase and numbers";

                return View("Register", model);
            }

            //if (!captchaValid)
            //{
            //    ViewBag.Error = "Your captcha is not match.";

            //    return View("Register", model);
            //}

            if (User_GetByEmail(model.Email) != null)
            {
                ViewBag.Error = "There is an user with same Email as you entered. Please use difference Email.";

                return View("Register", model);
            }

            if (User_GetByUsername(model.UserName) != null)
            {
                ViewBag.Error = "There is an user with same Username as you entered. Please use difference Username.";

                return View("Register", model);
            }

            var p = PasswordGenerate(model.Password);

            ABUserAuth user = new ABUserAuth()
            {
                Email = model.Email,
                UserName = model.Email,
                Roles = new List<string>() { RoleEnum.Customer.ToString() },
                PasswordHash = p.Id,
                Salt = p.Name,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Country = model.Country,
                MaHC = model.MaHC,
                PostalCode = model.PostalCode,
                Phone = model.Phone,
                DigestHa1Hash = encrypt.GetMD5HashData(model.Email),
                CreatedDate = DateTime.Now,
                ActiveStatus = true,
            };

            user.FullName = user.FirstName + " " + user.LastName;
            user.DisplayName = user.FullName;

            try
            {
                Db.Insert<ABUserAuth>(user);

                user.Id = (int)Db.GetLastInsertId();

                var template = Get_MaillingListTemplate("register_notify_user");

                var template_helper = new EmailHelper(template.Title, template.Body);

                template_helper.Parameters.Add("Host", CurrentWebsite.Domain.First());

                template_helper.Parameters.Add("User", user.UserName);

                template_helper.Parameters.Add("Code", user.DigestHa1Hash);

                template_helper.Sender_Email = CurrentWebsite.Email_Support;

                template_helper.Sender_Name = CurrentWebsite.Name;

                template_helper.Receiver.Add(user.Email);

                SendMail(template_helper);

                template = Get_MaillingListTemplate("register_notify_admin");

                template_helper = new EmailHelper(template.Title, template.Body);

                template_helper.Parameters.Add("Host", InternalService.CurrentWebsiteDomainURL);

                template_helper.Parameters.Add("Id", user.Id.ToString());

                template_helper.Parameters.Add("User", user.UserName);

                template_helper.Parameters.Add("Email", user.Email);

                template_helper.Parameters.Add("Date", DateTime.Now.ToString());

                template_helper.Sender_Email = CurrentWebsite.Email_Support;

                template_helper.Sender_Name = CurrentWebsite.Name;

                template_helper.Receiver.Add(CurrentWebsite.Email_Admin);

                SendMail(template_helper);

                ViewBag.Message = "Your Account has been created! We just sent to you one email to confirm your account information. Please make sure to check your spam folder in your mail box. <br>Photobookmart also login for you automatically. Enjoy...";

                // do the auto login
                //return SignInSubmit(new LoginModel() { CheckRemember = true, Pass = model.Password, RedirectTo = model.RedirectTo, UserName = model.UserName });
                var authResponse = AuthService.Post(new Auth
                {
                    UserName = model.Email,
                    Password = model.Password,
                    Continue = ""
                });
            }
            catch (Exception ex)
            {
                ViewBag.RedirectTo = Url.Action("Register", "User", new { });

                ViewBag.Message = string.Format("{0}: {1}.", "There was an error when registering", ex.Message);
            }

            if (!string.IsNullOrEmpty(model.RedirectTo))
            {
                //ViewBag.RedirectTo = Url.Action("SignIn", new { redirectTo = model.RedirectTo });
                ViewBag.RedirectTo = model.RedirectTo;
            }
            else
            {

                //ViewBag.RedirectTo = Url.Action("SignIn");
                ViewBag.RedirectTo = "/";
            }
            return View("Message");
        }

        public ActionResult RegisterConfirm(string code)
        {
            try
            {
                var user = Db.Select<ABUserAuth>(x => x.Where(y => (y.DigestHa1Hash == code)).Limit(1)).FirstOrDefault();

                user.ActiveStatus = true;

                user.ModifiedDate = DateTime.Now;

                user.DigestHa1Hash = null;

                Db.UpdateOnly<ABUserAuth>(user, ev => ev.Update(p => new
                {
                    p.ActiveStatus,

                    p.DigestHa1Hash,

                    p.ModifiedDate

                }).Where(m => (m.Id == user.Id)));

                var template = Get_MaillingListTemplate("register_confirm_user");

                var template_helper = new EmailHelper(template.Title, template.Body);

                template_helper.Parameters.Add("User", user.UserName);

                template_helper.Parameters.Add("Email", user.Email);

                template_helper.Parameters.Add("Password", string.Concat(Enumerable.Repeat("*", 10)));

                template_helper.Sender_Email = CurrentWebsite.Email_Support;

                template_helper.Sender_Name = CurrentWebsite.Name;

                template_helper.Receiver.Add(user.Email);

                SendMail(template_helper);

                ViewBag.RedirectTo = Url.Action("SignIn", "Home", new { redirectTo = Url.Action("Profile", "User", new { }) });

                ViewBag.Message = string.Format("Congratulations! Your account ({0}) has been activated.", user.Email);
            }
            catch (Exception ex)
            {
                ViewBag.RedirectTo = Url.Action("Index", "Home", new { });

                ViewBag.Message = string.Format("{0}: {1}", "There was an error", ex.Message);
            }

            return View("Message");
        }

        #endregion

        public ActionResult ForgotPassword()
        {
            var model = new ABUserAuth();

            return View("ForgotPassword", model);
        }

        public ActionResult ForgotPasswordSubmit(ABUserAuth model)
        {
            try
            {
                if (String.IsNullOrEmpty(model.Email))
                {
                    ViewBag.Error = "Please enter email.";

                    return View("ForgotPassword", model);
                }

                if (!IsValidEmailAddress(model.Email))
                {
                    ViewBag.Error("Email format is not valid.");

                    return View("ForgotPassword", model);
                }

                var user = User_GetByEmail(model.Email);

                if (user == null)
                {
                    ViewBag.Error = "Email you entered is not exist.";

                    return View("ForgotPassword", model);
                }

                var template = Get_MaillingListTemplate("forgot_password_user");

                var template_helper = new EmailHelper(template.Title, template.Body);

                template_helper.Parameters.Add("Host", CurrentWebsite.Domain.First());

                template_helper.Parameters.Add("Code", encrypt.GetMD5HashData(user.Email + user.PasswordHash));

                template_helper.Parameters.Add("Email", user.Email);

                template_helper.Sender_Email = CurrentWebsite.Email_Support;

                template_helper.Sender_Name = CurrentWebsite.Name;

                template_helper.Receiver.Add(user.Email);

                SendMail(template_helper);

                ViewBag.RedirectTo = Url.Action("Index", "Home", new { }); ;

                ViewBag.Message = "Please check your email for instruction to get new password.";
            }
            catch (Exception ex)
            {
                ViewBag.RedirectTo = Url.Action("Index", "Home", new { });

                ViewBag.Message = string.Format("{0}: {1}.", "There was an error getting new password", ex.Message);
            }

            return View("Message");
        }

        public ActionResult GetNewPassword(string email, string code)
        {
            var user = User_GetByEmail(email);

            if (user != null && code == encrypt.GetMD5HashData(email + user.PasswordHash))
            {
                return View("GetNewPassword", user);
            }

            ViewBag.RedirectTo = Url.Action("Index", "Home", new { });

            ViewBag.Message = "Get Password failed.";

            return View("Message");
        }

        public ActionResult GetNewPasswordSubmit(ABUserAuth model)
        {
            if (string.IsNullOrEmpty(model.PassNews) ||
                string.IsNullOrEmpty(model.ConfirmPassword))
            {
                ViewBag.Error = "Please enter all required fields.";

                return View("GetNewPassword", model);
            }

            if (model.PassNews != model.ConfirmPassword)
            {
                ViewBag.Error = "Please enter same New password and Confirm password fields.";

                return View("GetNewPassword", model);
            }

            var user = User_GetByID(model.Id);

            if (user == null || user.Email != model.Email)
            {
                ViewBag.Error = "Please don't try to hack us.";

                return View("GetNewPassword", model);
            }

            var pass = PasswordGenerate(model.PassNews);

            user.PasswordHash = pass.Id;

            user.Salt = pass.Name;

            user.ModifiedDate = DateTime.Now;

            Db.UpdateOnly<ABUserAuth>(user, ev => ev.Update(p => new
            {
                p.PasswordHash,

                p.Salt,

                p.ModifiedDate

            }).Where(m => (m.Id == user.Id)));

            ViewBag.RedirectTo = Url.Action("SignIn", "Home", new { redirectTo = Url.Action("Index", "User", new { }) });

            ViewBag.Message = "Congratulations! Your account password has been changed successful.";

            return View("Message");
        }
    }
}
