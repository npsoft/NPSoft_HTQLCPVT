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
    public class WebsiteContactUsController : WebAdminController
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
            return View(model);
        }

        public ActionResult List(ContactUsListModel model)
        {
            List<Site_ContactUs> c = new List<Site_ContactUs>();

            var p = PredicateBuilder.True<Site_ContactUs>();

            if (model.FromDate.HasValue)
            {
                p = p.And(m => m.Contact_On >= model.FromDate.Value || m.Reply_On >= model.FromDate.Value);
            }

            if (model.ToDate.HasValue)
            {
                p = p.And(m => m.Contact_On <= model.ToDate.Value || m.Reply_On <= model.ToDate.Value);
            }

            if (!string.IsNullOrEmpty(model.term))
            {
                model.term=model.term.ToLower();
                p = p.And(m => m.Comment.ToLower().Contains(model.term) || m.Name.ToLower().Contains(model.term) || m.Email.ToLower().Contains(model.term) || m.Phone.ToLower().Contains(model.term));
            }

            c = Db.Where<Site_ContactUs>(p);

            var list_users = Cache_GetAllUsers();

            foreach (var x in c)
            {
                var z = list_users.Where(m => m.Id == x.Reply_UserId);
                if (z.Count() > 0)
                {
                    var k = z.First();
                    if (string.IsNullOrEmpty(k.FullName))
                        x.Reply_Username = k.UserName;
                    else
                        x.Reply_Username = k.FullName;
                }
                else
                {
                    x.Reply_Username = "Deleted user";
                }
            }

            // now we need to 

            return PartialView("_List", c);
        }
    }
}
