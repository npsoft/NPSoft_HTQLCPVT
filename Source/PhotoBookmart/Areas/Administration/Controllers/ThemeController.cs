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
using ElFinder;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    [ABRequiresAnyRole(RoleEnum.Administrator)]
    public class ThemeController : WebAdminController
    {
        private const string CacheKey = "CMS_Admin_Cachekey";

        //
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult List()
        {
            List<Theme> c = new List<Theme>();


            c = Db.Select<Theme>();

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

        //[Authorize(Roles = "Administrator,Manager")]
        public ActionResult Add()
        {
            Theme model = new Theme();
            return View(model);
        }

        //[Authorize(Roles = "Administrator,Manager")]
        [Authenticate]
        public ActionResult Edit(Int64 id)
        {
            var models = Db.Where<Theme>(m => m.Id == id);
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
        public ActionResult Update(Theme model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (string.IsNullOrEmpty(model.ThemeName))
            {
                return JsonError("Please enter theme name");
            }

            Theme current_item = new Theme();
            if (model.Id > 0)
            {
                var z = Db.Where<Theme>(m => m.Id == model.Id);
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
                Db.Insert<Theme>(model);
            }
            else
            {
                Db.Update<Theme>(model);
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
        public ActionResult CustomizeTheme(int id)
        {
            var theme = Db.Where<Theme>(m => m.Id == id).Take(1).ToList();
            if (theme.Count > 0)
            {
                // set the id of this theme to cache
                Cache.Add<Theme>(CacheKey, theme.First(), TimeSpan.FromDays(30));
                return View(theme.First());
            }
            else
            {
                Cache.Remove(CacheKey);
                _connector = null;
                return RedirectToAction("Index");
            }
        }


        private Connector _connector;

        public Connector Connector
        {
            get
            {
                if (_connector == null)
                {
                    FileSystemDriver driver = new FileSystemDriver();
                    DirectoryInfo thumbsStorage = new DirectoryInfo(Path.GetTempPath());

                    // get current folder
                    var theme = Cache.Get<Theme>(CacheKey);

                    driver.AddRoot(new Root(new DirectoryInfo(Server.MapPath("~/Themes/"+theme.FolderName)), "/"+theme.FolderName+"/")
                    {
                        Alias = theme.ThemeName,
                        ThumbnailsStorage = thumbsStorage,
                        MaxUploadSizeInMb =100,
                        ThumbnailsUrl = "/Administration/Thumbnails/"
                    });
                    
                    // get current user
                    if (CurrentUser_HasRole(RoleEnum.Administrator))
                    {
                        driver.AddRoot(new Root(new DirectoryInfo(Server.MapPath("~/Content")))
                        {
                            //IsLocked = true,
                            //IsReadOnly = true,
                            //IsShowOnly = true,
                            ThumbnailsStorage = thumbsStorage,
                            ThumbnailsUrl = "/Administration/Thumbnails/"
                        });
                    }
                    else
                    {
                        driver.AddRoot(new Root(new DirectoryInfo(Server.MapPath("~/Content")))
                        {
                            IsLocked = true,
                            IsReadOnly = true,
                            IsShowOnly = true,
                            ThumbnailsStorage = thumbsStorage,
                            ThumbnailsUrl = "/Administration/Thumbnails/"
                        });
                    }

                    _connector = new Connector(driver);
                }
                return _connector;
            }
        }

        [ValidateInput(false)]
        public ActionResult FileManager_Index()
        {
            return Connector.Process(this.HttpContext.Request);
        }

        public ActionResult FileManager_SelectFile(string target)
        {
            return Json(Connector.GetFileByHash(target).FullName);
        }

        public ActionResult FileManager_Thumbs(string tmb)
        {
            return Connector.GetThumbnail(Request, Response, tmb);
        }


    }
}
