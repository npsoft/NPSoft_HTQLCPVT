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
using C4CChurchReality.Areas.Administration.Controllers;
using ABSoft.DataLayer.Models.Distributors;
using ABSoft.DataLayer.Models.Users_Management;

namespace C4CChurchReality.Areas.Administration.Controllers
{
    [Authenticate]
    [RequiredRole("Administrator")]
    public class DistributorController : RealBonusWebAdminController
    {
        //
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult List()
        {
            List<Distributor> c = Db.Select<Distributor>();

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

        //[Authorize(Roles = "Administrator,Manager")]
        public ActionResult Add()
        {
            Distributor model = new Distributor();
            return View(model);
        }

        //[Authorize(Roles = "Administrator,Manager")]
        [Authenticate]
        public ActionResult Edit(Int64 id)
        {
            var models = Db.Where<Distributor>(m => m.Id == id);
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
        public ActionResult Update(Distributor model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return JsonError("Please enter Distributor name");
            }
            if (string.IsNullOrEmpty(model.Email))
            {
                model.Email = "";
            }
           
            if (string.IsNullOrEmpty(model.Website))
            {
                model.Website = "";
            }

            if (string.IsNullOrEmpty(model.DeputyName))
            {
                model.DeputyName = "";
            }

            if (string.IsNullOrEmpty(model.Phone))
            {
                model.Phone = "";
            }

            if (string.IsNullOrEmpty(model.Website))
            {
                model.Website = "";
            }

            if (string.IsNullOrEmpty(model.Address))
            {
                model.Address = "";
            }

            if (model.Region == null)
            {
                model.Region = new List<string>();
                model.Region.Add("");
            }

            var region = model.Region.First();
            var tokens = region.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            model.Region = tokens.ToList();

            Distributor current_item = new Distributor();
            if (model.Id > 0)
            {
                var z = Db.Where<Distributor>(m => m.Id == model.Id);
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

            if (string.IsNullOrEmpty(model.Phone))
            {
                model.Phone = "";
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
                Db.Insert<Distributor>(model);
            }
            else
            {
                Db.Update<Distributor>(model);
            }

            return JsonSuccess(Url.Action("Index"));
        }

        [RequiredRole("Administrator")]
        public ActionResult deleteUser(int id)
        {
            try
            {
                Db.DeleteById<Distributor>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }
       
    }
}
