using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.Common.Helpers;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;
using PhotoBookmart.DataLayer;
using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.System;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    public class SettingsController : WebAdminController
    {
        [HttpGet]
        public ActionResult Index(int? page)
        {
            int totalItem = (int)Db.Count<Settings>();
            int totalPage = (int)Math.Ceiling((double)totalItem / ITEMS_PER_PAGE);
            int currPage = (page != null && page.Value > 0 && page < totalPage + 1) ? page.Value : 1;

            ViewData["CurrPage"] = currPage;
            return View();
        }

        [HttpGet]
        [ABRequiresAnyRole(RoleEnum.Admin, RoleEnum.Province, RoleEnum.District)]
        public ActionResult Add()
        {
            Settings model = new Settings();
            return View("CreateOrUpdate", model);
        }

        [HttpGet]
        [ABRequiresAnyRole(RoleEnum.Admin, RoleEnum.Province, RoleEnum.District)]
        public ActionResult Edit(int id)
        {
            List<ListModel> settings = GetSettingsByRole((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]));
            Settings model = Db.Select<Settings>(x => x.Where(y =>
                y.Id == id &&
                Sql.In(y.Key, settings.Select(z => z.Id)) &&
                ((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) == RoleEnum.Admin || y.MaHC == CurrentUser.MaHC)).Limit(0, 1)).FirstOrDefault();
            if (model == null) { RedirectToAction("Index", "Settings", new { page = 1 }); }
            return View("CreateOrUpdate", model);
        }

        [HttpPost]
        [ABRequiresAnyRole(RoleEnum.Admin, RoleEnum.Province, RoleEnum.District)]
        public ActionResult CreateOrUpdate(Settings model, string isContinue)
        {
            if (model.Id == 0 && string.IsNullOrEmpty(model.Key))
            {
                ViewBag.Error = "Vui lòng chọn thiết lập.";
                return View("CreateOrUpdate", model);
            }

            List<ListModel> settings = GetSettingsByRole((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]));
            Settings modelUpdated = model.Id > 0 ?
                Db.Select<Settings>(x => x.Where(y => (y.Id == model.Id)).Limit(0, 1)).FirstOrDefault() :
                new Settings() {
                    Key = model.Key,
                    MaHC = CurrentUser.MaHC != null ? CurrentUser.MaHC : "",
                    Value = model.Value,
                    Desc = model.Desc };

            if (modelUpdated == null ||
                !settings.Select(x => x.Id).Contains(modelUpdated.Key) ||
                !((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) == RoleEnum.Admin || modelUpdated.MaHC == CurrentUser.MaHC))
            {
                ViewBag.Error = "Vui lòng không hack ứng dụng.";
                return View("CreateOrUpdate", modelUpdated);
            }
            if (Db.Count<Settings>(x =>
                x.Id != modelUpdated.Id &&
                x.Key == modelUpdated.Key &&
                x.MaHC == modelUpdated.MaHC) > 0)
            {
                ViewBag.Error = "Thiết lập đã được tạo, vui lòng chọn thiết lập khác.";
                return View("CreateOrUpdate", modelUpdated);
            }
            
            if (modelUpdated.Id == 0)
            {
                Db.Insert<Settings>(modelUpdated);
            }
            else
            {
                Db.UpdateOnly<Settings>(new Settings()
                {
                    Value = model.Value,
                    Desc = model.Desc,
                },
                ev => ev.Update(p => new
                {
                    p.Value,
                    p.Desc
                }).Where(m => (m.Id == model.Id)));
            }

            bool addNew = false;
            bool.TryParse(isContinue, out addNew);
            return addNew ? RedirectToAction("Add", "Settings", new { }) : RedirectToAction("Index", "Settings", new { page = 1 });
        }
        
        [HttpPost]
        public ActionResult List(string SettingScope, string Setting, string Province, string District, int Page)
        {
            JoinSqlBuilder<Settings, Settings> jn = new JoinSqlBuilder<Settings, Settings>();
            SqlExpressionVisitor<Settings> sql_exp = Db.CreateExpression<Settings>();
            var p = PredicateBuilder.True<Settings>();

            List<ListModel> settings = GetAllSettings();
            List<string> settings_filter = settings.Select(x => x.Id).ToList();
            string mahc_filter = string.IsNullOrEmpty(District) ? Province : District;
            if (!string.IsNullOrEmpty(SettingScope))
            {
                settings_filter = GetSettingsByScope((Enum_Settings_Scope)Enum.Parse(typeof(Enum_Settings_Scope), SettingScope)).Select(x => x.Id).ToList();
            }
            if (!string.IsNullOrEmpty(Setting))
            {
                settings_filter = new List<string>() { Setting };
            }
            p = p.And(x => Sql.In(x.Key, settings_filter) && x.MaHC.StartsWith(mahc_filter));
            
            int pageSize = ITEMS_PER_PAGE;
            int totalItem = (int)Db.Count<Settings>(p);
            int totalPage = (int)Math.Ceiling((double)totalItem / pageSize);
            int currPage = (Page > 0 && Page < totalPage + 1) ? Page : 1;

            List<Settings> model = Db.Select<Settings>(x => x.Where(p).Limit((currPage - 1) * pageSize, pageSize));
            List<string> settings_role = GetSettingsByRole((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0])).Select(x => x.Id).ToList();
            model.ForEach(x => {
                x.Code_Province = x.MaHC.GetCodeProvince();
                x.Code_District = x.MaHC.GetCodeDistrict();
                x.CanEdit = settings_role.Contains(x.Key) && ((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) == RoleEnum.Admin || x.MaHC == CurrentUser.MaHC);
                x.CanDelete = settings_role.Contains(x.Key) && ((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) == RoleEnum.Admin || x.MaHC == CurrentUser.MaHC);
            });

            List<DanhMuc_HanhChinh> provinces_districts = new List<DanhMuc_HanhChinh>();
            List<string> mahcs_filter = model.Select(y => y.Code_Province).ToList();
            mahcs_filter.AddRange(model.Select(y => y.Code_District));
            mahcs_filter = mahcs_filter.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            if (mahcs_filter.Count > 0) { provinces_districts = Db.Select<DanhMuc_HanhChinh>(x => Sql.In(x.MaHC, mahcs_filter)); }
            model.ForEach(x => {
                x.Name_Province = string.IsNullOrEmpty(x.Code_Province) ? "" : provinces_districts.Single(y => y.MaHC == x.Code_Province).TenHC;
                x.Name_District = string.IsNullOrEmpty(x.Code_District) ? "" : provinces_districts.Single(y => y.MaHC == x.Code_District).TenHC;
            });

            ViewData["CurrPage"] = currPage;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalItem"] = totalItem;
            ViewData["TotalPage"] = totalPage;
            return PartialView("_List", model);
        }

        [HttpPost]
        public ActionResult Delete(int Id)
        {
            try
            {
                List<ListModel> settings = GetSettingsByRole((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]));
                if (Db.Count<Settings>(x =>
                    x.Id == Id &&
                    Sql.In(x.Key, settings.Select(y => y.Id)) &&
                    ((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) == RoleEnum.Admin || x.MaHC == CurrentUser.MaHC)) == 0)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                Db.DeleteById<Settings>(Id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult GetProvincesForFilter()
        {
            return Json(GetAllProvinces());
        }

        [HttpPost]
        public ActionResult GetDistrictsForFilter(string MaHC)
        {
            return Json(GetDistrictsByProvince(MaHC));
        }

        [HttpPost]
        public ActionResult GetSettingScopesForFilter()
        {
            return Json( GetAllSettingScopes() );
        }
        
        [HttpPost]
        public ActionResult GetDanhMucHanhChinhByMaHC(string MaHC)
        {
            return Json(Db.Select<DanhMuc_HanhChinh>(x => x.Where(y => y.MaHC == MaHC).Limit(0, 1)).First());
        }
    }
}
