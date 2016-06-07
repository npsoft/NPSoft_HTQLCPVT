using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using C4CChurchReality.Areas.Administration.Models;
using ABSoft.Common.Helpers;
using System.IO;
using C4CChurchReality.Controllers;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;
using TTGCMS.Areas.Administration.Controllers;
using ABSoft.DataLayer.Models.System;
using ABSoft.DataLayer.Models.Users_Management;
using ABSoft.DataLayer.Models.Sites;
using ABSoft.DataLayer.Models.Distributors;
using Google.WebmasterTools;
using Google.GData.WebmasterTools;

namespace TTGCMS.Areas.Administration.Controllers
{
    public class WebsiteGoogleWebmasterController : TTGCMSAdminController
    {
        //
        public ActionResult Index()
        {
            return Content("Not implement");
        }

        public ActionResult List()
        {
            List<Website> c = new List<Website>();


            c = Db.Select<Website>();

            var list_users = Cache_GetAllUsers();

            var list_distributor = Cache_GetAllDistrbutor();

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

                // distributors
                var zk = list_distributor.Where(m => m.Id == x.DisId);
                if (zk.Count() > 0)
                {
                    var t = zk.First();
                    x.Distributor_Name = t.Name;
                }
            }
            return PartialView("_List", c);
        }

        public ActionResult Add()
        {
            Website model = new Website();

            // get distributors
            var dis = Db.Select<Distributor>();
            ViewData["Distributors"] = dis;

            // theme
            var theme = Db.Select<Theme>();
            ViewData["Themes"] = theme;

            return View(model);
        }

        public ActionResult Edit(int id)
        {

            // get distributors
            var dis = Db.Select<Distributor>();
            ViewData["Distributors"] = dis;

            // theme
            var theme = Db.Select<Theme>();
            ViewData["Themes"] = theme;

            var models = Db.Where<Website>(m => m.Id == id);
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

        public ActionResult Update(Website model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return JsonError("Please enter website name");
            }

            if (string.IsNullOrEmpty(model.SiteTitle))
            {
                return JsonError("Please enter website Site Title");
            }

            if (model.DisId == 0)
            {
                return JsonError("Please select distributor for this web");
            }

            if (model.Domain == null || model.Domain.Count == 0)
            {
                return JsonError("Please enter at least one domain for this website");
            }

            if (string.IsNullOrEmpty(model.SiteDefaultKeyword))
            {
                model.SiteDefaultKeyword = "";
            }
            if (string.IsNullOrEmpty(model.SiteDefaultMeta))
            {
                model.SiteDefaultMeta = "";
            }


            Website current_item = new Website();
            if (model.Id > 0)
            {
                var z = Db.Where<Website>(m => m.Id == model.Id);
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
                Db.Insert<Website>(model);
            }
            else
            {
                Db.Update<Website>(model);
            }

            return JsonSuccess(Url.Action("Index"));
        }

        [RequiredRole("Administrator")]
        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<Website>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }
      
    }
}
