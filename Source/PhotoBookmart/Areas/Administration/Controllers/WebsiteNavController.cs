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
using ServiceStack;
using ServiceStack.Common;
using ServiceStack.ServiceInterface;
using PhotoBookmart.Areas.Administration.Controllers;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.Sites;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    [ABRequiresAnyRole(RoleEnum.Administrator)]
    public class WebsiteNavController : WebAdminController
    {
        //
        public ActionResult Index()
        {
            var site = Cache_GetWebSite();
            if (site == null)
            {
                return Redirect("/");
            }

            // get all langs belongs to this site by inner join
            //JoinSqlBuilder<Language, Language> jn = new JoinSqlBuilder<Language, Language>();
            //jn = jn.Join<Language, Site_Lang_Dis>(m => m.Id, k => k.LanguageId);
            //jn.Where<Site_Lang_Dis>(m => m.SiteId == site.Id);
            //var sql = jn.ToSql();
            //var langs = Db.Select<Language>(sql);
            var langs = Db.Where<Language>(m => m.Status);

            ViewData["Langs"] = langs;

            return View(site);
        }

        public ActionResult List(int lang_id)
        {

            List<Navigation> c = new List<Navigation>();

            var lang = Cache_GetAllLanguage().Where(m => m.Id == lang_id).FirstOrDefault();
            if (lang != null)
            {
                c = Db.Where<Navigation>(m => m.LanguageName == lang.LanguageCode);
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

            return PartialView("_List", c);
        }

        public ActionResult Add( int lang_id)
        {
            Navigation model = new Navigation();

            var lang = Cache_GetAllLanguage().Where(m => m.Id == lang_id).FirstOrDefault();
            if (lang == null)
                return RedirectToAction("Index", "Management");
            model.LanguageName = lang.LanguageCode;

            ViewData["LangName"] = lang.LanguageName;

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            var model = Db.Where<Navigation>(m => m.Id == id).FirstOrDefault();
            if (model == null)
                return Redirect("/");

            var lang = Cache_GetAllLanguage().Where(m => m.LanguageCode == model.LanguageName).FirstOrDefault();
            if (lang == null)
                return RedirectToAction("Index", "Management");
            model.LanguageName = lang.LanguageCode;
            ViewData["LangName"] = lang.LanguageName;
            return View("Add", model);
        }

        public ActionResult Update(Navigation model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return JsonError("Please enter navigation name");
            }

            if (string.IsNullOrEmpty(model.Link) && model.Menutype == 0)
            {
                return JsonError("Please enter external url");
            }

            Navigation current_item = new Navigation();
            if (model.Id > 0)
            {
                var z = Db.Where<Navigation>(m => m.Id == model.Id);
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

                // set Order Menu
                try
                {
                    int OrderMenu = Db.Where<Navigation>(m => m.LanguageName == model.LanguageName && m.ParentId == model.ParentId).Max(m => m.OrderMenu);
                    model.OrderMenu = OrderMenu + 1;
                }
                catch
                {
                    model.OrderMenu = 0;
                }
            }
            else
            {
                model.CreatedOn = current_item.CreatedOn;
                model.CreatedBy = current_item.CreatedBy;
            }

            if (model.Id == 0)
            {
                Db.Insert<Navigation>(model);
                return JsonSuccess(Url.Action("Index"), "Navigation added");
            }
            else
            {
                Db.Update<Navigation>(model);
                return JsonSuccess(Url.Action("Index"), "Navigation updated");
            }
        }

        public ActionResult Move(int id, int direction)
        {
            try
            {
                var entity = Db.Where<Navigation>(m => m.Id == id).FirstOrDefault();
                var a = new List<Navigation>();
                var temp = new Navigation();

                // get the nearest
                if (direction == 1) // down
                {
                    a = Db.Where<Navigation>(m => m.LanguageName == entity.LanguageName && m.OrderMenu < entity.OrderMenu).OrderBy(m => m.OrderMenu).ToList();
                    if (a.Count() > 0)
                        temp = a.LastOrDefault();
                }
                else
                {
                    a = Db.Where<Navigation>(m => m.OrderMenu > entity.OrderMenu && m.LanguageName == entity.LanguageName).OrderBy(m => m.OrderMenu).ToList();
                    if (a.Count() > 0)
                        temp = a.FirstOrDefault();
                }

                if (temp.Id > 0)
                {
                    int t = temp.OrderMenu;
                    temp.OrderMenu = entity.OrderMenu;
                    entity.OrderMenu = t;
                    Db.Update<Navigation>(temp);
                    Db.Update<Navigation>(entity);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return JsonSuccess("", "Menu order changed");
        }

        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<Navigation>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }


    }
}
