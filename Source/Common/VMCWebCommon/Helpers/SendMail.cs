using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Threading;
using System.Web;
using System.IO;
using System.Configuration;

namespace PhotoBookmart.Common.Helpers
{
    public class SendEmail
    {
        static Thread SendMail_Thread;
        private static List<EmailHelper> EmailQueueList = new List<EmailHelper>();

        public static bool EmailValid(string email)
        {
            try
            {
                var x = new MailAddress(email);
                if (x == null)
                    return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void StartThreadSendMail()
        {
            SendMail_Thread = new Thread(new ThreadStart(SendMailbyThread));
            SendMail_Thread.Start();
        }

        public static bool SendMail(string emailto, string subject, string body, string sender = "", string sender_name = "")
        {
            EmailHelper emailQueue = new EmailHelper(subject, body);
            emailQueue.Receiver.Add(emailto);
            emailQueue.Sender_Email = sender;
            emailQueue.Sender_Name = sender_name;
            EmailQueueList.Add(emailQueue);
            return true;
        }


        public static bool SendMail(EmailHelper email, bool insertQueue = true)
        {
            if (insertQueue)
            {
                EmailQueueList.Add(email);
                return true;
            }

            string email_account = ConfigManager.ReadSetting("Email");
            string email_admin = ConfigManager.ReadSetting("EmailRecive");
            string email_sendas= ConfigManager.ReadSetting("EmailSendAs");
            if (email.Receiver == null || email.Receiver.Count == 0)
                email.Receiver.Add(email_admin);

            if (string.IsNullOrEmpty(email.Sender_Email))
            {
                email.Sender_Email = email_sendas;
            }

            if (email.Receiver == null || email.Receiver.Count == 0 || string.IsNullOrEmpty(email.Receiver.FirstOrDefault()))
            {
                email.Receiver = new List<string>();
                email.Receiver.Add(email_admin);
            }

            if (string.IsNullOrEmpty(email.Sender_Name))
            {
                email.Sender_Name = "Photobookmart";
            }

            string password_account = ConfigManager.ReadSetting("Password");

            string host = ConfigManager.ReadSetting("Host");

            int port = int.Parse(ConfigManager.ReadSetting("Port"));

            bool enablessl = Convert.ToBoolean(ConfigManager.ReadSetting("EnableSSL"));

            SmtpClient SmtpServer = new SmtpClient();
            SmtpServer.UseDefaultCredentials = false;
            SmtpServer.Credentials = new System.Net.NetworkCredential(email_account, password_account);
            SmtpServer.Port = port;
            SmtpServer.Host = host;
            SmtpServer.EnableSsl = enablessl;
            MailMessage mail = new MailMessage();

            string emailto = "";
            foreach (var s in email.Receiver)
            {
                if (emailto != "")
                    emailto += ",";
                emailto += s;
            }

            // render before send
            email.RazorRender();

            try
            {
                mail.From = new MailAddress(email.Sender_Email, email.Sender_Name, System.Text.Encoding.UTF8);
                mail.To.Add(emailto);
                mail.Subject = email.Title;
                mail.Body = email.Body;
                mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
                mail.ReplyToList.Add(email.Sender_Email);
                //mail.ReplyTo = new MailAddress(email_account);
                mail.Priority = MailPriority.High;
                mail.IsBodyHtml = true;

                // attachment
                foreach (var file_name in email.Attachment)
                {
                    if (!File.Exists(file_name))
                        continue;
                    FileStream fileStream = File.OpenRead(file_name);
                    Attachment messageAttachment = new Attachment(fileStream, Path.GetFileName(file_name));
                    mail.Attachments.Add(messageAttachment);
                }
                SmtpServer.Send(mail);
                return true;
            }
            catch (Exception) { return false; }
        }

        static void SendMailbyThread()
        {
            while (SendMail_Thread.IsAlive)
            {
                while (EmailQueueList.Count > 0)
                {
                    try
                    {
                        EmailHelper item = EmailQueueList.Where(m => !m.MailSending).FirstOrDefault();
                        if (item == null)
                            break;

                        item.MailSending = true;
                        SendMail(item, false);
                        EmailQueueList.Remove(item);
                        Thread.Sleep(2500);
                    }
                    catch (Exception ex)
                    {

                    }
                }
                Thread.Sleep(2500);
            }

        }
    }



    /// <summary>
    /// Helper for Template Rendering
    /// </summary>
    public class EmailHelper
    {
        /// <summary>
        /// Template title . This is email title
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Template body, this is email body
        /// </summary>
        public string Body { get; set; }
        /// <summary>
        /// Parameter for template rendering
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; }

        /// <summary>
        /// Template model to be passed into the view to render
        /// </summary>
        public object TemplateModel { get; set; }

        /// <summary>
        /// Sender email address
        /// </summary>
        public string Sender_Email { get; set; }
        /// <summary>
        /// Sender Name
        /// </summary>
        public string Sender_Name { get; set; }
        /// <summary>
        /// List of receivers email
        /// </summary>
        public List<string> Receiver { get; set; }
        /// <summary>
        /// List of absolute path for attachment files
        /// </summary>
        public List<string> Attachment { get; set; }

        /// <summary>
        /// =True if mail in sending
        /// </summary>
        public bool MailSending { get; set; }

        public EmailHelper(string title, string body)
        {
            Parameters = new Dictionary<string, string>();
            Title = title;
            Body = body;
            Attachment = new List<string>();
            Receiver = new List<string>();
            MailSending = false;
            TemplateModel = null;
        }

        /// <summary>
        /// Render Razor before replace by token
        /// </summary>
        public void RazorRender()
        {
            if (TemplateModel == null)
            {
                Body = RenderRazorToString(Body);
                Title = RenderRazorToString(Title);
            }
            else
            {
                Body = RenderRazorToString(Body, TemplateModel);
                Title = RenderRazorToString(Title, TemplateModel);
            }

            var url = ConfigurationManager.AppSettings.Get("PaypalWebsiteURL");
            Body = Body.Replace("href=\"/", "href=\"" + url);
            Body = Body.Replace("src=\"/", "src=\"" + url);
            Body = Body.Replace("href='/", "href='" + url);
            Body = Body.Replace("src='/", "src='" + url);
        }

        /// <summary>
        /// Render Razor template based on input template
        /// </summary>
        /// <returns></returns>
        public static string RenderRazorToString(string template, object model)
        {
            template = HttpUtility.HtmlDecode(template);
            //var ret = RazorEngine.Razor.Parse<T>(template, model);
            var ret = RazorEngine.Razor.Parse(template, model);
            return ret;
        }

        public static string RenderRazorToString(string template)
        {
            template = HttpUtility.HtmlDecode(template);
            var ret = RazorEngine.Razor.Parse(template);
            return ret;
        }


    }
}
