using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.DataLayer.Models;
using System.Xml;
using System.Data;
using ServiceStack.Mvc;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.Sites;
using ServiceStack.Common.Web;
using PhotoBookmart.Models;
using MvcReCaptcha;

namespace PhotoBookmart.Controllers
{
    public class ContactUsController : BaseController
    {
        public ActionResult Index()
        {
            string country_code = Setting_GetCurrentCountry();

            var config = CurrentWebsite.Contactus_Info(country_code);
            if (config == null)
            {
                config = new Site_ContactusConfig() { Address = "" };
            }
            else
            {
                config.Info = config.Info.Replace("\r\n", "");
            }

            ViewData["config"] = config;
            return View(new Site_ContactUs());
        }

        [CaptchaValidator]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ContactSubmit(Site_ContactUs model, bool captchaValid)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return JsonError("Please enter your name");
            }

            if (string.IsNullOrEmpty(model.Email))
            {
                return JsonError("Please enter your email");
            }

            if (string.IsNullOrEmpty(model.Comment))
            {
                return JsonError("Please enter your message");
            }

            if (string.IsNullOrEmpty(model.Comment))
            {
                return JsonError("Please enter your captcha");
            }

            if (!IsValidEmailAddress(model.Email))
            {
                return JsonError("Email is not valid");
            }

            if (!captchaValid)
            {
                return JsonError("Your captcha is not match");
            }

            model.ParentId = 0;
            model.IsNew = true;
            model.Reply_On = DateTime.Now;
            model.Reply_UserId = 0;
            model.Id = 0;

            model.Contact_UserId = AuthenticatedUserID;
            model.Contact_On = DateTime.Now;
            model.Contact_IP = InternalService.CurrentUserIP;

            Db.Insert<Site_ContactUs>(model);

            try
            {
                this.SendEmailContact(model);
            }
            catch (Exception ex)
            {
                return JsonError(string.Format("{0}. {1}.", "There was an error sending the email contact", ex.Message));
            }

            return JsonSuccess("", "Thanks. Your contact has been submitted. We will get back with you as soon as possible.");
        }

        /// <summary>
        /// Send email to notify admin and visitor
        /// </summary>
        /// <param name="model"></param>
        private void SendEmailContact(Site_ContactUs model)
        {
            // send emailto notify 
            var template = Get_MaillingListTemplate("contact_us___user___thanks_and_confirm");
            var template_helper = new EmailHelper(template.Title, template.Body);
            template_helper.Parameters.Add("website_name", CurrentWebsite.Name);
            template_helper.Parameters.Add("contactus_field_name", model.Name);
            template_helper.Parameters.Add("contactus_field_email", model.Email);
            template_helper.Parameters.Add("contactus_field_website", model.Website);
            template_helper.Parameters.Add("contactus_field_phone", model.Phone);
            template_helper.Parameters.Add("contactus_field_message", model.Comment);
            template_helper.Sender_Email = CurrentWebsite.Email_Support;
            template_helper.Sender_Name = CurrentWebsite.Name;
            template_helper.Receiver.Add(model.Email);
            SendMail(template_helper);

            // send emailto notify admin
            template = Get_MaillingListTemplate("contact_us___admin___notify");
            template_helper = new EmailHelper(template.Title, template.Body);
            template_helper.Parameters.Add("website_name", CurrentWebsite.Name);
            template_helper.Parameters.Add("contactus_field_name", model.Name);
            template_helper.Parameters.Add("contactus_field_email", model.Email);
            template_helper.Parameters.Add("contactus_field_website", model.Website);
            template_helper.Parameters.Add("contactus_field_phone", model.Phone);
            template_helper.Parameters.Add("contactus_field_message", model.Comment);
            template_helper.Sender_Email = CurrentWebsite.Email_Contact;
            template_helper.Sender_Name = CurrentWebsite.Name;
            template_helper.Receiver.Add(CurrentWebsite.Email_Contact);
            SendMail(template_helper);
        }
    }
}