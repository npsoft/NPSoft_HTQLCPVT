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
using ServiceStack.ServiceInterface;
using PhotoBookmart.Areas.Administration.Controllers;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.Sites;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    public class WebsiteMaillinglistTemplateController : WebAdminController
    {
        public ActionResult Index()
        {
            Website model = Cache_GetWebSite();
            return View(model);
        }

        public ActionResult List()
        {
            List<Site_MaillingListTemplate> c = new List<Site_MaillingListTemplate>();
            c = Db.Where<Site_MaillingListTemplate>(m => m.Status);

            var list_users = Cache_GetAllUsers();

            foreach (var x in c)
            {
                var z = list_users.Where(m => m.Id == x.CreatedBy);
                if (z.Count() > 0)
                {
                    var k = z.First();
                    if (string.IsNullOrEmpty(k.FullName))
                        x.CreatedByUsername = k.UserName;
                    else
                        x.CreatedByUsername = k.FullName;
                }
                else
                {
                    x.CreatedByUsername = "Deleted user";
                }
            }
            return PartialView("_List", c);
        }

        public ActionResult Add()
        {
            Site_MaillingListTemplate model = new Site_MaillingListTemplate();
            return View(model);
        }

        [Authenticate]
        public ActionResult Edit(int id)
        {
            var models = Db.Where<Site_MaillingListTemplate>(m => m.Id == id);
            if (models.Count == 0)
            {
                return RedirectToAction("Index");
            }
            else
            {
                var model = models.First();

                return View("Add", model);
            }
        }

        [ValidateInput(false)]
        public ActionResult Update(Site_MaillingListTemplate model, IEnumerable<HttpPostedFileBase> FileUp, string IsPublic, string Status)
        {
            model.IsPublic = IsPublic != null ? true : false;
            model.Status = Status != null ? true : false;

            if (string.IsNullOrEmpty(model.Name))
            {
                return JsonError("Please enter template name");
            }

            if (string.IsNullOrEmpty(model.Body))
            {
                return JsonError("Please enter template body");
            }


            Site_MaillingListTemplate current_item = new Site_MaillingListTemplate();
            if (model.Id > 0)
            {
                var z = Db.Where<Site_MaillingListTemplate>(m => m.Id == model.Id);
                if (z.Count == 0)
                {
                    // the ID is not exist
                    return JsonError("Please dont try to hack us");
                }
                else
                {
                    current_item = z.First();
                }
            }

            // generate seo name
            string random = "";
            do
            {

                if (string.IsNullOrEmpty(model.Systemname))
                {
                    model.Systemname = model.Name + random;
                    model.Systemname = model.Systemname.ToSeoUrl();
                }
                else
                {
                    model.Systemname = model.Systemname.ToSeoUrl();
                }

                // check exist
                if (Db.Count<Site_MaillingListTemplate>(m => m.Systemname == model.Systemname && m.Id != model.Id) == 0)
                {
                    break;
                }

                random = "_" + random.GenerateRandomText(3);
                model.Systemname = "";
            } while (0 < 1);

            if (model.Id == 0)
            {
                model.CreatedOn = DateTime.Now;
                model.CreatedBy = AuthenticatedUserID;
            }
            else
            {
                model.CreatedOn = current_item.CreatedOn;
                model.CreatedBy = current_item.CreatedBy;
            }

            if (model.Id == 0)
            {
                Db.Insert<Site_MaillingListTemplate>(model);
            }
            else
            {
                Db.Update<Site_MaillingListTemplate>(model);
            }

            return JsonSuccess(Url.Action("Index"));
        }

        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<Site_MaillingListTemplate>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }
    }
}