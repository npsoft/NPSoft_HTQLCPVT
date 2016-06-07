using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.DataLayer.Models;
using System.Xml;
using System.Data;
using System.Web.Security;
using ServiceStack.Mvc;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.Products;
using ServiceStack.Common.Web;
using PhotoBookmart.Models;
using PhotoBookmart.DataLayer.Models.Sites;

namespace PhotoBookmart.Controllers
{
    public class CategoryController : BaseController
    {
        int item_per_page = 20;

        [ChildActionOnly]
        public ActionResult HomepageListings()
        {
            var listings = Db
                            .Where<ListingProperty>(m => (m.IsActive && m.Other_Featured))
                            .OrderBy(m => (m.Other_FeaturedItemOrder))
                            .ToList();

            return View("_HomepageListings", listings);
        }

        [HttpGet]
        public ActionResult Listings(string p_pro, string p_type, string p_status1, string p_status2, string p_search, int? p_page)
        {
            List<ListingProperty> model = new List<ListingProperty>();

            JoinSqlBuilder<ListingProperty, ListingProperty> jn = new JoinSqlBuilder<ListingProperty,ListingProperty>();
            jn = jn.LeftJoin<ListingProperty, ListingCategory>(x => x.Assign_Category, y => y.Id);
            jn = jn.LeftJoin<ListingProperty, ListingPropertyType>(x => x.Assign_Type, y => y.Id);

            if (p_pro != null && p_search == null)
            {
                var pro = Db.Where<ListingCategory>(x => (x.SEO == p_pro && x.IsActive));
                if (pro != null && pro.Count != 0)
                {
                    ViewData["Title"] = pro.TakeFirst().Name;
                    jn = jn.Where<ListingCategory>(x => (x.Id == pro.TakeFirst().Id));
                }
                else
                {
                    throw new HttpException(404, "Category listing not found!", 0 );
                }
            }
            else if (p_type != null && p_search == null)
            {
                var type = Db.Where<ListingPropertyType>(x => (x.SEO == p_type && x.IsActive));
                if (type != null && type.Count != 0)
                {
                    ViewData["Title"] = type.TakeFirst().Name;
                    jn = jn.Where<ListingCategory>(x => (x.Id == type.TakeFirst().Id));
                }
                else
                {
                    throw new HttpException(404, "Type listing not found!", 0);
                }
            }
            else if (p_status1 != null && p_search == null)
            {
                int result = 0;
                foreach(var e in (Status1Enum[])Enum.GetValues(typeof(Status1Enum)) ?? Enumerable.Empty<Status1Enum>())
                {
                    if (e.ToString().ToLower() == p_status1.ToLower())
                    {
                        result = (int)e;
                        break;
                    }
                }

                if (result != 0)
                {
                    ViewData["Title"] = ((Status1Enum)result).ToString();
                    jn = jn.Where<ListingProperty>(x => (x.Status1 == result));
                }
                else
                {
                    throw new HttpException(404, "Status 1 listing not found!", 0);
                }
            }
            else if (p_status2 != null && p_search == null)
            {
                int result = 0;
                foreach(var e in (Status2Enum[])Enum.GetValues(typeof(Status2Enum)) ?? Enumerable.Empty<Status2Enum>())
                {
                    if (e.ToString().ToLower() == p_status2.ToLower())
                    {
                        result = (int)e;
                        break;
                    }
                }

                if (result != 0)
                {
                    ViewData["Title"] = ((Status2Enum)result).ToString();
                    jn = jn.Where<ListingProperty>(x => (x.Status2 == result));
                }
                else
                {
                    throw new HttpException(404, "Status 2 listing not found!", 0);
                }
            }
            else if (p_search != null && p_pro != null)
            {
                ViewData["Title"] = "Search result";
                /* TODO */
            }
            else if (p_search != null/* && p_pro == null */)
            {
                ViewData["Title"] = "Search result";

                jn = jn.Where<ListingProperty>(x => (x.Info_Title.Contains(p_search)));
            }
            else
            {
                ViewData["Title"] = "All Properties";
            }

            jn = jn.Where<ListingProperty>(x => (x.IsActive && (x.IsSchedule != true || (x.IsSchedule && x.PublishSchedule <= DateTime.Now && x.UnPublishSchedule >= DateTime.Now))));
            var sql = jn.ToSql();
            
            var jn_count = jn.SelectCount<ListingProperty>(m => m.Id);

            var count = Db.Scalar<int>(jn_count.ToSql());

            var pages = (int)Math.Ceiling((decimal)count / (decimal)item_per_page);

            var current_page = 1;

            if (p_page.HasValue) current_page = p_page.Value;

            if (current_page > pages && pages > 0) current_page = pages;

            var start_index = (current_page - 1) * item_per_page;

            model = Db.Select<ListingProperty>(sql).ToList(); /*model = Db.Select<ListingProperty>(sql).Skip(start_index).Take(item_per_page).ToList();*/

            ViewData["page_curr"] = current_page;
            ViewData["page_total"] = pages;

            return View("ListingList", model);
        }

        [HttpGet]
        public ActionResult ListingDetail(string id)
        {
            var model = Db.Where<ListingProperty>(x => (x.SEO == id && x.IsActive && (!x.IsSchedule || (x.IsSchedule && x.PublishSchedule <= DateTime.Now && x.UnPublishSchedule >= DateTime.Now))));

            if (model != null)
            {
                return View(model.TakeFirst());
            }
            else
            {
                throw new HttpException(404, "Listing " + id + " not found!", 0);
            }
        }
    }
}