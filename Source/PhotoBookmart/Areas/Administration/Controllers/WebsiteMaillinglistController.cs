using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.Common.Helpers;
using System.IO;
using PhotoBookmart.Controllers;
using ServiceStack.OrmLite;
using ServiceStack;
using ServiceStack.Common;
using ServiceStack.ServiceInterface;
using PhotoBookmart.Areas.Administration.Controllers;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.Sites;
using System.Collections;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    public class WebsiteMaillinglistController : WebAdminController
    {
        //
        public ActionResult Index()
        {
            var model = new MaillingListSendModel();
            return View(model);
        }

        public ActionResult _Get_SiteUser()
        {
            var x = Cache_GetAllUsers();
            return Json(x);
        }

        public ActionResult _Get_Newsletter()
        {
            var x = Cache_GetAllNewsletter();
            return Json(x);
        }

        [ValidateInput(false)]
        public ActionResult SendMailing(MaillingListSendModel model)
        {
            var users = Cache_GetAllUsers();
            var user_to_send = new List<ABUserAuth>();

            if (model.TargetEmails == null || model.TargetEmails.Count == 0)
            {
                return JsonError("Please select receivers before send email");
            }

            if (string.IsNullOrEmpty(model.Title))
            {
                return JsonError("Please enter message title");
            }

            if (string.IsNullOrEmpty(model.Body))
            {
                return JsonError("Please enter message body");
            }

            foreach (var x in model.TargetEmails)
            {
                var k = users.Where(m => m.Id == x).FirstOrDefault();
                if (k == null || k.Id == 0)
                    continue;
                user_to_send.Add(k);
            }

            foreach (var u in user_to_send)
            {
                var body = RenderEmailBody(model.Body, model, u);
                var title = RenderEmailTitle(model.Title);
                // insert into email queue
                PhotoBookmart.Common.Helpers.SendEmail.SendMail(u.Email, title, body);
            }

            return JsonSuccess("", "Send success");
        }

        public ActionResult _Get_Template(int id)
        {
            var item = Db.Where<Site_MaillingListTemplate>(m => m.Id == id).FirstOrDefault();
            if (item == null)
            {
                return Json(new { Title = "", Body = "" });
            }
            else
            {
                return Json(new { Title = item.Title, Body = item.Body });
            }
        }

        string RenderEmailBody(string content, MaillingListSendModel model, ABUserAuth user)
        {
            Hashtable tokens = new Hashtable();
            var domain = Request.Url.Host;

            // prepare for the tokens
            // get current website information
            var site = Cache_GetWebSite();

            if (string.IsNullOrEmpty(site.Email_Admin))
            {
                site.Email_Admin = "";
            }

            if (string.IsNullOrEmpty(site.Email_Support))
            {
                site.Email_Support = "";
            }

            if (site.UseSSL)
            {
                domain = "https://" + domain + "/";
            }
            else
            {
                domain = "http://" + domain + "/";
            }

            tokens.Add("#website_domain", domain);
            tokens.Add("#website_name", site.Name);
            tokens.Add("#website_admin_email", site.Email_Admin);
            tokens.Add("#website_info_email", site.Email_Support);

            if (user != null)
            {
                if (string.IsNullOrEmpty(user.FullName))
                {
                    tokens.Add("#user_name", user.UserName);
                }
                else
                {
                    tokens.Add("#user_name", user.FullName);
                }

                tokens.Add("#user_username", user.UserName);
            }

            return content;
        }

        string RenderEmailTitle(string content)
        {
            return content;
        }
    }
}
