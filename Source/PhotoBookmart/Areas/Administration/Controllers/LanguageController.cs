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
    [RequiredRole("Administrator")]
    public class LanguageController : WebAdminController
    {
        //
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult List()
        {
            List<Language> c = new List<Language>();


            c = Db.Select<Language>();

            var list_users = Cache.Get<List<ABUserAuth>>("AB_List_ListUsers");
            if (list_users == null)
            {
                list_users = Db.Select<ABUserAuth>().ToList();
                Cache.Add<List<ABUserAuth>>("AB_List_ListUsers", list_users, TimeSpan.FromMinutes(10));
            }

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
            Language model = new Language();
            return View(model);
        }

        //[Authorize(Roles = "Administrator,Manager")]
        [Authenticate]
        public ActionResult Edit(Int64 id)
        {
            var models = Db.Where<Language>(m => m.Id == id);
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
        public ActionResult Update(Language model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (string.IsNullOrEmpty(model.LanguageName))
            {
                return JsonError("Please enter language name");
            }

            if (string.IsNullOrEmpty(model.LanguageCode))
            {
                return JsonError("Please enter language code");
            }


            Language current_item = new Language();
            if (model.Id > 0)
            {
                var z = Db.Where<Language>(m => m.Id == model.Id);
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
                Db.Insert<Language>(model);
            }
            else
            {
                Db.Update<Language>(model);
            }

            return JsonSuccess(Url.Action("Index"));
        }

        [RequiredRole("Administrator")]
        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<Theme>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [RequiredRole("Administrator")]
        public ActionResult Enable(int id)
        {
            try
            {
                var x = Db.Where<Theme>(m => m.Id == id);
                if (x.Count > 0)
                {
                    var y = x.First();
                    y.Disable();
                    Db.Update(y);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [RequiredRole("Administrator")]
        public ActionResult Disable(int id)
        {
            try
            {
                var x = Db.Where<Theme>(m => m.Id == id);
                if (x.Count > 0)
                {
                    var y = x.First();
                    y.Enable();
                    Db.Update(y);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [RequiredRole("Administrator")]
        public ActionResult Translation(int? lang_id )
        {
            // list of langs
            List<Language> langs = Db.Select<Language>();
            langs.Insert(0, new Language() { Id=0,LanguageName="Any language" });
            ViewData["Langs"] = langs;

            // list of sites
            List<Website> sites = Db.Select<Website>();
            sites.Insert(0, new Website() { Id = 0, Name = "Any website" });
            ViewData["Websites"] = sites;

            if (!lang_id.HasValue)
                lang_id = 0;

            ViewData["lang_id"] = lang_id;
            return View();
        }

        [RequiredRole("Administrator")]
        public ActionResult _Translation_List(int? lang_id )
        {
            var p = PredicateBuilder.True<Language_Translation>();
            bool can_predicate = false;
            if (lang_id.HasValue && lang_id.Value>0)
            {
                p = p.And(m => m.LangId == lang_id.Value);
                can_predicate = true;
            }

            List<Language_Translation> c = new List<Language_Translation>();

            if (can_predicate)
            {
                c = Db.Where<Language_Translation>(p);
            }
            else
            {
                c = Db.Select<Language_Translation>();
            }

            var list_language = Cache.Get<List<Language>>("AB_List_Languages");
            if (list_language == null)
            {
                list_language = Db.Select<Language>().ToList();
                Cache.Add<List<Language>>("AB_List_Languages", list_language, TimeSpan.FromMinutes(10));
            }

            foreach (var x in c)
            {
                if (x.LangId == 0) {
                    x.Language_Name = "Any Language";
                }
                else
                {
                    var z = list_language.Where(m => m.Id == x.LangId);
                    if (z.Count() > 0)
                    {
                        var k = z.First();
                        x.Language_Name = k.LanguageName;
                    }
                    else
                    {
                        x.Language_Name = "Unknown Language";
                    }
                }
            }

            return PartialView("_Translation_List", c);
        }

        /// <summary>
        /// Update / Add new translation
        /// </summary>
        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost]
        public ActionResult _Translation_Update(Language_Translation model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (string.IsNullOrEmpty(model.Key))
            {
                return JsonError("Please enter translation Key");
            }

            if (string.IsNullOrEmpty(model.Value))
            {
                return JsonError("Please enter translation Content");
            }
            // check dupplication

            model.Key = model.Key.ToLower();

            var x = Db.Where<Language_Translation>(m => m.Id!=model.Id && m.LangId == model.LangId && m.LangId == model.LangId && m.Key == model.Key);
            if (x.Count > 0 )
            {
                return JsonError("The key you entered has been existed. Please use difference key");
            }

            Language_Translation current_item = new Language_Translation();
            if (model.Id > 0)
            {
                var z = Db.Where<Language_Translation>(m => m.Id == model.Id);
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
                Db.Insert<Language_Translation>(model);
                return JsonSuccess("", "New translation added");
            }
            else
            {
                Db.Update<Language_Translation>(model);
                return JsonSuccess("", "Translation updated");
            }
        }


        public ActionResult _Translation_Delete(int id)
        {
            try
            {
                Db.DeleteById<Language_Translation>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }
    }
}
