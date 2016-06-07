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
    public class MembershipGroupController : WebAdminController
    {
        //
        public ActionResult Index(int group_id)
        {
            var group = Cache_GetMembershipGroup().Where(m => m.Id == group_id).FirstOrDefault();
            if (group == null)
            {
                return Redirect("/");
            }

            // get all users belongs to this site
            var users = Db.Where<ABUserAuth>(m => m.GroupId == 0);
            ViewData["Users"] = users;

            return View(group);
        }

        public ActionResult List(long group_id)
        {
            var c = Db.Where<ABUserAuth>(m => m.GroupId == group_id);
            return PartialView("_List", c);
        }

        public ActionResult Update(Site_MemberGroupDetail model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            // get user obj
            var x = User_GetByID(model.UserId);
            // check if user is found and belong to this site
            if (x != null)
            {
                // change
                x.GroupId = model.GroupId;

                // save then done    
                Db.Update<ABUserAuth>(x);
            }
            return RedirectToAction("Index", new { group_id = model.GroupId });
        }

        /// <summary>
        /// Id is user ID
        /// </summary>
        public ActionResult Delete(int id)
        {
            var user = User_GetByID(id);
            if (user != null)
            {
                Db.UpdateOnly<ABUserAuth>(new ABUserAuth() { GroupId = 0 }, ev => ev.Update(p => p.GroupId).Where(m => m.Id == id));
                return RedirectToAction("Index", new { group_id = user.GroupId });
            }
            else
            {
                return Redirect("/");
            }
        }


    }
}
