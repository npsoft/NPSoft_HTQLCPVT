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
    public class WebsiteFlashHeaderController : WebAdminController
    {
        public ActionResult Index()
        {
            Website model = Cache_GetWebSite();
            if (model == null)
            {
                return Redirect("/");
            }

            var langs = Db.Where<Language>(m => m.Status);

            ViewData["Langs"] = langs;

            return View(model);
        }

        public ActionResult List(int lang_id)
        {
            var c = new List<Site_FlashHeader>();
            var site = Cache_GetWebSite();

            var lang = Cache_GetAllLanguage().Where(m => m.Id == lang_id).FirstOrDefault();
            if (lang != null)
            {
                c = Db.Where<Site_FlashHeader>(m => m.LanguageCode == lang.LanguageCode).OrderBy(m => m.BannerIndex).ToList();
            }

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

            ViewData["Lang_name"] = "";
            if (lang != null)
            {
                ViewData["Lang_name"] = lang.LanguageName;
            }

            // now we need to 
            return PartialView("_List", c);
        }

        public ActionResult Add( int lang_id)
        {
            Site_FlashHeader model = new Site_FlashHeader();
            model.Status = true;
            var lang = Cache_GetAllLanguage().Where(m => m.Id == lang_id).FirstOrDefault();
            if (lang == null)
                return RedirectToAction("Index", "Management");
            model.LanguageCode = lang.LanguageCode;
            ViewData["LangName"] = lang.LanguageName;

            var langs = Db.Where<Language>(m => m.Status);

            ViewData["Langs"] = langs;

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            Site_FlashHeader model = Db.Where<Site_FlashHeader>(m => m.Id == id).FirstOrDefault();

            var lang = Cache_GetAllLanguage().Where(m => m.LanguageCode == model.LanguageCode).FirstOrDefault();
            if (lang == null)
                return RedirectToAction("Index", "Management");
            model.LanguageCode = lang.LanguageCode;
            ViewData["LangName"] = lang.LanguageName;

            var langs = Db.Where<Language>(m => m.Status);

            ViewData["Langs"] = langs;

            return View("Add", model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Update(Site_FlashHeader model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (string.IsNullOrEmpty(model.Content))
            {
                //return JsonError("Please enter banner name");
                return View("Add", model);
            }

            if (string.IsNullOrEmpty(model.LinkTo))
            {
                model.LinkTo = "";
            }

            Site_FlashHeader current_item = new Site_FlashHeader();
            if (model.Id > 0)
            {
                var z = Db.Where<Site_FlashHeader>(m => m.Id == model.Id);
                if (z.Count == 0)
                {
                    return View("Add", model);
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

                // set Order Menu
                try
                {
                    int OrderMenu = Db.Select<Site_FlashHeader>().Max(m => m.BannerIndex);
                    model.BannerIndex = OrderMenu + 1;
                }
                catch
                {
                    model.BannerIndex = 0;
                }
            }
            else
            {
                model.CreatedOn = current_item.CreatedOn;
                model.CreatedBy = current_item.CreatedBy;
                model.BannerIndex = current_item.BannerIndex;
            }

            if (model.Id == 0)
            {
                Db.Insert<Site_FlashHeader>(model);
            }
            else
            {
                Db.Update<Site_FlashHeader>(model);
            }

            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<Site_FlashHeader>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Move(int id, int direction)
        {
            try
            {
                var entity = Db.Where<Site_FlashHeader>(m => m.Id == id).FirstOrDefault();
                var a = new List<Site_FlashHeader>();
                var temp = new Site_FlashHeader();

                // get the nearest
                if (direction == 1) // down
                {
                    a = Db.Where<Site_FlashHeader>(m => m.BannerIndex < entity.BannerIndex).OrderBy(m => m.BannerIndex).ToList();
                    if (a.Count() > 0)
                        temp = a.LastOrDefault();
                }
                else
                {
                    a = Db.Where<Site_FlashHeader>(m => m.BannerIndex > entity.BannerIndex).OrderBy(m => m.BannerIndex).ToList();
                    if (a.Count() > 0)
                        temp = a.FirstOrDefault();
                }

                if (temp.Id > 0)
                {
                    int t = temp.BannerIndex;
                    temp.BannerIndex = entity.BannerIndex;
                    entity.BannerIndex = t;
                    Db.Update<Site_FlashHeader>(temp);
                    Db.Update<Site_FlashHeader>(entity);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return JsonSuccess("", "Flash Header Index Changed");
        }

    }
}
