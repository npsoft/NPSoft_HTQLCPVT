using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.DataLayer.Models;
using System.Data;
using System.Web.Security;
using ServiceStack.Mvc;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.Sites;
using ServiceStack.Common.Web;
using PhotoBookmart.Models;

namespace PhotoBookmart.Controllers
{
    public class TopicController : BaseController
    {
        /// <summary>
        /// Show Topic Detail by input system name
        /// </summary>
        public ActionResult TopicDetail(string id)
        {
            var model = _GetTopicById(id);
            if (model == null)
            {
                throw HttpError.NotFound("Topic " + id + " not found on website " + CurrentWebsite.Name + " (id = " + CurrentWebsite.Id.ToString() + ")");
            }
            return View(model);
        }

        [ChildActionOnly]
        public ActionResult Embed(string id, bool? header = false, bool ignore_on_error = false)
        {
            var model = _GetTopicById(id);
            if (model == null)
            {
                if (ignore_on_error)
                {
                    return Content("");
                }
                else
                {
                    return PartialView("Embed_Topic_NotFound", id);
                }
            }

            if (header.HasValue && header.Value == false)
            {
                model.ContextLanguageDetail.Title = "";
                model.Name = "";
            }
            return PartialView(model);
        }

        /// <summary>
        /// Show certificate in footer area
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ChildActionOnly]
        public ActionResult FooterCertificates(string id)
        {
            var model = _GetTopicById(id);
            if (model == null)
                return PartialView("Embed_Topic_NotFound", id); // nothing to embed now
            return PartialView(model);
        }

        [ChildActionOnly]
        public ActionResult Sidebar(string id)
        {
            var model = _GetTopicById(id);
            if (model == null)
                return PartialView("Embed_Topic_NotFound", id); // nothing to embed now
            return PartialView(model);
        }

        /// <summary>
        /// Get Topic by system name
        /// </summary>
        [NonAction]
        private TopicDetail _GetTopicById(string id)
        {
            var topic = Db.Where<SiteTopic>(m => m.SystemName == id).FirstOrDefault();
            if (topic == null)
                return null;

            var model = topic.TranslateTo<TopicDetail>();
            // get the correct language
            var details = Db.Where<SiteTopicLanguage>(m => m.TopicId == topic.Id);
            var t = details.Where(m => m.LanguageId == CurrentLanguage.Id).FirstOrDefault();
            if (t == null)
            {
                t = details.Where(m => m.IsDefault).FirstOrDefault();
            }
            model.SetLanguageContext(t);
            return model;
        }
    }
}