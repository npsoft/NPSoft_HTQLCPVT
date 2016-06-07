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
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PhotoBookmart.DataLayer.Models.ExtraShipping;
using PhotoBookmart.DataLayer.Models.Sites;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    [ABRequiresAnyRole(RoleEnum.Administrator, RoleEnum.OrderManagement)]
    public class ExtraShippingController : WebAdminController
    {
        #region [CONTROLLER] ExtraShipping

        public ActionResult Index(int? country_id)
        {
            Country country = new Country() { Id = 0 };
            if (country_id.HasValue)
            {
                country = Db.Select<Country>(x => x.Where(m => m.Id == country_id.Value).Limit(1)).FirstOrDefault();
            }

            ViewData["Country"] = country;
            return View();
        }

        public ActionResult List(int country_id)
        {
            List<Country_State_ExtraShipping> model = new List<Country_State_ExtraShipping>();

            if (country_id > 0)
            {
                model = Db.Select<Country_State_ExtraShipping>(x => x.Where(y => (y.CountryId == country_id)));
            }
            else
            {
                model = Db.Select<Country_State_ExtraShipping>(x => x.OrderBy(w => (w.CountryId)));
            }

            var countries = Db.Select<Country>();
            foreach (Country_State_ExtraShipping item in model ?? Enumerable.Empty<Country_State_ExtraShipping>())
            {
                var country = countries.Where(x => (x.Id == item.CountryId)).FirstOrDefault();
                item.CountryName = country != null ? country.Name : "Deleted Country";
            }

            return PartialView("_List", model);
        }

        public ActionResult Add(int? country_id)
        {
            Country_State_ExtraShipping model = new Country_State_ExtraShipping() { CountryId = country_id.HasValue ? country_id.Value : 0 };

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            var models = Db.Where<Country_State_ExtraShipping>(m => m.Id == id);

            if (models.Count == 0) return RedirectToAction("Index", "Management");

            var model = models.First();
            return View("Add", model);
        }

        [HttpPost]
        public ActionResult Update(Country_State_ExtraShipping model)
        {
            if (Db.Count<Country>(x => (x.Id == model.CountryId)) == 0)
            {
                return JsonError("Please choose field » Country.");
            }
            if (string.IsNullOrEmpty(model.State))
            {
                return JsonError("Please enter field » State.");
            }
            if (model.Amount < 0)
            {
                model.Amount = 0;
            }
            Country_State_ExtraShipping current_item = new Country_State_ExtraShipping();
            if (model.Id != 0)
            {
                var z = Db.Where<Country_State_ExtraShipping>(m => m.Id == model.Id);
                if (z.Count == 0) return JsonError("Please don't try to hack us.");
                current_item = z.First();
            }
            if (Db.Count<Country_State_ExtraShipping>(x => (
                (model.Id == 0 && x.CountryId == model.CountryId && x.State == model.State) ||
                (model.Id != 0 && x.CountryId == model.CountryId && x.State == model.State && x.Id != model.Id))) != 0)
            {
                return JsonError("State » is already used.");
            }
            if (model.Id == 0)
            {
                Db.Insert<Country_State_ExtraShipping>(model);
            }
            else
            {
                Db.Update<Country_State_ExtraShipping>(model);
            }

            return JsonSuccess(Url.Action("Index", new { country_id = model.CountryId }));
        }

        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<Country_State_ExtraShipping>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}