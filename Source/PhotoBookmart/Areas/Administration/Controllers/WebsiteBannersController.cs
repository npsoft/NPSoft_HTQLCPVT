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
    public class WebsiteBannersController : WebAdminController
    {
        public ActionResult Index()
        {
            Website model = Cache_GetWebSite();
            if (model == null)
            {
                return Redirect("/");
            }

            //// get all langs belongs to this site by inner join
            //JoinSqlBuilder<Language, Language> jn = new JoinSqlBuilder<Language, Language>();
            //jn = jn.Join<Language, Site_Lang_Dis>(m => m.Id, k => k.LanguageId);
            //jn.Where<Site_Lang_Dis>(m => m.SiteId == model.Id);
            //var sql = jn.ToSql();
            //var langs = Db.Select<Language>(sql);

            var langs = Db.Where<Language>(m => m.Status);

            ViewData["Langs"] = langs;

            return View(model);
        }

        public ActionResult List(int lang_id)
        {
            var c = new List<Site_Banner>();
            var site = Cache_GetWebSite();

            var lang = Cache_GetAllLanguage().Where(m => m.Id == lang_id).FirstOrDefault();
            if (lang != null)
            {
                c = Db.Where<Site_Banner>(m => m.LanguageCode == lang.LanguageCode).OrderBy(m => m.BannerIndex).ToList();
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
            Site_Banner model = new Site_Banner();

            var lang = Cache_GetAllLanguage().Where(m => m.Id == lang_id).FirstOrDefault();
            if (lang == null)
                return RedirectToAction("Index", "Management");
            model.LanguageCode = lang.LanguageCode;
            ViewData["LangName"] = lang.LanguageName;

            // get all langs belongs to this site by inner join
            //JoinSqlBuilder<Language, Language> jn = new JoinSqlBuilder<Language, Language>();
            //jn = jn.Join<Language, Site_Lang_Dis>(m => m.Id, k => k.LanguageId);
            //jn.Where<Site_Lang_Dis>(m => m.SiteId == site_id);
            //var sql = jn.ToSql();
            //var langs = Db.Select<Language>(sql);

            var langs = Db.Where<Language>(m => m.Status);

            ViewData["Langs"] = langs;

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            Site_Banner model = Db.Where<Site_Banner>(m => m.Id == id).FirstOrDefault();

            var lang = Cache_GetAllLanguage().Where(m => m.LanguageCode == model.LanguageCode).FirstOrDefault();
            if (lang == null)
                return RedirectToAction("Index", "Management");
            model.LanguageCode = lang.LanguageCode;
            ViewData["LangName"] = lang.LanguageName;

            // get all langs belongs to this site by inner join
            //JoinSqlBuilder<Language, Language> jn = new JoinSqlBuilder<Language, Language>();
            //jn = jn.Join<Language, Site_Lang_Dis>(m => m.Id, k => k.LanguageId);
            //jn.Where<Site_Lang_Dis>(m => m.SiteId == site.Id);
            //var sql = jn.ToSql();
            //var langs = Db.Select<Language>(sql);
            var langs = Db.Where<Language>(m => m.Status);

            ViewData["Langs"] = langs;

            return View("Add", model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Update(Site_Banner model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                //return JsonError("Please enter banner name");
                return View("Add", model);
            }

            if (string.IsNullOrEmpty(model.Description))
            {
                model.Description = "";
            }

            Site_Banner current_item = new Site_Banner();
            if (model.Id > 0)
            {
                var z = Db.Where<Site_Banner>(m => m.Id == model.Id);
                if (z.Count == 0)
                {
                    // the ID is not exist
                    //return JsonError("Please dont try to hack us");
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
                    int OrderMenu = Db.Select<Site_Banner>().Max(m => m.BannerIndex);
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
            }

            if (FileUp != null && FileUp.Count() > 0 && FileUp.First() != null)
            {
                model.FileName = UploadFile(AuthenticatedUserID, User.Identity.Name, "Banner", FileUp);
            }


            if (model.Id == 0)
            {
                Db.Insert<Site_Banner>(model);
            }
            else
            {
                Db.Update<Site_Banner>(model);
            }

            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<Site_Banner>(id);
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
                var entity = Db.Where<Site_Banner>(m => m.Id == id).FirstOrDefault();
                var a = new List<Site_Banner>();
                var temp = new Site_Banner();

                // get the nearest
                if (direction == 1) // down
                {
                    a = Db.Where<Site_Banner>(m => m.BannerIndex < entity.BannerIndex).OrderBy(m => m.BannerIndex).ToList();
                    if (a.Count() > 0)
                        temp = a.LastOrDefault();
                }
                else
                {
                    a = Db.Where<Site_Banner>(m => m.BannerIndex > entity.BannerIndex).OrderBy(m => m.BannerIndex).ToList();
                    if (a.Count() > 0)
                        temp = a.FirstOrDefault();
                }

                if (temp.Id > 0)
                {
                    int t = temp.BannerIndex;
                    temp.BannerIndex = entity.BannerIndex;
                    entity.BannerIndex = t;
                    Db.Update<Site_Banner>(temp);
                    Db.Update<Site_Banner>(entity);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return JsonSuccess("", "Menu order changed");
        }

    }
}
