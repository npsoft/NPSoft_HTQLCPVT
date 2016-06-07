using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Text.RegularExpressions;

using MvcReCaptcha;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.DataLayer.Models.System;

namespace PhotoBookmart.Controllers
{
    [Authenticate]
    public class UserController : BaseController
    {
        public ActionResult Index()
        {
            return View("index");
        }

        public ActionResult Profile()
        {
            return View("Profile", CurrentUser);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Profile(ABUserAuth model, IEnumerable<HttpPostedFileBase> FileUps)
        {
            if (string.IsNullOrEmpty(model.FirstName) ||
                string.IsNullOrEmpty(model.LastName) ||
                string.IsNullOrEmpty(model.FullName) ||
                string.IsNullOrEmpty(model.Country) ||
                string.IsNullOrEmpty(model.MaHC) ||
                string.IsNullOrEmpty(model.PostalCode) ||
                string.IsNullOrEmpty(model.Phone) ||
                string.IsNullOrEmpty(model.Gender) ||
                string.IsNullOrEmpty(model.Email))
            {
                ViewBag.Error = "Please enter all required fields.";

                return View("Profile", model);
            }

            // get the country
            var c = Db.Select<Country>(x => x.Where(m => m.Code == model.Country).Limit(1)).FirstOrDefault();
            if (c == null)
            {
                ViewBag.Error = "Your selected country is not found";
                return View("Profile", model);
            }

            // validate the phone number
            if (!IsValidPhoneByCountry(model.Phone, c.Code, true))
            {
                ViewBag.Error = "We can not validate your phone number with your selected country.";
                return View("Profile", model);
            }

            if (!IsValidEmailAddress(model.Email))
            {
                ViewBag.Error = "We can not validate your email address format.";
                return View("Profile", model);
            }

            model.Nickname = (FileUps != null && FileUps.Count() != 0 && FileUps.First() != null) ? UploadFile(model.Id, model.UserName, "", FileUps) : CurrentUser.Nickname;
            model.UserName = model.Email;

            model.ModifiedDate = DateTime.Now;
            
            Db.UpdateOnly<ABUserAuth>(model, ev => ev.Update(p => new {
                p.FirstName,
                p.LastName,
                p.FullName,
                p.Country,
                p.MaHC,
                p.PostalCode,
                p.Phone,
                p.Gender,
                p.BirthDate,
                p.Nickname,
                p.ModifiedDate,
                p.Email,
                p.UserName
            }).Where(m => (m.Id == CurrentUser.Id)).Limit(1));

            ViewBag.RedirectTo = Url.Action("Index", "User", new { });

            ViewBag.Message = "Update profile success.";

            return View("Message");
        }

        public ActionResult ChangePass()
        {
            return View("ChangePass", CurrentUser);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ChangePass(ABUserAuth model)
        {
            if (CurrentUser.Id != model.Id ||
                CurrentUser.Email != model.Email ||
                CurrentUser.UserName != model.UserName)
            {
                ViewBag.Error = "Please don't try to hack us.";

                return View("ChangePass", model);
            }

            if (string.IsNullOrEmpty(model.PassNews) ||
                string.IsNullOrEmpty(model.ConfirmPassword))
            {
                ViewBag.Error = "Please enter all required fields.";

                return View("ChangePass", model);
            }

            if (model.PassNews != model.ConfirmPassword)
            {
                ViewBag.Error = "Please enter same New password and Confirm password fields.";

                return View("ChangePass", model);
            }

            if (!new Regex(@"(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).{8,}$", RegexOptions.Compiled).IsMatch(model.PassNews))
            {
                ViewBag.Error = "Password must contain at least 8 characters, including uppercase/lowercase and numbers";

                return View("ChangePass", model);
            }

            var newPassword = PasswordGenerate(model.PassNews);

            model.PasswordHash = newPassword.Id;

            model.Salt = newPassword.Name;

            model.ModifiedDate = DateTime.Now;

            Db.UpdateOnly<ABUserAuth>(model, ev => ev.Update(p => new
            {
                p.PasswordHash,

                p.Salt,

                p.ModifiedDate

            }).Where(m => (m.Id == model.Id)));

            ViewBag.RedirectTo = Url.Action("Index", "User", new { });

            ViewBag.Message = "Change password success.";

            return View("Message");
        }
    }
}