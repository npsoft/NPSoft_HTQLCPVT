using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.Common.Helpers;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;
using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    [ABRequiresAnyRole(RoleEnum.Administrator)]
    public class PriceController : WebAdminController
    {
        /// <summary>
        /// List the price 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public ActionResult Index(Enum_Price_MasterType type, long id)
        {
            string name = getMasterObjectName(type, id);
            if (string.IsNullOrEmpty(name))
            {
                // there is no master object prepresented by id
                return Redirect("/");
            }
            ViewData["type"] = type;
            ViewData["id"] = id;
            ViewData["name"] = name;

            Db.Close();
            return View();
        }

        /// <summary>
        /// Get Master Object Name
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        string getMasterObjectName(Enum_Price_MasterType type, long id)
        {
            var name = "";
            switch (type)
            {
                case Enum_Price_MasterType.ProductOption:
                    var po = Db.Select<Product_Option>(x => x.Where(m => m.Id == id).Limit(1)).FirstOrDefault();
                    if (po == null)
                    {
                        name = "";
                    }
                    else
                    {
                        name = po.Name;
                    }
                    break;
                case Enum_Price_MasterType.Product:
                case Enum_Price_MasterType.ProductShippingPrice:
                    var pp = Db.Select<Product>(x => x.Where(m => m.Id == id).Limit(1)).FirstOrDefault();
                    if (pp != null)
                    {
                        name = pp.Name;
                        // try to get the category also
                        var cat = Db.Select<Product_Category>(x => x.Where(m => m.Id == pp.CatId).Limit(1)).FirstOrDefault();
                        if (cat != null)
                        {
                            name += " / " + cat.Name;
                        }
                    }
                    break;
                default:
                    name = "";
                    break;
            }
            return name;
        }

        public ActionResult List(Enum_Price_MasterType type, long id)
        {
            List<Price> c = new List<Price>();
            c = Db.Where<Price>(x => x.MasterType == type && x.MasterId == id);
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

            Db.Close();
            return PartialView("_List", c);
        }

        public ActionResult Add(Enum_Price_MasterType type, long m_id)
        {
            Price model = new Price();
            model.Master_Name = getMasterObjectName(type, m_id);
            if (string.IsNullOrEmpty(model.Master_Name))
            {
                return Redirect("/");
            }
            model.MasterId = m_id;
            model.MasterType = type;
            return View(model);
        }

        public ActionResult Edit(int id)
        {
            var model = Db.Where<Price>(m => m.Id == id).FirstOrDefault();
            if (model == null)
                return Redirect("/");

            model.Master_Name = getMasterObjectName(model.MasterType, model.MasterId);
            if (string.IsNullOrEmpty(model.Master_Name))
            {
                return Redirect("/");
            }
            return View("Add", model);
        }

        [ValidateInput(false)]
        public ActionResult Update(Price model, IEnumerable<HttpPostedFileBase> FileUp)
        {

            if (string.IsNullOrEmpty(model.CountryCode))
            {
                ViewBag.Error = "Please enter field Country Code";

                return View("Add", model);
            }

            Price current_item = new Price();
            if (model.Id > 0)
            {
                var z = Db.Where<Price>(m => m.Id == model.Id);
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
                model.CreatedBy = CurrentUser.Id;
                model.CreatedOn = DateTime.Now;
            }
            else
            {
                model.CreatedOn = current_item.CreatedOn;
                model.CreatedBy = current_item.CreatedBy;
                model.MasterId = current_item.MasterId;
            }

            // update currency code
            var c = Db.Select<Country>(x => x.Where(m => m.Code == model.CountryCode).Limit(1)).FirstOrDefault();
            if (c != null)
            {
                model.CurrencyCode = c.CurrencyCode;
            }
            else
            {
                model.CurrencyCode = Setting_Defaultcurrency();
            }

            if (model.Id == 0)
            {
                Db.Insert<Price>(model);
            }
            else
            {
                Db.Update<Price>(model);
            }
            Db.Close();
            return RedirectToAction("Index", new { type = model.MasterType, id = model.MasterId });
        }


        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<Price>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            Db.Close();
            return Json(null, JsonRequestBehavior.AllowGet);
        }


    }
}