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
    [Authenticate]
    [RequiredRole("Administrator")]
    public class CountryController : WebAdminController
    {
        //
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult List()
        {
            List<Country> c = new List<Country>();
            c = Db.Select<Country>();
            return PartialView("_List", c);
        }

        //[Authorize(Roles = "Administrator,Manager")]
        public ActionResult Add()
        {
            Country model = new Country();
            model.Status = true;

            return View(model);
        }

        //[Authorize(Roles = "Administrator,Manager")]
        [Authenticate]
        public ActionResult Edit(Int64 id)
        {
            var models = Db.Where<Country>(m => m.Id == id);
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

        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost]
        public ActionResult Update(Country model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return JsonError("Please enter country name");
            }

            if (string.IsNullOrEmpty(model.Code))
            {
                return JsonError("Please enter country code");
            }

            if (model.Domains == null)
            {
                model.Domains = new List<string>();
            }
            // parse domain list
            var domain = model.Domains.First();
            var tokens = domain.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            model.Domains = tokens.ToList();

            Country current_item = new Country();
            if (model.Id > 0)
            {
                var z = Db.Where<Country>(m => m.Id == model.Id);
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

            model.Code = model.Code.ToUpper();
            model.CurrencyCode = model.CurrencyCode.ToUpper();

            if (model.Id == 0)
            {
                Db.Insert<Country>(model);
            }
            else
            {
                Db.Update<Country>(model);
            }

            return JsonSuccess(Url.Action("Index"));
        }

        [RequiredRole("Administrator")]
        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<Country>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }
    }
}
