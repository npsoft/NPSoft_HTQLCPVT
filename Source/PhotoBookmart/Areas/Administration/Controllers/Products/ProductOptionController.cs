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
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    [ABRequiresAnyRole(RoleEnum.Administrator)]
    public class ProductOptionController : WebAdminController
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult List()
        {
            List<Product_Option> c = new List<Product_Option>();
            c = Db.Where<Product_Option>(true);
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
            Product_Option model = new Product_Option();
            model.Status = true;

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            var model = Db.Where<Product_Option>(m => m.Id == id).FirstOrDefault();
            if (model == null)
                return Redirect("/");

            return View("Add", model);
        }

        [ValidateInput(false)]
        public ActionResult Update(Product_Option model, IEnumerable<HttpPostedFileBase> FileUp, string Status)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                ViewBag.Error = "Please enter field \"Name\"";

                return View("Add", model);
            }

            if (string.IsNullOrEmpty(model.InternalName))
            {
                model.InternalName = model.Name;
            }

            Product_Option current_item = new Product_Option();
            if (model.Id > 0)
            {
                var z = Db.Where<Product_Option>(m => m.Id == model.Id);
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
            if (FileUp != null && FileUp.Count() > 0 && FileUp.First() != null)
            {
                model.Thumbnail = UploadFile(model.Id, model.Thumbnail, "", FileUp);
            }
            else
            {
                model.Thumbnail = current_item.Thumbnail;
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
                model.Code = current_item.Code;
            }

            if (model.Id == 0)
            {
                Db.Insert<Product_Option>(model);
                // generate OptionCode
                model.Id = Db.GetLastInsertId();
                model.Code = "OPT" + model.Id.ToString("00000");
                Db.Update<Product_Option>(model);
                return RedirectToAction("Index");
            }
            else
            {
                if (string.IsNullOrEmpty(model.Code))
                {
                    model.Code = "OPT" + model.Id.ToString("00000");
                }
                Db.Update<Product_Option>(model);
                return RedirectToAction("Index");
            }
        }

        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<Product_Option>(id);
                // delete all price by this productoption
                Db.Delete<Price>(x => x.Where(m => m.MasterType == Enum_Price_MasterType.ProductOption && m.MasterId == id));
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Detail(long id)
        {
            var model = Db.Where<Product_Option>(m => m.Id == id).FirstOrDefault();
            if (model == null)
                return Redirect("/");


            // created by username
            var list_users = Cache_GetAllUsers();

            var zk = list_users.Where(m => m.Id == model.CreatedBy).FirstOrDefault();
            if (zk == null)
            {
                model.CreatedByUsername = "Deleted User";
            }
            else
            {
                if (string.IsNullOrEmpty(zk.FullName))
                    model.CreatedByUsername = zk.UserName;
                else
                    model.CreatedByUsername = zk.FullName;
            }

            return View(model);
        }

    }
}