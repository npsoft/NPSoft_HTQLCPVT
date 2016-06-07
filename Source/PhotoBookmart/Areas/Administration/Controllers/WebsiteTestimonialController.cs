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
    public class WebsiteTestimonialController : WebAdminController
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

            // get all langs belongs to this site by inner join
            //JoinSqlBuilder<Language, Language> jn = new JoinSqlBuilder<Language, Language>();
            //jn = jn.Join<Language, Site_Lang_Dis>(m => m.Id, k => k.LanguageId);
            //jn.Where<Site_Lang_Dis>(m => m.SiteId == model.Id);
            //var sql = jn.ToSql();
            //var langs = Db.Select<Language>(sql);

            // we only need to get sites with enabled
            var langs = Db.Where<Language>(m => m.Status);

            ViewData["Langs"] = langs;

            return View(model);
        }


        public ActionResult List(int lang_id, DateTime? FromDate, DateTime? ToDate, string term)
        {
            List<Testimonial> c = new List<Testimonial>();

            var site = Cache_GetWebSite();

            var lang = Cache_GetAllLanguage().Where(m => m.Id == lang_id).FirstOrDefault();

            if (lang == null)
            {
                return Redirect("/");
            }

            var p = PredicateBuilder.True<Testimonial>();
            p = p.And(m => m.LanguageCode == lang.LanguageCode);

            if (FromDate.HasValue)
            {
                p = p.And(m => m.SubmitOn >= FromDate.Value || m.SubmitOn >= FromDate.Value);
            }

            if (ToDate.HasValue)
            {
                p = p.And(m => m.SubmitOn <= ToDate.Value || m.SubmitOn <= ToDate.Value);
            }

            if (!string.IsNullOrEmpty(term))
            {
                var x = term.ToLower();
                p = p.And(m => m.Comment.ToLower().Contains(x) || m.Name.ToLower().Contains(x) || m.Email.ToLower().Contains(x) || m.Phone.ToLower().Contains(x));
            }

            c = Db.Where<Testimonial>(p);

            ViewData["Lang_name"] = "";
            if (lang != null)
            {
                ViewData["Lang_name"] = lang.LanguageName;
            }

            return PartialView("_List", c);
        }


        public ActionResult Add(int lang_id)
        {
            Testimonial model = new Testimonial();

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
            Testimonial model = Db.Where<Testimonial>(m => m.Id == id).FirstOrDefault();

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
        public ActionResult Update(Testimonial model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                ViewBag.Error = "Please enter testimonial name";
                return View("Add", model);
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
                if (Db.Count<Testimonial>(m => m.SeoName == model.SeoName && m.Id != model.Id) == 0)
                {
                    break;
                }

                random = "_" + random.GenerateRandomText(3);
                model.SeoName = "";
            } while (0 < 1);

            Testimonial current_item = new Testimonial();
            if (model.Id > 0)
            {
                var z = Db.Where<Testimonial>(m => m.Id == model.Id);
                if (z.Count == 0)
                {
                    // the ID is not exist
                    ViewBag.Error = "Please dont try to hack us";
                    return View("Add", model);
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

            if (FileUp != null && FileUp.Count() > 0 && FileUp.First() != null)
                model.ThumbnailFilename = UploadFile(AuthenticatedUserID, User.Identity.Name, "", FileUp);


            if (model.Id == 0)
            {
                model.SubmitOn = DateTime.Now;
            }
            else
            {
                model.SubmitOn = current_item.SubmitOn;
            }

            if (model.Id == 0)
            {
                Db.Insert<Testimonial>(model);
            }
            else
            {
                Db.Update<Testimonial>(model);
            }

            return RedirectToAction("Index");
        }
    }
}
