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
    public class WebsiteNewsController : WebAdminController
    {
        //
        public ActionResult Index(int cat_id)
        {
            var cat = Db.Where<Site_News_Category>(m => m.Id == cat_id).TakeFirst();
            if (cat == null)
            {
                return Redirect("/");
            }
            return View(cat);
        }

        public ActionResult List(int cat_id)
        {
            var c = Db.Where<Site_News>(m => m.CategoryId == cat_id).OrderByDescending(m => m.CreatedOn).ToList();

            var list_users = Cache_GetAllUsers();
            var cat_list = Cache_GetNewsCategory();

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

                var z2 = cat_list.Where(m => m.Id == x.CategoryId).FirstOrDefault();
                if (z2 == null)
                {
                    x.Category_Name = "Deleted Category";
                }
                else
                {
                    x.Category_Name = z2.Name;
                }
            }

            return PartialView("_List", c);
        }

        public ActionResult Add(int cat_id)
        {
            var cat = Db.Where<Site_News_Category>(m => m.Id == cat_id).TakeFirst();
            if (cat == null)
            {
                return RedirectToAction("Index", "Management");
            }

            Site_News model = new Site_News();
            model.CategoryId = cat.Id;
            model.Category_Name = cat.Name;
            model.LanguageCode = cat.LanguageCode;
            model.isNew = true;
            model.IsActive = true;

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            var model = Db.Where<Site_News>(m => m.Id == id).FirstOrDefault();
            if (model == null)
            {
                return RedirectToAction("Index", "Management");
            }

            var cat = Cache_GetNewsCategory().Where(m => m.Id == model.CategoryId).FirstOrDefault();
            if (cat == null)
            {
                model.Category_Name = "Deleted Category";
            }
            else
            {
                model.Category_Name = cat.Name;
            }

            return View("Add", model);
        }

        [ValidateInput(false)]
        public ActionResult Update(Site_News model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                ViewBag.Error = "Please enter news title";
                return View("Add", model);
            }

            if (model.PublishSchedule && model.PublishOn.CompareTo(model.UnPublishOn) > 0)
            {
                ViewBag.Error = "Please check Publish schedule date";
                return View("Add", model);
            }

            if (model.Tag != null)
            {
                // parse domain list
                var tag = model.Tag.First();
                var tokens = tag.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                model.Tag = tokens.ToList();
            }
            else
            {
                model.Tag = new List<string>();
            }

            // generate seo name
            string random = "";
            do
            {

                if (string.IsNullOrEmpty(model.SeoName))
                {
                    model.SeoName = model.Name + random;
                    model.SeoName = model.SeoName.ToSeoUrl();
                }
                else
                {
                    model.SeoName = model.SeoName.ToSeoUrl();
                }

                // check exist
                if (Db.Count<Site_News>(m => m.SeoName == model.SeoName && m.Id != model.Id) == 0)
                {
                    break;
                }

                random = "_" + random.GenerateRandomText(3);
                model.SeoName = "";
            } while (0 < 1);


            Site_News current_item = new Site_News();
            if (model.Id > 0)
            {
                var z = Db.Where<Site_News>(m => m.Id == model.Id);
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
                model.Statistic_Views = current_item.Statistic_Views;
            }

            if (FileUp != null && FileUp.Count() > 0 && FileUp.First() != null)
                model.ThumbnailFile = UploadFile(AuthenticatedUserID, User.Identity.Name, "", FileUp);
            else
                model.ThumbnailFile = current_item.ThumbnailFile;

            if (model.Id == 0)
            {
                Db.Insert<Site_News>(model);
            }
            else
            {
                Db.Update<Site_News>(model);
            }
            return RedirectToAction("Index", new { cat_id = model.CategoryId });
        }

        /// <summary>
        /// Id is user ID
        /// </summary>
        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<Site_News>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }


    }
}
