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
    public class WebsiteTopicController : WebAdminController
    {
        //
        public ActionResult Index()
        {
            var site = Cache_GetWebSite();
            if (site == null)
            {
                return Redirect("/");
            }
            return View(site);
        }

        public ActionResult List()
        {
            List<SiteTopic> c = new List<SiteTopic>();
            c = Db.Select<SiteTopic>();

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
            SiteTopic model = new SiteTopic();

            return View(model);
        }

        public ActionResult Detail(int id)
        {
            var model = Db.Where<SiteTopic>(m => m.Id == id).FirstOrDefault();
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            else
            {
                // get all langs belongs to this site by inner join
                //JoinSqlBuilder<Language, Language> jn = new JoinSqlBuilder<Language, Language>();
                //jn = jn.Join<Language, Site_Lang_Dis>(m => m.Id, k => k.LanguageId);
                //jn.Where<Site_Lang_Dis>(m => m.SiteId == model.SiteId);
                //var sql = jn.ToSql();
                //var langs = Db.Select<Language>(sql);

                var langs = Db.Where<Language>(m => m.Status);
                
                ViewData["Langs"] = langs;

                return View("Detail", model);
            }
        }

        public ActionResult Update(SiteTopic model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return JsonError("Please enter topic name");
            }

            // generate seo name
            string random = "";
            do
            {

                if (string.IsNullOrEmpty(model.SystemName))
                {
                    model.SystemName = model.Name + random;
                    model.SystemName = model.SystemName.ToSeoUrl();
                }
                else
                {
                    model.SystemName = model.SystemName.ToSeoUrl();
                }

                // check exist
                if (Db.Count<SiteTopic>(m => m.SystemName == model.SystemName && m.Id != model.Id) == 0)
                {
                    break;
                }

                random = "_" + random.GenerateRandomText(3);
                model.SystemName = "";
            } while (0 < 1);

            SiteTopic current_item = new SiteTopic();
            if (model.Id > 0)
            {
                var z = Db.Where<SiteTopic>(m => m.Id == model.Id);
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
            else
            {
                // generate systemname to avoid duplication
                var can_use = false;
                while (!can_use)
                {
                    var x = Db.Where<SiteTopic>(m => m.SystemName == model.SystemName).FirstOrDefault();
                    if (x == null)
                    {
                        can_use = true;
                        break;
                    }
                    else
                    {
                        model.SystemName += new Random().Next(0, 9);
                    }
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
                Db.Insert<SiteTopic>(model);
                return JsonSuccess("", "Topic added");
            }
            else
            {
                Db.Update<SiteTopic>(model);
                return JsonSuccess("", "Topic updated");
            }
        }

        [ValidateInput(false)]
        // for each topic translation form update ajax
        public ActionResult UpdateTopicLanguage(SiteTopicLanguage model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            // Note: we have to reload the form to update new ID for each translation

            // get language name
            var langs = Cache_GetAllLanguage();
            var lang = langs.Where(m => m.Id == model.LanguageId).FirstOrDefault();
            var lang_name = lang == null ? "Unknown language" : lang.LanguageName;

            // delete the translation
            if (model.IsDeleted)
            {
                if (model.Id > 0)
                {
                    try
                    {
                        if (model.IsDefault)
                        {
                            // before delete, try to update the default
                            Db.UpdateOnly<SiteTopicLanguage>(new SiteTopicLanguage() { IsDefault = false }, ev => ev.Update(p => p.IsDefault).Where(m => m.TopicId == model.TopicId));
                        }
                        // delete it
                        Db.DeleteById<SiteTopicLanguage>(model.Id);
                        if (model.IsDefault)
                        {
                            // then set default
                            var x = Db.Where<SiteTopicLanguage>(m => m.TopicId == model.TopicId).FirstOrDefault();
                            if (x != null)
                            {
                                x.IsDefault = true;
                                Db.Update<SiteTopicLanguage>(x);
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                
                if (model.Id > 0)
                {
                    return JsonSuccess(Url.Action("Detail", new { id = model.TopicId }), "Topic translation in " + lang_name + " has been deleted");
                }
                else
                {
                    return JsonSuccess(Url.Action("Detail", new { id = model.TopicId }));
                }
            }

            // or update - add new

            if (string.IsNullOrEmpty(model.Title))
            {
                return JsonError("Please enter Title in translation for topic in " + lang_name + " language");
            }

            if (string.IsNullOrEmpty(model.Body))
            {
                return JsonError("Please enter Body in translation for topic in " + lang_name + " language");
            }

            if (string.IsNullOrEmpty(model.MetaKeywords))
            {
                model.MetaKeywords = "";
            }

            if (string.IsNullOrEmpty(model.MetaDescription))
            {
                model.MetaDescription = "";
            }

            if (string.IsNullOrEmpty(model.MetaTitle))
            {
                model.MetaTitle = "";
            }

            if (string.IsNullOrEmpty(model.MoreLink))
            {
                model.MoreLink = "";
            }


            if (model.Id == 0)
            {
                Db.Insert<SiteTopicLanguage>(model);
                //if is default, then set default
                if (model.IsDefault)
                {
                    var id = (int)Db.GetLastInsertId();
                    Db.UpdateOnly<SiteTopicLanguage>(new SiteTopicLanguage() { IsDefault = false }, ev => ev.Update(p => p.IsDefault).Where(m => m.TopicId == model.TopicId));
                    Db.UpdateOnly<SiteTopicLanguage>(new SiteTopicLanguage() { IsDefault = true }, ev => ev.Update(p => p.IsDefault).Where(m => m.Id == id));
                }
                return JsonSuccess(Url.Action("Detail", new { id = model.TopicId }), "Translation in " + lang_name + " added");
            }
            else
            {
                Db.Update<SiteTopicLanguage>(model);

                Db.UpdateOnly<SiteTopicLanguage>(new SiteTopicLanguage() { IsDefault = false }, ev => ev.Update(p => p.IsDefault).Where(m => m.TopicId == model.TopicId));
                Db.UpdateOnly<SiteTopicLanguage>(new SiteTopicLanguage() { IsDefault = true }, ev => ev.Update(p => p.IsDefault).Where(m => m.Id == model.Id));

                return JsonSuccess("", "Translation in " + lang_name + " updated");
            }
        }

        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<SiteTopic>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }


    }
}
