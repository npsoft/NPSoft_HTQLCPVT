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
    public class WebsiteBlogController : WebAdminController
    {
        public ActionResult Index(int cat_id)
        {
            var cat = Db.Select<Site_Blog_Category>(x => x.Where(m => m.Id == cat_id).Limit(1)).FirstOrDefault();

            if (cat == null) return Redirect("/Administration");

            var site = Cache_GetWebSite();

            cat.SiteName = (site != null ? site.Name : "Deleted site");

            return View(cat);
        }

        public ActionResult List(int cat_id)
        {
            var c = Db.Where<Site_Blog>(m => m.CategoryId == cat_id );

            var list_users = Cache_GetAllUsers();

            var cat_list = Cache_GetBlogCategory();

            foreach (var x in c)
            {
                var z = list_users.Where(m => m.Id == x.CreatedBy);

                if (z.Count() != 0)
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
                    x.CategoryName = "Deleted Category";
                }
                else
                {
                    x.CategoryName = z2.Name;
                }
            }

            ViewData["cat_id"] = cat_id;

            return PartialView("_List", c);
        }

        public ActionResult Add(int cat_id)
        {
            var cat = Db.Where<Site_Blog_Category>(m => m.Id == cat_id).TakeFirst();

            if (cat == null) return RedirectToAction("Index", "Management");
       
            Site_Blog model = new Site_Blog();

            model.CategoryId = cat.Id;

            model.CategoryName = cat.Name;

            model.LanguageCode = cat.LanguageCode;

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            var model = Db.Where<Site_Blog>(m => m.Id == id).FirstOrDefault();

            if (model == null) return RedirectToAction("Index", "Management");

            var cat = Cache_GetBlogCategory().Where(m => m.Id == model.CategoryId).FirstOrDefault();

            model.CategoryName = (cat != null ? cat.Name : "Deleted category");
            
            return View("Add", model);
        }

        [ValidateInput(false)]
        public ActionResult Update(Site_Blog model, string PublishSchedule, string IsActive, IEnumerable<HttpPostedFileBase> FileUp)
        {
            model.PublishSchedule = (PublishSchedule != null ? true : false);
            model.IsActive = (IsActive != null ? true : false);

            if (string.IsNullOrEmpty(model.Name))
            {
                ViewBag.Error = "Please enter post name";

                return View("Add", model);
            }

            if (model.PublishSchedule && model.PublishOn.CompareTo(model.UnPublishOn) > 0)
            {
                ViewBag.Error = "Please check publish schedule date";

                return View("Add", model);
            }

            if (model.Tag != null)
            {
                var tag = model.Tag.First();

                var tokens = tag.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                model.Tag = tokens.ToList();
            }
            else
            {
                model.Tag = new List<string>();
            }

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

                if (Db.Count<Site_Blog>(m => m.SeoName == model.SeoName && m.Id != model.Id) == 0) break;

                random = "_" + random.GenerateRandomText(3);

                model.SeoName = "";

            } while (true);

            Site_Blog current_item = new Site_Blog();

            if (model.Id > 0)
            {
                var z = Db.Where<Site_Blog>(m => m.Id == model.Id);

                if (z.Count == 0)
                {
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

            if (FileUp != null && FileUp.Count() > 0 && FileUp.First() != null)

                model.ThumbnailFile = UploadFile(AuthenticatedUserID, User.Identity.Name, "", FileUp);
            else
                model.ThumbnailFile = current_item.ThumbnailFile;

            if (model.Id == 0)
            {
                Db.Insert<Site_Blog>(model);
            }
            else
            {
                Db.Update<Site_Blog>(model);
            }
            return RedirectToAction("Index", new { cat_id = model.CategoryId });
        }

        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<Site_Blog>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpGet][RequiredRole("Administrator")]
        public ActionResult SetActivePost(int id, bool active)
        {
            try
            {
                var entity = Db.Select<Site_Blog>(x => (x.Where(y => y.Id == id))).FirstOrDefault();

                if (entity != null && !entity.IsActive == active)
                {
                    entity.IsActive = !entity.IsActive;

                    Db.Update(entity);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpGet][RequiredRole("Administrator")]
        public ActionResult SetOrderPost(int id, int move)
        {
            try
            {
                var e = Db.Select<Site_Blog>(x => x.Where(y => y.Id == id)).FirstOrDefault();

                if (e != null)
                {    
                    var a = new List<Site_Blog>();

                    var t = new Site_Blog() { Id = 0 };

                    if (move == 1)
                    {
                        a = Db.Where<Site_Blog>(x => (x.CategoryId == e.CategoryId && x.Order < e.Order)).OrderBy(x => x.Order).ToList();

                        if (a.Count != 0) t = a.Last();
                    }
                    else
                    {
                        a = Db.Where<Site_Blog>(x => (x.CategoryId == e.CategoryId && x.Order > e.Order)).OrderBy(x => x.Order).ToList();

                        if (a.Count != 0) t = a.First();
                    }

                    if (t.Id > 0)
                    {
                        int tmp = t.Order;
                        t.Order = e.Order;
                        e.Order = tmp;

                        Db.Update<Site_Blog>(t);
                        Db.Update<Site_Blog>(e);
                    }
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Category()
        {
            var site = Db.Select<Website>().FirstOrDefault();

            if (site == null) return Redirect("/Administration");

            var list_users = Cache.Get<List<ABUserAuth>>("AB_List_ListUsers");

            if (list_users == null)
            {
                list_users = Db.Select<ABUserAuth>();

                Cache.Add<List<ABUserAuth>>("AB_List_ListUsers", list_users, TimeSpan.FromMinutes(10));
            }

            var zk = list_users.Where(x => x.Id == site.CreatedBy).FirstOrDefault();

            site.CreatedByUsername = zk != null ? (string.IsNullOrEmpty(zk.FullName) ? zk.UserName : zk.FullName) : "Deleted user";

            return View("Category.Blog", site);
        }

        public ActionResult Detail_BlogCategory_List(int id, string lang_id)
        {
            var c = new List<Site_Blog_Category>();

            var lang = Cache_GetAllLanguage().Where(m => m.LanguageCode == lang_id).FirstOrDefault();

            if (lang == null) return Content("");

            c = Db.Where<Site_Blog_Category>(m => m.LanguageCode == lang.LanguageCode);

            var list_users = Cache_GetAllUsers();

            foreach (var x in c)
            {
                var z = list_users.Where(m => m.Id == x.CreatedBy);

                if (z.Count() != 0)
                {
                    var k = z.First();

                    x.CreatedByUsername = (string.IsNullOrEmpty(k.FullName) ? k.UserName : k.FullName);
                }
                else
                {
                    x.CreatedByUsername = "Deleted user";
                }
            }

            ViewData["Lang_name"] = (lang != null ? lang.LanguageName : "");

            return PartialView(c);
        }

        public ActionResult Detail_BlogCategory_Add(Site_Blog_Category model, string LanguageCode, string IsActive)
        {
            model.IsActive = !string.IsNullOrEmpty(IsActive);

            if (string.IsNullOrEmpty(model.Name))
            {
                return JsonError("Please enter blog category name");
            }

            if (string.IsNullOrEmpty(LanguageCode))
            {
                return JsonError("Please enter language code");
            }

            var x = Db.Where<Language>(m => m.LanguageCode == LanguageCode).FirstOrDefault();

            if (x == null) return JsonError("Your language code is not valid");

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

                if (Db.Count<Site_Blog_Category>(m => m.SeoName == model.SeoName && m.Id != model.Id) == 0) break;

                random = "_" + random.GenerateRandomText(3);

                model.SeoName = "";

            } while (true);

            var current_item = new Site_Blog_Category();

            if (model.Id > 0)
            {
                current_item = Db.Where<Site_Blog_Category>(m => m.Id == model.Id).FirstOrDefault();

                if (current_item == null) return JsonError("Please don't try to hack us");
            }
            else
            {
                current_item.CreatedBy = AuthenticatedUserID;
                current_item.CreatedOn = DateTime.Now;
            }

            current_item.IsActive = model.IsActive;
            current_item.Name = model.Name;
            current_item.SeoName = model.SeoName;
            current_item.LanguageCode = LanguageCode;

            if (model.Id > 0)
            {
                Db.Update<Site_Blog_Category>(current_item);

                return JsonSuccess("", "Blog updated");
            }
            else
            {
                Db.Insert<Site_Blog_Category>(current_item);

                return JsonSuccess("", "Blog added");
            }
        }

        public ActionResult Detail_BlogCategory_Delete(int id)
        {
            try
            {
                Db.DeleteById<Site_Blog_Category>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }
    }
}