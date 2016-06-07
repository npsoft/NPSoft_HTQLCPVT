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
    [ABRequiresAnyRole(RoleEnum.Administrator)]
    public class WebsiteSocialController : WebAdminController
    {
        //
        public ActionResult Index()
        {
            Website model = new Website();

            model = Cache_GetWebSite();
            if (model == null)
            {
                return Redirect("/");
            }

            return View(model);
        }


        public ActionResult List()
        {
            var c = Db.Select<SocialAccount>();

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

            // now we need to 

            return PartialView("_List", c);
        }


        public ActionResult Add()
        {
            SocialAccount model = new SocialAccount();

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            SocialAccount model = Db.Where<SocialAccount>(m => m.Id == id).FirstOrDefault();

            return View("Add", model);
        }


        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Update(SocialAccount model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (string.IsNullOrEmpty(model.URL))
            {
                return JsonError("Please enter URL");
            }

            if (string.IsNullOrEmpty(model.AccountId))
            {
                model.AccountId = "";
            }

            if (string.IsNullOrEmpty(model.AccountPassword))
            {
                model.AccountPassword = "";
            }

            SocialAccount current_item = new SocialAccount();
            if (model.Id > 0)
            {
                var z = Db.Where<SocialAccount>(m => m.Id == model.Id);
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
                Db.Insert<SocialAccount>(model);
            }
            else
            {
                Db.Update<SocialAccount>(model);
            }

            return JsonSuccess(Url.Action("Index"));
        }

        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<SocialAccount>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

    }
}
