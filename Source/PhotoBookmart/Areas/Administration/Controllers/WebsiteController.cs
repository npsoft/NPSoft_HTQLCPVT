using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.Areas.Administration.Controllers;
using PhotoBookmart.Common.Helpers;
using System.IO;
using PhotoBookmart.Controllers;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.Sites;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    [ABRequiresAnyRole(RoleEnum.Administrator)]
    public class WebsiteController : WebAdminController
    {
        //
        public ActionResult Index()
        {
            return RedirectToAction("Detail");
        }

        [NonAction]
        public ActionResult Index2()
        {
            return View("Index");
        }

        [NonAction]
        public ActionResult List()
        {
            List<Website> c = new List<Website>();


            c = Db.Select<Website>();

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

        [NonAction]
        public ActionResult Add()
        {
            Website model = new Website();

            // theme
            var theme = Db.Select<Theme>();
            ViewData["Themes"] = theme;

            return View(model);
        }

        public ActionResult Edit(int id)
        {

            // theme
            var theme = Db.Select<Theme>();
            ViewData["Themes"] = theme;

            var models = Db.Where<Website>(m => m.Id == id);
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

        public ActionResult Update(Website model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return JsonError("Please enter website name");
            }

            if (string.IsNullOrEmpty(model.SiteTitle))
            {
                return JsonError("Please enter website Site Title");
            }

            if (model.Domain == null || model.Domain.Count == 0)
            {
                return JsonError("Please enter at least one domain for this website");
            }

            if (string.IsNullOrEmpty(model.SiteDefaultKeyword))
            {
                model.SiteDefaultKeyword = "";
            }
            if (string.IsNullOrEmpty(model.SiteDefaultMeta))
            {
                model.SiteDefaultMeta = "";
            }

            // parse domain list
            var domain = model.Domain.First();
            var tokens = domain.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            model.Domain = tokens.ToList();

            Website current_item = new Website();
            if (model.Id > 0)
            {
                var z = Db.Where<Website>(m => m.Id == model.Id);
                if (z.Count == 0)
                {
                    // the ID is not exist
                    return JsonError("Please don't try to hack us");
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
                Db.Insert<Website>(model);
            }
            else
            {
                Db.Update<Website>(model);
            }

            return JsonSuccess(Url.Action("Index"));
        }

        [RequiredRole("Administrator")]
        public ActionResult Delete(long id)
        {
            try
            {
                Db.DeleteById<Website>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Detail(long? id)
        {
            long site_id = 0;
            if (id.HasValue)
            {
                site_id = id.Value;
            }
            else
            {
                var tx = Db.Select<Website>(x => x.Limit(1)).FirstOrDefault();
                if (tx != null)
                {
                    site_id = tx.Id;
                }
            }

            var site = Db.Where<Website>(m => m.Id == site_id).FirstOrDefault();
            if (site == null)
                return Redirect("/");


            // created by username
            var list_users = Cache.Get<List<ABUserAuth>>("AB_List_ListUsers");
            if (list_users == null)
            {
                list_users = Db.Select<ABUserAuth>().ToList();
                Cache.Add<List<ABUserAuth>>("AB_List_ListUsers", list_users, TimeSpan.FromMinutes(10));
            }
            var zk = list_users.Where(m => m.Id == site.CreatedBy).FirstOrDefault();
            if (zk == null)
            {
                site.CreatedByUsername = "Deleted User";
            }
            else
            {
                if (string.IsNullOrEmpty(zk.FullName))
                    site.CreatedByUsername = zk.UserName;
                else
                    site.CreatedByUsername = zk.FullName;
            }

            return View(site);
        }


        #region Detail Membership Group
        /// <param name="id">Site ID</param>
        /// <returns></returns>
        public ActionResult Detail_Group_List(int id)
        {
            var c = Db.Select<Site_MemberGroup>();

            // created by username
            var list_users = Cache_GetAllUsers();

            foreach (var x in c)
            {
                var z = list_users.Where(m => m.Id == x.CreatedBy);
                if (z.Count() > 0)
                {
                    var k = z.First();
                    if (string.IsNullOrEmpty(k.FullName))
                        x.CreatedByUserName = k.UserName;
                    else
                        x.CreatedByUserName = k.FullName;
                }
                else
                {
                    x.CreatedByUserName = "Deleted user";
                }
            }

            return PartialView(c);
        }

        public ActionResult Detail_Group_Add(Site_MemberGroup model)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return JsonError("Please enter group name");
            }
            var curent_item = new Site_MemberGroup();
            if (model.Id > 0)
            {
                curent_item = Db.Where<Site_MemberGroup>(m => m.Id == model.Id).FirstOrDefault();
                if (curent_item == null)
                {
                    return JsonError("Please dont try to hack us");
                }
            }
            else
            {
                curent_item.CreatedBy = AuthenticatedUserID;
                curent_item.CreatedOn = DateTime.Now;
            }

            curent_item.Status = model.Status;
            curent_item.Name = model.Name;

            if (model.Id > 0)
            {
                Db.Update<Site_MemberGroup>(curent_item);
                return JsonSuccess("", "Group updated");
            }
            else
            {
                Db.Insert<Site_MemberGroup>(curent_item);
                return JsonSuccess("", "Group added");
            }

        }

        public ActionResult Detail_Group_Delete(int id)
        {
            try
            {
                Db.DeleteById<Site_MemberGroup>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Detail Analytic Chart
        public ActionResult Detail_AnalyticChart(int id)
        {
            return PartialView(id);
        }

        public ActionResult Detail_AnalyticChart_Data(int id)
        {
            var model = new List<GoogleAnalyticResultModel>();

            return Json(model);
        }
        #endregion

        #region Detail Site News Category
        public ActionResult Detail_NewsCategory_List(string lang_id)
        {
            var c = new List<Site_News_Category>();

            var lang = Cache_GetAllLanguage().Where(m => m.LanguageCode == lang_id).FirstOrDefault();

            if (lang == null)
            {
                return Content("");
            }

            c = Db.Where<Site_News_Category>(m => m.LanguageCode == lang.LanguageCode);
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
            return PartialView(c);
        }

        public ActionResult Detail_NewsCategory_Add(Site_News_Category model, string LanguageCode)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return JsonError("Please enter News Category name");
            }

            if (string.IsNullOrEmpty(LanguageCode))
            {
                return JsonError("Please enter language code");
            }

            var x = Db.Where<Language>(m => m.LanguageCode == LanguageCode).FirstOrDefault();
            if (x == null)
            {
                return JsonError("Your language code is not valid");
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
                if (Db.Count<Site_News_Category>(m => m.SeoName == model.SeoName && m.Id != model.Id) == 0)
                {
                    break;
                }

                random = "_" + random.GenerateRandomText(3);
                model.SeoName = "";
            } while (0 < 1);

            var curent_item = new Site_News_Category();
            if (model.Id > 0)
            {
                curent_item = Db.Where<Site_News_Category>(m => m.Id == model.Id).FirstOrDefault();
                if (curent_item == null)
                {
                    return JsonError("Please dont try to hack us");
                }
            }
            else
            {
                curent_item.CreatedBy = AuthenticatedUserID;
                curent_item.CreatedOn = DateTime.Now;
            }

            curent_item.Status = model.Status;
            curent_item.Name = model.Name;
            curent_item.SeoName = model.SeoName;
            curent_item.LanguageCode = LanguageCode;

            if (model.Id > 0)
            {
                Db.Update<Site_News_Category>(curent_item);
                return JsonSuccess("", "News category updated");
            }
            else
            {
                Db.Insert<Site_News_Category>(curent_item);
                return JsonSuccess("", "News category added");
            }

        }

        public ActionResult Detail_NewsCategory_Delete(int id)
        {
            try
            {
                Db.DeleteById<Site_News_Category>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }
        #endregion

        /// <summary>
        /// Show Contact us update info
        /// </summary>
        /// <returns></returns>
        public ActionResult ContactUsInfo(long? country_code)
        {
            var countries = Db.Select<Country>();
            ViewData["Countries"] = countries;

            var country = new Country();
            if (country_code.HasValue)
            {
                country = countries.Where(m => m.Id == country_code.Value).FirstOrDefault();
            }
            else
            {
                country = countries.FirstOrDefault();
            }
            if (country == null)
            {
                country = new Country();
            }

            var info = Db.Select<Site_ContactusConfig>(x=>x.Where(m => m.LanguageCode == country.Code).Limit(1)).FirstOrDefault();
            if (info == null)
            {
                info = new Site_ContactusConfig() { Address = "", Coor_Lat = 37.996163f, Coor_Lng = -99.711914f, Center_Lat = 37.991163f, Center_Lng = -99.211914f, Info = "", AllowRouting = true, Id = 0, LanguageCode = country.Code };
            }
            return View("ContactUsInfo", info);
        }

        [ValidateInput(false)]
        public ActionResult ContactUsInfoUpdate(Site_ContactusConfig model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (string.IsNullOrEmpty(model.Address))
            {
                return JsonError("Please enter website Address location");
            }

            if (string.IsNullOrEmpty(model.Info))
            {
                model.Info = "";
            }

            if (model.Id == 0)
            {
                Db.Insert<Site_ContactusConfig>(model);
            }
            else
            {
                Db.Update<Site_ContactusConfig>(model);
            }

            return JsonSuccess(Url.Action("Detail", new { id = 1 }));
        }
    }
}