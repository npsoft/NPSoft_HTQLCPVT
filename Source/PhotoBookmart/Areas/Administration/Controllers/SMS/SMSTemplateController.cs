using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.Common.Helpers;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;
using PhotoBookmart.Areas.Administration.Controllers;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.SMS;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Sites;
using ServiceStack.ServiceClient.Web;
using PhotoBookmart.Models;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    [ABRequiresAnyRole(RoleEnum.Administrator)]
    public class SMSTemplateController : WebAdminController
    {
        public ActionResult Index()
        {
            Website model = Cache_GetWebSite();
            if (model == null)
            {
                return Redirect("/");
            }

            var countries = Db.Select<Country>();

            ViewData["Countries"] = countries;

            return View(model);
        }

        public ActionResult List(int country_id)
        {
            //// get all template by country code
            JoinSqlBuilder<SMSTemplateModel, SMSTemplateModel> jn = new JoinSqlBuilder<SMSTemplateModel, SMSTemplateModel>();
            jn = jn.Join<SMSTemplateModel, Country>(m => m.CountryCode, k => k.Code);
            jn.Where<Country>(m => m.Id == country_id);
            var sql = jn.ToSql();
            var templates = Db.Select<SMSTemplateModel>(sql);

            var list_users = Cache_GetAllUsers();
            //var countries = Cache_GetAllCountry();

            foreach (var x in templates)
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

                ////
                //var z1 = countries.Where(y => y.Code == x.CountryCode).FirstOrDefault();
                //if (z1 != null)
                //{
                //    x.CountryName = z1.Name;
                //}
                //else
                //{
                //    x.CountryName = "Unknown";
                //}
            }

            // now we need to 
            return PartialView("_List", templates);
        }

        public ActionResult Add(int country_id)
        {
            SMSTemplateModel model = new SMSTemplateModel();

            var countries = Db.Select<Country>();

            var c = countries.Where(x => x.Id == country_id).FirstOrDefault();
            if (c == null)
            {
                return Redirect("/");
            }
            model.CountryCode = c.Code;

            ViewData["Countries"] = countries;

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            SMSTemplateModel model = Db.Where<SMSTemplateModel>(m => m.Id == id).FirstOrDefault();

            var countries = Db.Select<Country>();

            ViewData["Countries"] = countries;

            return View("Add", model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Update(SMSTemplateModel model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            var countries = Db.Select<Country>();

            ViewData["Countries"] = countries;

            if (string.IsNullOrEmpty(model.Name))
            {
                ViewBag.Error = "Please enter SMS Internal name";
                return View("Add", model);
            }

            if (string.IsNullOrEmpty(model.SystemName))
            {
                model.SystemName = model.Name.ToSeoUrl();
            }

            if (string.IsNullOrEmpty(model.Content))
            {
                ViewBag.Error = "Please enter SMS content";
                return View("Add", model);
            }

            // validate existing
            var v = Db.Select<SMSTemplateModel>(x => x.Where(m => m.Id != model.Id && m.CountryCode == model.CountryCode && m.SystemName == model.SystemName).Limit(1)).FirstOrDefault();
            if (v != null)
            {
                ViewBag.Error = "Please use difference System Name.";
                return View("Add", model);
            }

            SMSTemplateModel current_item = new SMSTemplateModel();
            if (model.Id > 0)
            {
                var z = Db.Select<SMSTemplateModel>(x=>x.Where(m => m.Id == model.Id).Limit(1));
                if (z.Count == 0)
                {
                    // the ID is not exist
                    ViewBag.Error = "We can not find your SMS Template";
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
            }
            else
            {
                model.CreatedOn = current_item.CreatedOn;
                model.CreatedBy = current_item.CreatedBy;
            }


            if (model.Id == 0)
            {
                Db.Insert<SMSTemplateModel>(model);
            }
            else
            {
                Db.Update<SMSTemplateModel>(model);
            }

            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<SMSTemplateModel>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Do template clone
        /// </summary>
        /// <param name="Country"></param>
        /// <param name="SMSId"></param>
        /// <returns></returns>
        public ActionResult CloneTemplate(int Country, int SMSId)
        {
            var template = Db.Select<SMSTemplateModel>(x => x.Where(m => m.Id == SMSId).Limit(1)).FirstOrDefault();

            if (template == null)
            {
                return JsonError("Can not find your sms template");
            }

            var country = Db.Select<Country>(x => x.Where(m => m.Id == Country).Limit(1)).FirstOrDefault();
            if (country == null)
            {
                return JsonError("Can not find target country");
            }

            // find out that we can assign to this country or not
            var c = Db.Count<SMSTemplateModel>(x => x.CountryCode == country.Code && x.SystemName == template.SystemName);

            if (c > 0)
            {
                return JsonError("Can not clone template " + template.Name + " to " + country.Name);
            }
            else
            {
                template.Id = 0;
                template.CountryCode = country.Code;
                Db.Insert<SMSTemplateModel>(template);
                return JsonSuccess("", "Template " + template.Name + " has been cloned to " + country.Name);
            }
        }

        #region Common Functions
        static JsonServiceClient server_client;
        /// <summary>
        /// Prepare the conection to SMS server
        /// It will throw the Exception if can not prepare the connection
        /// </summary>
        void _PrepareConnection()
        {
            // connect to server
            if (server_client == null)
            {
                var server_url = (string)Settings.Get(Enum_Settings_Key.SMS_SERVICE_URL, "", Enum_Settings_DataType.String);

                if (string.IsNullOrEmpty(server_url))
                {
                    throw new Exception("Please contact Admin to configure the SMS Settings");
                }
                server_client = new JsonServiceClient(server_url);


                if (!_LoginToServer())
                {
                    throw new Exception("Please contact Admin to configure the SMS Settings. Can not login into SMS Server");
                }
            }
        }

        /// <summary>
        /// Return true if we can connect to server
        /// </summary>
        /// <returns></returns>
        bool _LoginToServer()
        {
            var username = (string)Settings.Get(Enum_Settings_Key.SMS_SERVICE_USERNAME, "", Enum_Settings_DataType.String);
            var password = (string)Settings.Get(Enum_Settings_Key.SMS_SERVICE_PASSWORD, "", Enum_Settings_DataType.String);

            SMSServer_LoginModel loginmodel = new SMSServer_LoginModel() { Username = username, Password = password };
            var login = server_client.Post(loginmodel);
            if (login.Status != null && login.Status.ErrorCode == "1")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region SMSItem Management
        public ActionResult SMSItem_Index(int page = 1)
        {
            ViewData["page"] = page;
            return View();
        }

        public ActionResult SMSItem_List(int page = 1)
        {
            var error_mess = "";
            try
            {
                _PrepareConnection();
            }
            catch (Exception ex)
            {
                error_mess = ex.Message;
            }
            TPQuerySMSItemRequest req = new TPQuerySMSItemRequest()
            {
                Page = page
            };

            var model = new List<TPQuerySMSItem>();
            var pages = 0;
            var total = 0;
            var perpage = 0;

            try
            {
                if (server_client != null)
                {
                    var ret = server_client.Post(req);
                    if (ret.Status != null && ret.Status.ErrorCode == "1")
                    {
                        page = ret.Page;
                        pages = ret.Pages;
                        perpage = ret.PageSize;
                        total = ret.Total;
                        model = ret.Items;
                    }
                }
            }
            catch (Exception ex)
            {
                error_mess = ex.Message;
            }

            ViewData["pages"] = pages;
            ViewData["page"] = page;
            ViewData["total_items"] = total;
            ViewData["action"] = "Index";
            ViewData["perpage"] = perpage;
            ViewData["Error"] = error_mess;
            return PartialView("_SMSItem_List", model);
        }
        #endregion
    }
}
