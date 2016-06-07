using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using System.IO;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Newtonsoft.Json;
using PhotoBookmart.Areas.Administration.Controllers;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.Controllers;
using PhotoBookmart.DataLayer;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.Sites;
using PhotoBookmart.DataLayer.Models.ExtraShipping;
using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.DataLayer.Models.Reports;
using PhotoBookmart.Helper;
using PhotoBookmart.Support;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    [ABRequiresAnyRole(RoleEnum.Admin, RoleEnum.Province, RoleEnum.District, RoleEnum.Village)]
    public class WebsiteProductController : WebAdminController
    {
        [HttpGet]
        public ActionResult Index(int? page)
        {
            DoiTuongSearchModel model = new DoiTuongSearchModel()
            {
                MaHC_Province = CurrentUser.MaHC.GetCodeProvince(),
                MaHC_District = CurrentUser.MaHC.GetCodeDistrict(),
                MaHC_Village = CurrentUser.MaHC.GetCodeVillage(),
                OrderDesc = true,
                Page = page.HasValue ? page.Value : 1
            };
            return View(model);
        }
        
        public ActionResult List(DoiTuongSearchModel model)
        {
            #region Initialize
            JoinSqlBuilder<DoiTuong, DoiTuong> jn = new JoinSqlBuilder<DoiTuong, DoiTuong>();
            SqlExpressionVisitor<DoiTuong> sql_exp = Db.CreateExpression<DoiTuong>();
            var p = PredicateBuilder.True<DoiTuong>();
            #endregion

            #region Where clause
            string ma_hc = string.IsNullOrEmpty(model.MaHC_Village) ? (string.IsNullOrEmpty(model.MaHC_District) ? (string.IsNullOrEmpty(model.MaHC_Province) ? "" : model.MaHC_Province) : model.MaHC_District) : model.MaHC_Village;
            p = p.And(x => x.MaHC.StartsWith(ma_hc));
            if (model.IDDiaChi.HasValue)
            {
                p = p.And(x => x.IDDiaChi == model.IDDiaChi.Value);
            }
            if (!string.IsNullOrEmpty(model.MaLDT))
            {
                p = p.And(x => x.MaLDT == model.MaLDT);
            }
            if (!string.IsNullOrEmpty(model.TinhTrang))
            {
                p = p.And(x => x.TinhTrang == model.TinhTrang);
            }
            if (model.IsDuyet.HasValue)
            {
                p = p.And(x => x.IsDuyet == model.IsDuyet.Value);
            }
            if (!string.IsNullOrEmpty(model.Keywords))
            {
                p = p.And(x => x.HoTen.Contains(model.Keywords));
            }
            #endregion

            #region Order By clause
            jn = jn.Where(p);
            string st = jn.ToSql();
            int idx = st.IndexOf("WHERE");
            sql_exp.SelectExpression = st.Substring(0, idx);
            sql_exp.WhereExpression = string.Format("{0}", st.Substring(idx));
            if (model.OrderDesc)
            {
                switch (model.OrderBy)
                {
                    case "HoTen":
                        sql_exp = sql_exp.OrderByDescending(x => x.HoTen);
                        break;
                    case "NgaySinh":
                        sql_exp = sql_exp.OrderByDescending(x => new { x.NamSinh }).ThenByDescending(x => new { x.ThangSinh }).ThenByDescending(x => new { x.NgaySinh });
                        break;
                    case "GioiTinh":
                        sql_exp = sql_exp.OrderByDescending(x => x.GioiTinh);
                        break;
                    case "MaLDT":
                        sql_exp = sql_exp.OrderByDescending(x => x.MaLDT);
                        break;
                    case "TinhTrang":
                        sql_exp = sql_exp.OrderByDescending(x => x.TinhTrang);
                        break;
                    case "IsDuyet":
                        sql_exp = sql_exp.OrderByDescending(x => x.IsDuyet);
                        break;
                    default:
                        sql_exp = sql_exp.OrderByDescending(x => x.Id);
                        break;
                }
            }
            else
            {
                switch (model.OrderBy)
                {
                    case "HoTen":
                        sql_exp = sql_exp.OrderBy(x => x.HoTen);
                        break;
                    case "NgaySinh":
                        sql_exp = sql_exp.OrderBy(x => new { x.NamSinh }).ThenBy(x => new { x.ThangSinh }).ThenBy(x => new { x.NgaySinh });
                        break;
                    case "GioiTinh":
                        sql_exp = sql_exp.OrderBy(x => x.GioiTinh);
                        break;
                    case "MaLDT":
                        sql_exp = sql_exp.OrderBy(x => x.MaLDT);
                        break;
                    case "TinhTrang":
                        sql_exp = sql_exp.OrderBy(x => x.TinhTrang);
                        break;
                    case "IsDuyet":
                        sql_exp = sql_exp.OrderBy(x => x.IsDuyet);
                        break;
                    default:
                        sql_exp = sql_exp.OrderBy(x => x.Id);
                        break;
                }
            }
            #endregion

            #region Paging (Top) clause
            int pageSize = ITEMS_PER_PAGE;
            int totalItem = (int)Db.Count<DoiTuong>(p);
            int totalPage = (int)Math.Ceiling((double)totalItem / pageSize);
            int currPage = (model.Page > 0 && model.Page < totalPage + 1) ? model.Page : 1;
            sql_exp = sql_exp.Limit((currPage - 1) * pageSize, pageSize);
            #endregion

            #region Retrieve data
            st = sql_exp.ToSelectStatement();
            idx = st.IndexOf("FROM");
            st = currPage > 1 ? string.Format("SELECT * {0}", st.Substring(idx)) : st;
            List<DoiTuong> c = Db.Select<DoiTuong>(st);
            #endregion

            #region Prepare data
            PermissionChecker permission = new PermissionChecker(this);
            List<DanhMuc_LoaiDT> Lst_DanhMuc_LoaiDT = new List<DanhMuc_LoaiDT>();
            List<DanhMuc_TinhTrangDT> Lst_DanhMuc_TinhTrangDT = new List<DanhMuc_TinhTrangDT>();
            List<string> lst_maldt = c.Where(x => !string.IsNullOrEmpty(x.MaLDT)).Select(x => x.MaLDT).Distinct().ToList();
            List<string> lst_tinhtrangdt = c.Where(x => !string.IsNullOrEmpty(x.TinhTrang)).Select(x => x.TinhTrang).Distinct().ToList();
            if (lst_maldt.Count > 0) { Lst_DanhMuc_LoaiDT = Db.Select<DanhMuc_LoaiDT>(x => x.Where(y => Sql.In(y.MaLDT, lst_maldt)).Limit(0, lst_maldt.Count)); }
            if (lst_tinhtrangdt.Count > 0) { Lst_DanhMuc_TinhTrangDT = Db.Select<DanhMuc_TinhTrangDT>(x => x.Where(y => Sql.In(y.MaTT, lst_tinhtrangdt)).Limit(0, lst_tinhtrangdt.Count)); }
            List<DanhMuc_TinhTrangDT> lst_tinhtrang_noneapprove = GetTinhTrangDTsByParams(false);
            c.ForEach(x => {
                x.CanView = permission.CanGet(x);
                x.CanEdit = permission.CanUpdate(x);
                x.CanDelete = permission.CanDelete(x);
                x.CanApprove = x.CheckApprove(CurrentUser, lst_tinhtrang_noneapprove);
                x.CanBienDong = x.CheckBienDongCreate(CurrentUser);
                x.MaLDT_Name = string.IsNullOrEmpty(x.MaLDT) || Lst_DanhMuc_LoaiDT.Count(y => y.MaLDT == x.MaLDT) == 0 ? "" : Lst_DanhMuc_LoaiDT.Single(y => y.MaLDT == x.MaLDT).TenLDT;
                x.TinhTrang_Name = string.IsNullOrEmpty(x.TinhTrang) || Lst_DanhMuc_TinhTrangDT.Count(y => y.MaTT == x.TinhTrang) == 0 ? "" : Lst_DanhMuc_TinhTrangDT.Single(y => y.MaTT == x.TinhTrang).TenTT;
            });
            #endregion

            #region Model data
            ViewData["CurrPage"] = currPage;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalItem"] = totalItem;
            ViewData["TotalPage"] = totalPage;
            return PartialView("_List", c);
            #endregion
        }

        public ActionResult BienDong(long Id)
        {
            DoiTuong model = Db.Select<DoiTuong>(x => x.Where(y => y.Id == Id).Limit(0, 1)).FirstOrDefault();
            if (model == null || !model.CheckBienDongCreate(CurrentUser)) { return Content("Vui lòng không hack ứng dụng."); }

            string code_province = model.MaHC.GetCodeProvince();
            string code_district = model.MaHC.GetCodeDistrict();
            string code_village = model.MaHC.GetCodeVillage();
            List<DanhMuc_HanhChinh> hanh_chinh = Db.Select<DanhMuc_HanhChinh>(x => 
                x.Where(y => Sql.In(y.MaHC, new string[3] { code_province, code_district, code_village }))
                .Limit(0, 3));
            DanhMuc_DiaChi dia_chi = Db.Select<DanhMuc_DiaChi>(x => x.Where(y => y.IDDiaChi == model.IDDiaChi).Limit(0, 1)).FirstOrDefault();
            DanhMuc_LoaiDT loai_dt = Db.Select<DanhMuc_LoaiDT>(x => x.Where(y => y.MaLDT == model.MaLDT).Limit(0, 1)).FirstOrDefault();
            if (hanh_chinh.Count(x => x.MaHC == code_province) != 0)
            {
                model.Province_Name = hanh_chinh.Single(x => x.MaHC == code_province).TenHC;
            }
            if (hanh_chinh.Count(x => x.MaHC == code_district) != 0)
            {
                model.District_Name = hanh_chinh.Single(x => x.MaHC == code_district).TenHC;
            }
            if (hanh_chinh.Count(x => x.MaHC == code_village) != 0)
            {
                model.Village_Name = hanh_chinh.Single(x => x.MaHC == code_village).TenHC;
            }
            if (dia_chi != null)
            {
                model.Hamlet_Name = dia_chi.TenDiaChi;
            }
            if (loai_dt != null)
            {
                model.MaLDT_Name = string.Format("{0} - {1}", loai_dt.MaLDT, loai_dt.TenLDT);
            }

            return PartialView("_BienDong", model);
        }

        public ActionResult LichSu(long Id)
        {
            DoiTuong doi_tuong = Db.Select<DoiTuong>(x => x.Where(y => y.Id == Id).Limit(0, 1)).FirstOrDefault();
            if (doi_tuong == null) { return Content("Vui lòng không hack ứng dụng."); }

            List<DoiTuong_BienDong> model = Db.Where<DoiTuong_BienDong>(x => x.IDDT == Id);
            List<DanhMuc_LoaiDT> lst_danhmuc_loaidt = new List<DanhMuc_LoaiDT>();
            List<DanhMuc_HanhChinh> lst_danhmuc_hanhchinh = new List<DanhMuc_HanhChinh>();
            List<string> vals_danhmuc_loaidt = new List<string>();
            List<string> vals_danhmuc_hanhchinh = new List<string>();
            vals_danhmuc_loaidt = model.Where(x => !string.IsNullOrEmpty(x.MaLDT)).Select(x => x.MaLDT).Distinct().ToList();
            model.Where(x => !string.IsNullOrEmpty(x.MaHC)).ToList().ForEach(y => {
                vals_danhmuc_hanhchinh.AddRange(new string[2] { y.MaHC, y.MaHC.GetCodeVillage() });
            });
            vals_danhmuc_hanhchinh.Distinct().ToList();
            if (vals_danhmuc_loaidt.Count > 0) { lst_danhmuc_loaidt = Db.Select<DanhMuc_LoaiDT>(x => x.Where(y => Sql.In(y.MaLDT, vals_danhmuc_loaidt)).Limit(0, vals_danhmuc_loaidt.Count)); }
            if (vals_danhmuc_hanhchinh.Count > 0) { lst_danhmuc_hanhchinh = Db.Select<DanhMuc_HanhChinh>(x => x.Where(y => Sql.In(y.MaHC, vals_danhmuc_hanhchinh)).Limit(0, vals_danhmuc_hanhchinh.Count)); }

            model.ForEach(x => {
                x.MaLDT_Ten = string.IsNullOrEmpty(x.MaLDT) || lst_danhmuc_loaidt.Count(y => y.MaLDT == x.MaLDT) == 0 ? "" : lst_danhmuc_loaidt.Single(y => y.MaLDT == x.MaLDT).TenLDT;
                x.MaHC_Ten = string.IsNullOrEmpty(x.MaHC) || lst_danhmuc_hanhchinh.Count(y => y.MaHC == x.MaHC) == 0 ? "" : lst_danhmuc_hanhchinh.Single(y => y.MaHC == x.MaHC).TenDayDu;
                x.MaHC_Ten_Village = string.IsNullOrEmpty(x.MaHC) || lst_danhmuc_hanhchinh.Count(y => y.MaHC == x.MaHC.GetCodeVillage()) == 0 ? "" : lst_danhmuc_hanhchinh.Single(y => y.MaHC == x.MaHC.GetCodeVillage()).TenHC;
            });
            
            int page_size = ITEMS_PER_PAGE;
            int total_item = model.Count();
            int total_page = (int)Math.Ceiling((double)total_item / page_size);
            int curr_page = total_page;

            ViewData["Id"] = Id;
            ViewData["CanDelete"] = doi_tuong.CheckBienDongDelete(CurrentUser, model);
            ViewData["CurrPage"] = curr_page;
            ViewData["PageSize"] = page_size;
            ViewData["TotalItem"] = total_item;
            ViewData["TotalPage"] = total_page;
            return PartialView("_LichSu", model);
        }

        [HttpGet]
        public ActionResult Add()
        {
            DoiTuong model = new DoiTuong() {
                CanApprove = !CurrentUser.HasRole(RoleEnum.Village)
            };
            return View(model);
        }

        [HttpGet]
        public ActionResult Edit(long id)
        {  
            var model = Db.Select<DoiTuong>(x => x.Where(y => y.Id == id).Limit(0, 1)).FirstOrDefault();

            PermissionChecker permission = new PermissionChecker(this);
            if (!permission.CanUpdate(model)) { return RedirectToAction("Index", "WebsiteProduct", new { }); }

            model.CanApprove = model.CheckApprove(CurrentUser, GetTinhTrangDTsByParams(false));
            model.MaLDT_Details = Db.Where<DoiTuong_LoaiDoiTuong_CT>(x => x.CodeObj == model.IDDT && !x.IsDelete);
            return View("Add", model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Update(DoiTuong model, IEnumerable<HttpPostedFileBase> FilesUp)
        {
            #region Refill for object
            model.Id = model.Id > 0 ? model.Id : 0;
            DoiTuong old_model = null;
            if (model.Id > 0)
            {
                old_model = Db.Select<DoiTuong>(x => x.Where(y => y.Id == model.Id).Limit(0, 1)).FirstOrDefault();
                if (old_model != null)
                {
                    model.IDDT = old_model.IDDT;
                    model.CreatedOn = old_model.CreatedOn;
                    model.CreatedBy = old_model.CreatedBy;
                    old_model.MaLDT_Details = Db.Where<DoiTuong_LoaiDoiTuong_CT>(x => x.CodeObj == old_model.IDDT).OrderBy(x => x.Id).ToList();
                }
            }
            else
            {
                model.IDDT = Guid.NewGuid();
                model.CreatedOn = DateTime.Now;
                model.CreatedBy = CurrentUser.Id;
            }
            #endregion

            #region Validate for object
            #region Block #1
            model.HoTen = string.Format("{0}", model.HoTen).FormalizeName();
            model.NamSinh = string.Format("{0}", model.NamSinh).Trim();
            model.ThangSinh = string.Format("{0}", model.ThangSinh).Trim();
            model.NgaySinh = string.Format("{0}", model.NgaySinh).Trim();
            model.TruQuan = string.Format("{0}", model.TruQuan).Trim();
            model.NguyenQuan = string.Format("{0}", model.NguyenQuan).Trim();
            model.MaLDT_Details = model.MaLDT_Details.OrderBy(x => x.Id).ToList();
            if (!model.isKhuyetTat.HasValue || !model.isKhuyetTat.Value)
            {
                model.DangKT = null;
                model.MucDoKT = null;
            }
            #endregion
            #region Block #2
            if (string.IsNullOrEmpty(model.MaHC))
            {
                return JsonError("Vui lòng chọn xã.");
            }
            if (!model.IDDiaChi.HasValue)
            {
                return JsonError("Vui lòng chọn xóm.");
            }
            #endregion
            #region Block #3
            if (string.IsNullOrEmpty(model.HoTen))
            {
                return JsonError("Vui lòng nhập họ & tên.");
            }
            if (string.IsNullOrEmpty(model.NamSinh))
            {
                return JsonError("Vui lòng nhập ngày sinh » Năm.");
            }
            if (!new Regex(@"^([1-9]\d{3})$", RegexOptions.Compiled).IsMatch(model.NamSinh) ||
                int.Parse(model.NamSinh) < DateTime.MinValue.Year ||
                int.Parse(model.NamSinh) > DateTime.MaxValue.Year)
            {
                return JsonError("Ngày sinh » Năm không đúng định dạng.");
            }
            if (!string.IsNullOrEmpty(model.NgaySinh) && string.IsNullOrEmpty(model.ThangSinh))
            {
                return JsonError("Vui lòng nhập ngày sinh » Tháng.");
            }
            if (!string.IsNullOrEmpty(model.ThangSinh) && !new Regex(@"^(0?[1-9]|1[012])$", RegexOptions.Compiled).IsMatch(model.ThangSinh))
            {
                return JsonError("Ngày sinh » Tháng không đúng định dạng.");
            }
            if (!string.IsNullOrEmpty(model.NgaySinh))
            {
                DateTime dt = new DateTime(int.Parse(model.NamSinh), int.Parse(model.ThangSinh), 1).AddMonths(1).Subtract(TimeSpan.FromSeconds(1));
                if (!new Regex(@"^(0?[1-9]|[12][0-9]|3[01])$", RegexOptions.Compiled).IsMatch(model.NgaySinh) || int.Parse(model.NgaySinh) > dt.Day)
                {
                    return JsonError("Ngày sinh » Ngày không đúng định dạng.");
                }
            }
            model.ThangSinh = !string.IsNullOrEmpty(model.ThangSinh) && model.ThangSinh.Length < 2 ? "0" + model.ThangSinh : model.ThangSinh;
            model.NgaySinh = !string.IsNullOrEmpty(model.NgaySinh) && model.NgaySinh.Length < 2 ? "0" + model.NgaySinh : model.NgaySinh;
            if (string.IsNullOrEmpty(model.GioiTinh))
            {
                return JsonError("Vui lòng chọn giới tính.");
            }
            if (string.IsNullOrEmpty(model.TruQuan))
            {
                return JsonError("Vui lòng nhập trú quán.");
            }
            if (string.IsNullOrEmpty(model.NguyenQuan))
            {
                return JsonError("Vui lòng nhập nguyên quán.");
            }
            #endregion
            #region Block #4
            if (string.IsNullOrEmpty(model.MaLDT))
            {
                return JsonError("Vui lòng chọn loại.");
            }
            if (model.MaLDT.StartsWith("01"))
            {
                #region TODO: 01
                if (model.MaLDT_Details.Count != 1)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                DoiTuong_LoaiDoiTuong_CT detail = new DoiTuong_LoaiDoiTuong_CT();
                detail.Type1_InfoFather = string.Format("{0}", model.MaLDT_Details[0].Type1_InfoFather).Trim();
                detail.Type1_InfoMother = string.Format("{0}", model.MaLDT_Details[0].Type1_InfoMother).Trim();
                if (string.IsNullOrEmpty(detail.Type1_InfoFather))
                {
                    return JsonError("Vui lòng nhập thông tin cha.");
                }
                if (string.IsNullOrEmpty(detail.Type1_InfoMother))
                {
                    return JsonError("Vui lòng nhập thông tin mẹ.");
                }
                model.MaLDT_Details[0] = detail;
                #endregion
            }
            else if (model.MaLDT.StartsWith("03"))
            {
                #region TODO: 03
                if (model.MaLDT_Details.Count == 0)
                {
                    return JsonError("Vui lòng thêm thông tin » con.");
                }
                else if (model.MaLDT.StartsWith("0301") && model.MaLDT_Details.Count != 1)
                {
                    return JsonError("Loại 0301 giành cho đối tượng nuôi 1 con.");
                }
                else if (model.MaLDT.StartsWith("0302") && model.MaLDT_Details.Count < 2)
                {
                    return JsonError("Loại 0302 giành cho đối tượng nuôi 2 con trở lên.");
                }
                List<DoiTuong_LoaiDoiTuong_CT> details = new List<DoiTuong_LoaiDoiTuong_CT>();
                foreach (DoiTuong_LoaiDoiTuong_CT item in model.MaLDT_Details)
                {
                    DoiTuong_LoaiDoiTuong_CT detail = new DoiTuong_LoaiDoiTuong_CT();
                    detail.Id = item.Id;
                    detail.Type3_FullName = string.Format("{0}", item.Type3_FullName).Trim();
                    detail.Type3_DateOfBirth = item.Type3_DateOfBirth;
                    detail.Type3_DateOfBirth_IsMonth = item.Type3_DateOfBirth_IsMonth;
                    detail.Type3_DateOfBirth_IsDate = item.Type3_DateOfBirth_IsDate;
                    detail.Type3_Gender = item.Type3_Gender;
                    detail.Type3_CurrAddr = string.Format("{0}", item.Type3_CurrAddr).Trim();
                    detail.Type3_StatusLearn = string.Format("{0}", item.Type3_StatusLearn).Trim();
                    if (string.IsNullOrEmpty(detail.Type3_FullName))
                    {
                        return JsonError("Vui lòng kiểm tra lại họ & tên cho các con.");
                    }
                    if (detail.Type3_DateOfBirth.Year == DateTime.MinValue.Year)
                    {
                        return JsonError("Vui lòng kiểm tra lại ngày sinh cho các con » Năm.");
                    }
                    if (!detail.Type3_DateOfBirth_IsMonth && detail.Type3_DateOfBirth.Month != 1)
                    {
                        return JsonError("Vui lòng kiểm tra lại ngày sinh cho các con » Tháng.");
                    }
                    if (!detail.Type3_DateOfBirth_IsDate && detail.Type3_DateOfBirth.Day != 1)
                    {
                        return JsonError("Vui lòng kiểm tra lại ngày sinh cho các con » Ngày.");
                    }
                    if (string.IsNullOrEmpty(detail.Type3_Gender) || !new string[] { "Male", "Female" }.Contains(detail.Type3_Gender))
                    {
                        return JsonError("Vui lòng kiểm tra lại giới tính cho các con.");
                    }
                    details.Add(detail);
                }
                model.MaLDT_Details = details;
                if (model.Id == 0 && details.Count(x => x.Id > 0) > 0)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                #endregion
            }
            else if (model.MaLDT.StartsWith("04"))
            {
                #region TODO: 04
                if (model.MaLDT_Details.Count != 1)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                DoiTuong_LoaiDoiTuong_CT detail = new DoiTuong_LoaiDoiTuong_CT();
                detail.Type4_MaritalStatus = model.MaLDT_Details[0].Type4_MaritalStatus;
                detail.Type4_InfoAdditional = string.Format("{0}", model.MaLDT_Details[0].Type4_InfoAdditional).Trim();
                if (string.IsNullOrEmpty(detail.Type4_MaritalStatus))
                {
                    return JsonError("Vui lòng chọn tình trạng hôn nhân.");
                }
                if (Db.Count<DanhMuc_TinhTrangHonNhan>(x => x.IDTinhTrangHN == Guid.Parse(detail.Type4_MaritalStatus)) == 0)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                model.MaLDT_Details[0] = detail;
                #endregion
            }
            else if (model.MaLDT.StartsWith("05"))
            {
                #region TODO: 05
                if (model.MaLDT_Details.Count != 1)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                DoiTuong_LoaiDoiTuong_CT detail = new DoiTuong_LoaiDoiTuong_CT();
                detail.Type5_SelfServing = model.MaLDT_Details[0].Type5_SelfServing;
                detail.Type5_Carer = string.Format("{0}", model.MaLDT_Details[0].Type5_Carer).Trim();
                if (string.IsNullOrEmpty(detail.Type5_SelfServing))
                {
                    return JsonError("Vui lòng chọn khả năng phục vụ.");
                }
                if (Db.Count<DanhMuc_KhaNangPhucVu>(x => x.IDKhaNangPV == Guid.Parse(detail.Type5_SelfServing)) == 0)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                model.MaLDT_Details[0] = detail;
                #endregion
            }
            else
            {
                #region TODO: Others
                if (model.MaLDT_Details.Count > 0)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                #endregion
            }
            if (model.Id > 0 &&
                model.MaLDT_Details.Count(x => x.Id > 0) > 0 &&
                model.MaLDT_Details.Count(x => x.Id > 0) != Db.Count<DoiTuong_LoaiDoiTuong_CT>(x =>
                    x.IsDelete == false &&
                    x.CodeObj == model.IDDT &&
                    Sql.In(x.Id, model.MaLDT_Details.Where(y => y.Id > 0).Select(y => y.Id))))
            {
                return JsonError("Vui lòng không hack ứng dụng.");
            }
            if (model.isKhuyetTat.HasValue && model.isKhuyetTat.Value)
            {
                if (!model.DangKT.HasValue)
                {
                    return JsonError("Vui lòng chọn dạng khuyết tật.");
                }
                if (!model.MucDoKT.HasValue)
                {
                    return JsonError("Vui lòng chọn mức độ khuyết tật.");
                }
            }
            #endregion
            #region Block #5
            if (!model.MucTC.HasValue)
            {
                return JsonError("Vui lòng nhập mức trợ cấp.");
            }
            if (model.MucTC.Value < 0)
            {
                return JsonError("Mức trợ cấp không đúng định dạng.");
            }
            if (!model.NgayHuong.HasValue)
            {
                return JsonError("Vui lòng chọn ngày hưởng.");
            }
            if (string.IsNullOrEmpty(model.TinhTrang))
            {
                return JsonError("Vui lòng chọn tình trạng.");
            }
            #endregion
            #region Block #6
            List<long> MaLDT_Details_Ids = model.MaLDT_Details.Where(x => x.Id > 0).Select(x => x.Id).ToList(); 
            if (Db.Count<DanhMuc_HanhChinh>(x => x.MaHC == model.MaHC) == 0 ||
                Db.Count<DanhMuc_DiaChi>(x => x.MaHC == model.MaHC && x.IDDiaChi == model.IDDiaChi) == 0 ||
                !new string[] { "Male", "Female" }.Contains(model.GioiTinh) ||
                Db.Count<DanhMuc_LoaiDT>(x => x.MaLDT == model.MaLDT) == 0 ||
                Db.Count<DanhMuc_TinhTrangDT>(x => x.MaTT == model.TinhTrang) == 0 ||
                model.DangKT.HasValue && Db.Count<DanhMuc_DangKhuyetTat>(x => x.IDDangTat == model.DangKT.Value) == 0 ||
                model.MucDoKT.HasValue && Db.Count<DanhMuc_MucDoKhuyetTat>(x => x.IDMucDoKT == model.MucDoKT.Value) == 0 ||
                model.MaDanToc.HasValue && Db.Count<DanhMuc_DanToc>(x => x.Id == model.MaDanToc.Value) == 0 ||
                model.Id > 0 && MaLDT_Details_Ids.Count > 0 && MaLDT_Details_Ids.Count != Db.Count<DoiTuong_LoaiDoiTuong_CT>(x => x.CodeObj == model.IDDT && Sql.In(x.Id, MaLDT_Details_Ids)) ||
                (model.Id == 0 &&
                    model.IsDuyet &&
                    CurrentUser.HasRole(RoleEnum.Village)) ||
                (model.Id > 0 &&
                    !old_model.IsDuyet && model.IsDuyet &&
                    !old_model.CheckApprove(CurrentUser, GetTinhTrangDTsByParams(false))) ||
                (model.Id == 0 || !model.IsDuyet || !old_model.IsDuyet && model.IsDuyet) &&
                    !GetTinhTrangDTsByParams(false).Select(x => x.MaTT).Contains(model.TinhTrang))
            {
                return JsonError("Vui lòng không hack ứng dụng.");
            }
            #endregion
            #region Block #7
            DanhMuc_LoaiDT loaidt = Db.Select<DanhMuc_LoaiDT>(x => x.Where(y => y.MaLDT == model.MaLDT).Limit(0, 1)).FirstOrDefault();
            if (!model.MaLDT.CheckDateOfBirth(model.NamSinh, model.ThangSinh, model.NgaySinh))
            {
                return JsonError("Ngày sinh không phù hợp với loại.");
            }

            if (model.Id == 0 && model.IsDuyet ||
                model.Id > 0 && !old_model.IsDuyet && model.IsDuyet && GetSBD(model.Id) == 0)
            {
                DoiTuong_BienDong bien_dong = new DoiTuong_BienDong();
                bien_dong.MaHC = model.MaHC;
                bien_dong.IDDiaChi = model.IDDiaChi;
                bien_dong.TinhTrang = model.TinhTrang;
                bien_dong.MaLDT = model.MaLDT;
                bien_dong.NgayHuong = model.NgayHuong;
                bien_dong.HeSo = decimal.Parse(string.Format("{0}", loaidt.HeSo));
                bien_dong.MucTC = model.MucTC;
                bien_dong.MucChenh = model.MucTC;
                bien_dong.MoTa = model.TinhTrang == "HDH" ? "Đang hưởng" : model.TinhTrang == "HDE" ? "Chuyển đến từ nơi khác" : "";
                model.BienDong_Lst.Add(bien_dong);
            }

            if (model.Id > 0 &&
                old_model.IsDuyet &&
                old_model.MaLDT != model.MaLDT)
            {
                if (!old_model.CheckBienDongCreate(CurrentUser))
                {
                    return JsonError("Vui lòng xóa hết lịch sử biến động trước khi sửa.");
                }
                DateTime dt_old = new DateTime(old_model.NgayHuong.Value.Year, old_model.NgayHuong.Value.Month, 1, 0, 0, 0, 0);
                DateTime dt_new = new DateTime(model.NgayHuong.Value.Year, model.NgayHuong.Value.Month, 1, 0, 0, 0, 0);
                if (dt_new <= dt_old)
                {
                    return JsonError("Tháng biến động phải lớn hơn tháng đang hưởng.");
                }
                
                DanhMuc_LoaiDT loaidt_old = Db.Select<DanhMuc_LoaiDT>(x => x.Where(y => y.MaLDT == old_model.MaLDT).Limit(0, 1)).FirstOrDefault();
                DoiTuong_BienDong bien_dong_cat = new DoiTuong_BienDong();
                DoiTuong_BienDong bien_dong_them = new DoiTuong_BienDong();

                bien_dong_them.MaHC = bien_dong_cat.MaHC = model.MaHC;
                bien_dong_them.IDDiaChi = bien_dong_cat.IDDiaChi = model.IDDiaChi;
                bien_dong_them.NgayHuong = bien_dong_cat.NgayHuong = model.NgayHuong;
                bien_dong_them.MucChenh = bien_dong_cat.MucChenh = Math.Abs(model.MucTC.Value - old_model.MucTC.Value);
                bien_dong_cat.MaLDT = old_model.MaLDT;
                bien_dong_cat.HeSo = decimal.Parse(string.Format("{0}", loaidt_old.HeSo));
                bien_dong_them.MaLDT = model.MaLDT;
                bien_dong_them.HeSo = decimal.Parse(string.Format("{0}", loaidt.HeSo));
                if (model.MucTC > old_model.MucTC)
                {
                    bien_dong_cat.TinhTrang = "KCT";
                    bien_dong_cat.MoTa = "Cắt do chuyển loại trợ cấp tăng";
                    bien_dong_them.TinhTrang = "HCT";
                    bien_dong_them.MoTa = "Thêm do chuyển loại trợ cấp tăng";
                    // model.TinhTrang = "HCT";
                }
                else if (model.MucTC < old_model.MucTC)
                {
                    bien_dong_cat.TinhTrang = "KCG";
                    bien_dong_cat.MoTa = "Cắt do chuyển loại trợ cấp giảm";
                    bien_dong_them.TinhTrang = "HCG";
                    bien_dong_them.MoTa = "Thêm do chuyển loại trợ cấp giảm";
                    // model.TinhTrang = "HCG";
                }
                else
                {
                    bien_dong_cat.TinhTrang = "KCK";
                    bien_dong_cat.MoTa = "Cắt do chuyển loại trợ cấp";
                    bien_dong_them.TinhTrang = "HCK";
                    bien_dong_them.MoTa = "Thêm do chuyển loại trợ cấp";
                    // model.TinhTrang = "HCK";
                }
                bien_dong_cat.MucTC = bien_dong_them.MucTC = model.MucTC;

                model.BienDong_Lst.Add(bien_dong_cat);
                model.BienDong_Lst.Add(bien_dong_them);
            }
            #endregion
            #region Block #8
            PermissionChecker permission = new PermissionChecker(this);
            if (!(model.Id == 0 && permission.CanAdd(model) ||
                  model.Id > 0 && permission.CanUpdate(old_model) && permission.CanUpdate(model)))
            {
                return JsonError("Vui lòng không hack ứng dụng.");
            }
            #endregion
            #endregion
            
            #region Save changes to database
            using (IDbTransaction dbTrans = Db.OpenTransaction())
            {
                if (model.Id > 0 &&
                    old_model.IsDuyet && old_model.MaLDT != model.MaLDT)
                {
                    Db.UpdateOnly<DoiTuong_LoaiDoiTuong_CT>(new DoiTuong_LoaiDoiTuong_CT() { 
                        IsDelete = true 
                    }, ev => ev.Update(p => new { 
                        p.IsDelete 
                    }).Where(x => x.IsDelete == false && x.CodeObj == model.IDDT));
                }
                else
                {
                    Db.Delete<DoiTuong_LoaiDoiTuong_CT>(x => x.IsDelete == false && x.CodeObj == model.IDDT && x.CodeType != model.MaLDT);
                    if (model.MaLDT.StartsWith("03"))
                    {
                        List<long> ids = new List<long>() { 0 };
                        ids.AddRange(model.MaLDT_Details.Where(x => x.Id > 0).Select(x => x.Id));
                        Db.Delete<DoiTuong_LoaiDoiTuong_CT>(x => x.IsDelete == false && x.CodeObj == model.IDDT && !Sql.In(x.Id, ids));
                    }
                    if (!model.MaLDT.StartsWith("03") && model.MaLDT_Details.Count == 1)
                    {
                        DoiTuong_LoaiDoiTuong_CT detail = Db.Select<DoiTuong_LoaiDoiTuong_CT>(x => x.Where(y => y.CodeObj == model.IDDT).Limit(0, 1)).FirstOrDefault();
                        if (detail != null) { model.MaLDT_Details[0].Id = detail.Id; }
                    }
                }
                
                model.MaLDT_Details.ForEach(x => {
                    x.IsDelete = false;
                    x.CodeObj = model.IDDT;
                    x.CodeType = model.MaLDT;
                });

                Db.Save(model);
                if (model.Id == 0) { model.Id = Db.GetLastInsertId(); }
                model.BienDong_Lst.ForEach(x => {
                    x.IDDT = model.Id;
                });
                Db.InsertAll<DoiTuong_BienDong>(model.BienDong_Lst);
                Db.UpdateAll<DoiTuong_LoaiDoiTuong_CT>(model.MaLDT_Details.Where(x => x.Id > 0));
                Db.InsertAll<DoiTuong_LoaiDoiTuong_CT>(model.MaLDT_Details.Where(x => x.Id == 0));
                dbTrans.Commit();
            }
            return JsonSuccess(Url.Action("Index", "WebsiteProduct", new { }), null);
            #endregion
        }
        
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Delete(int id)
        {
            try
            {
                PermissionChecker permission = new PermissionChecker(this);
                var entity = Db.Select<DoiTuong>(x => x.Where(y => y.Id == id).Limit(0, 1)).FirstOrDefault();
                if (!permission.CanDelete(entity))
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                using (IDbTransaction dbTrans = Db.OpenTransaction())
                {
                    Db.Delete<DoiTuong_LoaiDoiTuong_CT>(x => x.CodeObj == entity.IDDT);
                    Db.DeleteById<DoiTuong>(id);
                    dbTrans.Commit();
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult BienDong_Duyet(BienDong_DuyetModel model)
        {
            try
            {
                DoiTuong entity = Db.Select<DoiTuong>(x => x.Where(y => y.Id == model.Id).Limit(0, 1)).FirstOrDefault();
                if (entity == null || !entity.CheckApprove(CurrentUser, GetTinhTrangDTsByParams(false))) { return JsonError("Vui lòng không hack ứng dụng."); }

                entity.IsDuyet = !entity.IsDuyet;
                DoiTuong_BienDong bien_dong = null;
                if (Db.Count<DoiTuong_BienDong>(x => x.IDDT == entity.Id) == 0)
                {
                    DanhMuc_LoaiDT loai_dt = Db.Select<DanhMuc_LoaiDT>(x => x.Where(y => y.MaLDT == entity.MaLDT).Limit(0, 1)).FirstOrDefault();
                    bien_dong = new DoiTuong_BienDong();
                    bien_dong.IDDT = entity.Id;
                    bien_dong.MaHC = entity.MaHC;
                    bien_dong.IDDiaChi = entity.IDDiaChi;
                    bien_dong.TinhTrang = entity.TinhTrang;
                    bien_dong.MaLDT = entity.MaLDT;
                    bien_dong.NgayHuong = entity.NgayHuong;
                    bien_dong.HeSo = decimal.Parse(string.Format("{0}", loai_dt.HeSo));
                    bien_dong.MucTC = entity.MucTC;
                    bien_dong.MucChenh = entity.MucTC;
                    bien_dong.MoTa = entity.TinhTrang == "HDH" ? "Đang hưởng" : entity.TinhTrang == "HDE" ? "Chuyển đến từ nơi khác" : "";
                }
                using (IDbTransaction dbTrans = Db.OpenTransaction())
                {
                    Db.UpdateOnly<DoiTuong>(entity, ev => ev.Update(p => new { p.IsDuyet }).Where(x => x.Id == entity.Id).Limit(0, 1));
                    if (bien_dong != null) { Db.Save(bien_dong); }
                    dbTrans.Commit();
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult BienDong_CatChet(BienDong_CatChetModel model)
        {
            try
            {
                var entity = Db.Select<DoiTuong>(x => x.Where(y => y.Id == model.Id).Limit(0, 1)).FirstOrDefault();
                if (entity == null || !entity.CheckBienDongCreate(CurrentUser)) { return JsonError("Vui lòng không hack ứng dụng."); }
                DateTime dt_old = new DateTime(entity.NgayHuong.Value.Year, entity.NgayHuong.Value.Month, 1, 0, 0, 0, 0);
                DateTime dt_new = new DateTime(model.NgayBienDong.Year, model.NgayBienDong.Month, 1, 0, 0, 0, 0);
                if (dt_new <= dt_old) { return JsonError("Tháng biến động phải lớn hơn tháng đang hưởng."); }
                var loai_dt = Db.Select<DanhMuc_LoaiDT>(x => x.Where(y => y.MaLDT == entity.MaLDT).Limit(0, 1)).FirstOrDefault();

                entity.TinhTrang = "KCC";
                entity.NgayHuong = model.NgayBienDong;
                DoiTuong_BienDong bien_dong = new DoiTuong_BienDong();
                bien_dong.IDDT = entity.Id;
                bien_dong.MaHC = entity.MaHC;
                bien_dong.IDDiaChi = entity.IDDiaChi;
                bien_dong.TinhTrang = entity.TinhTrang;
                bien_dong.MaLDT = entity.MaLDT;
                bien_dong.NgayHuong = entity.NgayHuong;
                bien_dong.HeSo = decimal.Parse(string.Format("{0}", loai_dt.HeSo));
                bien_dong.MucTC = entity.MucTC;
                bien_dong.MucChenh = 0;
                bien_dong.MoTa = "Cắt chết";
                using (IDbTransaction dbTrans = Db.OpenTransaction())
                {
                    Db.UpdateOnly<DoiTuong>(entity, ev => ev.Update(p => new 
                    {
                        p.TinhTrang,
                        p.NgayHuong
                    }).Where(x => x.Id == entity.Id).Limit(0, 1));
                    Db.Save(bien_dong);
                    dbTrans.Commit();
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult BienDong_DungTroCap(BienDong_DungTroCapModel model)
        {
            try
            {
                var entity = Db.Select<DoiTuong>(x => x.Where(y => y.Id == model.Id).Limit(0, 1)).FirstOrDefault();
                if (entity == null || !entity.CheckBienDongCreate(CurrentUser)) { return JsonError("Vui lòng không hack ứng dụng."); }
                DateTime dt_old = new DateTime(entity.NgayHuong.Value.Year, entity.NgayHuong.Value.Month, 1, 0, 0, 0, 0);
                DateTime dt_new = new DateTime(model.NgayBienDong.Year, model.NgayBienDong.Month, 1, 0, 0, 0, 0);
                if (dt_new <= dt_old) { return JsonError("Tháng biến động phải lớn hơn tháng đang hưởng."); }
                var loai_dt = Db.Select<DanhMuc_LoaiDT>(x => x.Where(y => y.MaLDT == entity.MaLDT).Limit(0, 1)).FirstOrDefault();

                entity.TinhTrang = "KTD";
                entity.NgayHuong = model.NgayBienDong;
                DoiTuong_BienDong bien_dong = new DoiTuong_BienDong();
                bien_dong.IDDT = entity.Id;
                bien_dong.MaHC = entity.MaHC;
                bien_dong.IDDiaChi = entity.IDDiaChi;
                bien_dong.TinhTrang = entity.TinhTrang;
                bien_dong.MaLDT = entity.MaLDT;
                bien_dong.NgayHuong = entity.NgayHuong;
                bien_dong.HeSo = decimal.Parse(string.Format("{0}", loai_dt.HeSo));
                bien_dong.MucTC = entity.MucTC;
                bien_dong.MucChenh = 0;
                bien_dong.MoTa = "Tạm dừng trợ cấp";
                using (IDbTransaction dbTrans = Db.OpenTransaction())
                {
                    Db.UpdateOnly<DoiTuong>(entity, ev => ev.Update(p => new
                    {
                        p.TinhTrang,
                        p.NgayHuong
                    }).Where(x => x.Id == entity.Id).Limit(0, 1));
                    Db.Save(bien_dong);
                    dbTrans.Commit();
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult BienDong_ChuyenDiaBan(BienDong_ChuyenDiaBanModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.MaHC)) { return JsonError("Vui lòng chọn xã."); }
                if (!model.IDDiaChi.HasValue) { return JsonError("Vui lòng chọn xóm."); }
                var entity = Db.Select<DoiTuong>(x => x.Where(y => y.Id == model.Id).Limit(0, 1)).FirstOrDefault();
                if (entity == null || !entity.CheckBienDongCreate(CurrentUser) ||
                    Db.Count<DanhMuc_HanhChinh>(x => x.MaHC == model.MaHC) == 0 ||
                    Db.Count<DanhMuc_DiaChi>(x => x.IDDiaChi == model.IDDiaChi) == 0) { return JsonError("Vui lòng không hack ứng dụng."); }
                DateTime dt_old = new DateTime(entity.NgayHuong.Value.Year, entity.NgayHuong.Value.Month, 1, 0, 0, 0, 0);
                DateTime dt_new = new DateTime(model.NgayBienDong.Year, model.NgayBienDong.Month, 1, 0, 0, 0, 0);
                if (dt_new <= dt_old) { return JsonError("Tháng biến động phải lớn hơn tháng đang hưởng."); }
                if (!entity.MaHC.ChangeVillage(model.MaHC)) { return JsonError("Vui lòng chọn địa bàn mới."); }
                var loai_dt = Db.Select<DanhMuc_LoaiDT>(x => x.Where(y => y.MaLDT == entity.MaLDT).Limit(0, 1)).FirstOrDefault();
                
                List<DoiTuong_BienDong> lst_bien_dong = new List<DoiTuong_BienDong>();
                DoiTuong_BienDong bien_dong_kdi = new DoiTuong_BienDong();
                DoiTuong_BienDong bien_dong_hde = new DoiTuong_BienDong();

                entity.NgayHuong = model.NgayBienDong;
                bien_dong_kdi.IDDT = bien_dong_hde.IDDT = entity.Id;
                bien_dong_kdi.MaLDT = bien_dong_hde.MaLDT = entity.MaLDT;
                bien_dong_kdi.NgayHuong = bien_dong_hde.NgayHuong = entity.NgayHuong;
                bien_dong_kdi.HeSo = bien_dong_hde.HeSo = decimal.Parse(string.Format("{0}", loai_dt.HeSo));
                bien_dong_kdi.MucTC = bien_dong_hde.MucTC = entity.MucTC;
                bien_dong_kdi.MucChenh = bien_dong_hde.MucChenh = 0;

                entity.TinhTrang = "KDI";
                bien_dong_kdi.MaHC = entity.MaHC;
                bien_dong_kdi.IDDiaChi = entity.IDDiaChi;
                bien_dong_kdi.TinhTrang = entity.TinhTrang;
                bien_dong_kdi.MoTa = "Chuyển đi";

                entity.TinhTrang = "HDE";
                entity.MaHC = model.MaHC;
                entity.IDDiaChi = model.IDDiaChi;
                bien_dong_hde.MaHC = entity.MaHC;
                bien_dong_hde.IDDiaChi = entity.IDDiaChi;
                bien_dong_hde.TinhTrang = entity.TinhTrang;
                bien_dong_hde.MoTa = "Chuyển đến từ nơi khác";

                lst_bien_dong.Add(bien_dong_kdi);
                if (entity.MaHC.ChangeProvince(model.MaHC) || entity.MaHC.ChangeDistrict(model.MaHC))
                {
                    entity.IsDuyet = false;
                }
                else
                {
                    lst_bien_dong.Add(bien_dong_hde);
                }
                
                using (IDbTransaction dbTrans = Db.OpenTransaction())
                {
                    Db.UpdateOnly<DoiTuong>(entity, ev => ev.Update(p => new
                    {
                        p.TinhTrang,
                        p.NgayHuong,
                        p.MaHC,
                        p.IDDiaChi,
                        p.IsDuyet
                    }).Where(x => x.Id == entity.Id).Limit(0, 1));
                    Db.InsertAll<DoiTuong_BienDong>(lst_bien_dong);
                    dbTrans.Commit();
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult BienDong_ThayDoiLoaiDoiTuong(BienDong_ThayDoiLoaiDoiTuong model)
        {
            try
            {                
                #region Block #1
                List<DoiTuong_LoaiDoiTuong_CT> maldt_details = new List<DoiTuong_LoaiDoiTuong_CT>();
                if (string.IsNullOrEmpty(model.MaLDT))
                {
                    return JsonError("Vui lòng chọn loại.");
                }
                if (model.MaLDT.StartsWith("01"))
                {
                    #region TODO: 01
                    if (model.MaLDT_Details.Count != 1)
                    {
                        return JsonError("Vui lòng không hack ứng dụng.");
                    }
                    DoiTuong_LoaiDoiTuong_CT detail = new DoiTuong_LoaiDoiTuong_CT();
                    detail.Type1_InfoFather = string.Format("{0}", model.MaLDT_Details[0].Type1_InfoFather).Trim();
                    detail.Type1_InfoMother = string.Format("{0}", model.MaLDT_Details[0].Type1_InfoMother).Trim();
                    if (string.IsNullOrEmpty(detail.Type1_InfoFather))
                    {
                        return JsonError("Vui lòng nhập thông tin cha.");
                    }
                    if (string.IsNullOrEmpty(detail.Type1_InfoMother))
                    {
                        return JsonError("Vui lòng nhập thông tin mẹ.");
                    }
                    maldt_details.Add(detail);
                    #endregion
                }
                else if (model.MaLDT.StartsWith("03"))
                {
                    #region TODO: 03
                    if (model.MaLDT_Details.Count == 0)
                    {
                        return JsonError("Vui lòng thêm thông tin » con.");
                    }
                    else if (model.MaLDT.StartsWith("0301") && model.MaLDT_Details.Count != 1)
                    {
                        return JsonError("Loại 0301 giành cho đối tượng nuôi 1 con.");
                    }
                    else if (model.MaLDT.StartsWith("0302") && model.MaLDT_Details.Count < 2)
                    {
                        return JsonError("Loại 0302 giành cho đối tượng nuôi 2 con trở lên.");
                    }
                    List<DoiTuong_LoaiDoiTuong_CT> details = new List<DoiTuong_LoaiDoiTuong_CT>();
                    foreach (DoiTuong_LoaiDoiTuong_CT item in model.MaLDT_Details)
                    {
                        DoiTuong_LoaiDoiTuong_CT detail = new DoiTuong_LoaiDoiTuong_CT();
                        detail.Id = item.Id;
                        detail.Type3_FullName = string.Format("{0}", item.Type3_FullName).Trim();
                        detail.Type3_DateOfBirth = item.Type3_DateOfBirth;
                        detail.Type3_DateOfBirth_IsMonth = item.Type3_DateOfBirth_IsMonth;
                        detail.Type3_DateOfBirth_IsDate = item.Type3_DateOfBirth_IsDate;
                        detail.Type3_Gender = item.Type3_Gender;
                        detail.Type3_CurrAddr = string.Format("{0}", item.Type3_CurrAddr).Trim();
                        detail.Type3_StatusLearn = string.Format("{0}", item.Type3_StatusLearn).Trim();
                        if (string.IsNullOrEmpty(detail.Type3_FullName))
                        {
                            return JsonError("Vui lòng kiểm tra lại họ & tên cho các con.");
                        }
                        if (detail.Type3_DateOfBirth.Year == DateTime.MinValue.Year)
                        {
                            return JsonError("Vui lòng kiểm tra lại ngày sinh cho các con » Năm.");
                        }
                        if (!detail.Type3_DateOfBirth_IsMonth && detail.Type3_DateOfBirth.Month != 1)
                        {
                            return JsonError("Vui lòng kiểm tra lại ngày sinh cho các con » Tháng.");
                        }
                        if (!detail.Type3_DateOfBirth_IsDate && detail.Type3_DateOfBirth.Day != 1)
                        {
                            return JsonError("Vui lòng kiểm tra lại ngày sinh cho các con » Ngày.");
                        }
                        if (string.IsNullOrEmpty(detail.Type3_Gender) || !new string[] { "Male", "Female" }.Contains(detail.Type3_Gender))
                        {
                            return JsonError("Vui lòng kiểm tra lại giới tính cho các con.");
                        }
                        details.Add(detail);
                    }
                    maldt_details.AddRange(details);
                    #endregion
                }
                else if (model.MaLDT.StartsWith("04"))
                {
                    #region TODO: 04
                    if (model.MaLDT_Details.Count != 1)
                    {
                        return JsonError("Vui lòng không hack ứng dụng.");
                    }
                    DoiTuong_LoaiDoiTuong_CT detail = new DoiTuong_LoaiDoiTuong_CT();
                    detail.Type4_MaritalStatus = model.MaLDT_Details[0].Type4_MaritalStatus;
                    detail.Type4_InfoAdditional = string.Format("{0}", model.MaLDT_Details[0].Type4_InfoAdditional).Trim();
                    if (string.IsNullOrEmpty(detail.Type4_MaritalStatus))
                    {
                        return JsonError("Vui lòng chọn tình trạng hôn nhân.");
                    }
                    if (Db.Count<DanhMuc_TinhTrangHonNhan>(x => x.IDTinhTrangHN == Guid.Parse(detail.Type4_MaritalStatus)) == 0)
                    {
                        return JsonError("Vui lòng không hack ứng dụng.");
                    }
                    maldt_details.Add(detail);
                    #endregion
                }
                else if (model.MaLDT.StartsWith("05"))
                {
                    #region TODO: 05
                    if (model.MaLDT_Details.Count != 1)
                    {
                        return JsonError("Vui lòng không hack ứng dụng.");
                    }
                    DoiTuong_LoaiDoiTuong_CT detail = new DoiTuong_LoaiDoiTuong_CT();
                    detail.Type5_SelfServing = model.MaLDT_Details[0].Type5_SelfServing;
                    detail.Type5_Carer = string.Format("{0}", model.MaLDT_Details[0].Type5_Carer).Trim();
                    if (string.IsNullOrEmpty(detail.Type5_SelfServing))
                    {
                        return JsonError("Vui lòng chọn khả năng phục vụ.");
                    }
                    if (Db.Count<DanhMuc_KhaNangPhucVu>(x => x.IDKhaNangPV == Guid.Parse(detail.Type5_SelfServing)) == 0)
                    {
                        return JsonError("Vui lòng không hack ứng dụng.");
                    }
                    maldt_details.Add(detail);
                    #endregion
                }
                else
                {
                    #region TODO: Others
                    if (model.MaLDT_Details.Count > 0)
                    {
                        return JsonError("Vui lòng không hack ứng dụng.");
                    }
                    #endregion
                }
                if (!model.MucTC.HasValue)
                {
                    return JsonError("Vui lòng nhập mức trợ cấp.");
                }
                if (model.MucTC.Value < 0)
                {
                    return JsonError("Mức trợ cấp không đúng định dạng.");
                }
                #endregion
                #region Block #2
                var entity = Db.Select<DoiTuong>(x => x.Where(y => y.Id == model.Id).Limit(0, 1)).FirstOrDefault();
                if (entity == null || !entity.CheckBienDongCreate(CurrentUser) ||
                    Db.Count<DanhMuc_LoaiDT>(x => x.MaLDT == model.MaLDT) == 0)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                DateTime dt_old = new DateTime(entity.NgayHuong.Value.Year, entity.NgayHuong.Value.Month, 1, 0, 0, 0, 0);
                DateTime dt_new = new DateTime(model.NgayBienDong.Year, model.NgayBienDong.Month, 1, 0, 0, 0, 0);
                if (dt_new <= dt_old)
                {
                    return JsonError("Tháng biến động phải lớn hơn tháng đang hưởng.");
                }
                if (model.MaLDT == entity.MaLDT)
                {
                    return JsonError("Vui lòng chọn loại mới.");
                }
                if (!model.MaLDT.CheckDateOfBirth(entity.NamSinh, entity.ThangSinh, entity.NgaySinh))
                {
                    return JsonError("Ngày sinh không phù hợp với loại.");
                }
                #endregion
                #region Block #3
                var loai_dts = Db.Select<DanhMuc_LoaiDT>(x => x.Where(y => y.MaLDT == entity.MaLDT || y.MaLDT == model.MaLDT).Limit(0, 2));
                var loai_dt_old = loai_dts.Single(x => x.MaLDT == entity.MaLDT);
                var loai_dt_new = loai_dts.Single(x => x.MaLDT == model.MaLDT);

                DoiTuong_BienDong bien_dong_cat = new DoiTuong_BienDong();
                DoiTuong_BienDong bien_dong_them = new DoiTuong_BienDong();

                entity.NgayHuong = model.NgayBienDong;
                bien_dong_cat.IDDT = bien_dong_them.IDDT = entity.Id;
                bien_dong_cat.MaHC = bien_dong_them.MaHC = entity.MaHC;
                bien_dong_cat.IDDiaChi = bien_dong_them.IDDiaChi = entity.IDDiaChi;
                bien_dong_cat.NgayHuong = bien_dong_them.NgayHuong = entity.NgayHuong;
                bien_dong_cat.MucChenh = bien_dong_them.MucChenh = Math.Abs(model.MucTC.Value - entity.MucTC.Value);

                bien_dong_cat.MaLDT = entity.MaLDT;
                bien_dong_cat.HeSo = decimal.Parse(string.Format("{0}", loai_dt_old.HeSo));

                entity.MaLDT = model.MaLDT;
                bien_dong_them.MaLDT = entity.MaLDT;
                bien_dong_them.HeSo = decimal.Parse(string.Format("{0}", loai_dt_new.HeSo));

                if (model.MucTC > entity.MucTC)
                {
                    bien_dong_cat.TinhTrang = "KCT";
                    bien_dong_cat.MoTa = "Cắt do chuyển loại trợ cấp tăng";
                    bien_dong_them.TinhTrang = "HCT";
                    bien_dong_them.MoTa = "Thêm do chuyển loại trợ cấp tăng";
                    entity.TinhTrang = "HCT";
                }
                else if (model.MucTC < entity.MucTC)
                {
                    bien_dong_cat.TinhTrang = "KCG";
                    bien_dong_cat.MoTa = "Cắt do chuyển loại trợ cấp giảm";
                    bien_dong_them.TinhTrang = "HCG";
                    bien_dong_them.MoTa = "Thêm do chuyển loại trợ cấp giảm";
                    entity.TinhTrang = "HCG";
                }
                else
                {
                    bien_dong_cat.TinhTrang = "KCK";
                    bien_dong_cat.MoTa = "Cắt do chuyển loại trợ cấp";
                    bien_dong_them.TinhTrang = "HCK";
                    bien_dong_them.MoTa = "Thêm do chuyển loại trợ cấp";
                    entity.TinhTrang = "HCK";
                }
                entity.MucTC = model.MucTC;
                bien_dong_cat.MucTC = bien_dong_them.MucTC = entity.MucTC;

                entity.BienDong_Lst.Add(bien_dong_cat);
                entity.BienDong_Lst.Add(bien_dong_them);

                entity.MaLDT_Details = maldt_details;
                entity.MaLDT_Details.ForEach(x => {
                    x.CodeObj = entity.IDDT;
                    x.CodeType = entity.MaLDT;
                });
                #endregion
                #region Block #4
                using (IDbTransaction dbTrans = Db.OpenTransaction())
                {
                    Db.UpdateOnly<DoiTuong>(entity, ev => ev.Update(p => new 
                    {
                        p.TinhTrang,
                        p.NgayHuong,
                        p.MaLDT,
                        p.MucTC
                    }).Where(x => x.Id == entity.Id).Limit(0, 1));
                    Db.UpdateOnly<DoiTuong_LoaiDoiTuong_CT>(new DoiTuong_LoaiDoiTuong_CT() {
                        IsDelete = true
                    }, ev => ev.Update(p => new {
                        p.IsDelete
                    }).Where(x => x.IsDelete == false && x.CodeObj == entity.IDDT));
                    Db.InsertAll<DoiTuong_LoaiDoiTuong_CT>(entity.MaLDT_Details);
                    Db.InsertAll<DoiTuong_BienDong>(entity.BienDong_Lst);
                    dbTrans.Commit();
                }
                #endregion
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult LichSu_Delete(int Id)
        {
            try
            {
                DoiTuong doi_tuong = Db.Select<DoiTuong>(x => x.Where(y => y.Id == Id).Limit(0, 1)).First();
                if (doi_tuong == null) { return JsonError("Vui lòng không hack ứng dụng."); }
                List<DoiTuong_BienDong> lst_bien_dong = Db.Select<DoiTuong_BienDong>(x => x.Where(y => y.IDDT == doi_tuong.Id).OrderBy(z => z.Id));
                if (!doi_tuong.CheckBienDongDelete(CurrentUser, lst_bien_dong)) { return JsonError("Vui lòng không hack ứng dụng."); }

                DoiTuong_BienDong bien_dong = lst_bien_dong[lst_bien_dong.Count - 2];
                List<DoiTuong_BienDong> lst_bien_dong_delete = new List<DoiTuong_BienDong>() { lst_bien_dong.Last() };
                if (new string[] { "HDE", "HCT", "HCG", "HCK" }.Contains(lst_bien_dong_delete[0].TinhTrang))
                {
                    bien_dong = lst_bien_dong[lst_bien_dong.Count - 3];
                    lst_bien_dong_delete.Add(lst_bien_dong[lst_bien_dong.Count - 2]);
                }

                List<DoiTuong_LoaiDoiTuong_CT> lst_loaidt_ct_delete = new List<DoiTuong_LoaiDoiTuong_CT>();
                List<DoiTuong_LoaiDoiTuong_CT> lst_loaidt_ct_update = new List<DoiTuong_LoaiDoiTuong_CT>();
                if (new string[] { "HCT", "HCG", "HCK" }.Contains(lst_bien_dong_delete[0].TinhTrang))
                {
                    List<DoiTuong_LoaiDoiTuong_CT> lst_loai_dt = Db.Select<DoiTuong_LoaiDoiTuong_CT>(x => x.Where(y => y.CodeObj == doi_tuong.IDDT).OrderByDescending(z => z.Id)); 
                    foreach (DoiTuong_LoaiDoiTuong_CT loai_ct in lst_loai_dt)
                    {
                        if (!loai_ct.IsDelete)
                        {
                            lst_loaidt_ct_delete.Add(loai_ct);
                            continue;
                        }
                        if (loai_ct.CodeType != lst_bien_dong[lst_bien_dong.Count - 2].MaLDT)
                        {
                            break;
                        }
                        loai_ct.IsDelete = false;
                        lst_loaidt_ct_update.Add(loai_ct);
                    }
                }

                doi_tuong.MaHC = bien_dong.MaHC;
                doi_tuong.IDDiaChi = bien_dong.IDDiaChi;
                doi_tuong.TinhTrang = bien_dong.TinhTrang;
                doi_tuong.MaLDT = bien_dong.MaLDT;
                doi_tuong.NgayHuong = bien_dong.NgayHuong;
                doi_tuong.MucTC = bien_dong.MucTC;

                using (IDbTransaction dbTrans = Db.OpenTransaction())
                {
                    Db.UpdateOnly<DoiTuong>(doi_tuong, ev => ev.Update(p => new
                    {
                        p.MaHC,
                        p.IDDiaChi,
                        p.TinhTrang,
                        p.MaLDT,
                        p.NgayHuong,
                        p.MucTC
                    }).Where(x => x.Id == doi_tuong.Id).Limit(0, 1));
                    Db.DeleteAll(lst_loaidt_ct_delete);
                    Db.UpdateAll(lst_loaidt_ct_update);
                    Db.Delete<DoiTuong_BienDong>(x => Sql.In(x.Id, lst_bien_dong_delete.Select(y => y.Id)));
                    dbTrans.Commit();
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Detail(int id)
        {
            var model = Db.Where<Product>(m => m.Id == id).FirstOrDefault();
            if (model == null)
                return Redirect("/");


            // created by username
            var list_users = Cache_GetAllUsers();

            var zk = list_users.Where(m => m.Id == model.CreatedBy).FirstOrDefault();
            if (zk == null)
            {
                model.CreatedByUsername = "Deleted User";
            }
            else
            {
                if (string.IsNullOrEmpty(zk.FullName))
                    model.CreatedByUsername = zk.UserName;
                else
                    model.CreatedByUsername = zk.FullName;
            }

            return View(model);
        }
        
        #region Detail Image

        /// <param name="id">Site ID</param>
        /// <returns></returns>
        public ActionResult Detail_Image_List(int id)
        {
            var c = Db.Where<Product_Images>(m => m.ProductId == id);

            // created by username
            var list_users = Cache_GetAllUsers();
            var list_cat = Cache_GetProductCategory();

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

            return PartialView(c);
        }

        public ActionResult Detail_Image_Add(Product_Images model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            var curent_item = new Product_Images();
            if (model.Id > 0)
            {
                curent_item = Db.Where<Product_Images>(m => m.Id == model.Id).FirstOrDefault();
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

            curent_item.ProductId = model.ProductId;
            curent_item.Name = model.Name;
            curent_item.Status = model.Status;

            if (FileUp != null && FileUp.FirstOrDefault() != null)
            {
                curent_item.Filename = UploadFile(AuthenticatedUserID, model.ProductId.ToString(), "ProductImage", FileUp);
            }

            if (model.Id > 0)
            {
                Db.Update<Product_Images>(curent_item);
            }
            else
            {
                Db.Insert<Product_Images>(curent_item);
            }
            return RedirectToAction("Detail", new { id = model.ProductId });
        }

        #endregion

        #region Support
        
        [HttpPost]
        public ActionResult GetEthnicsForFilter()
        {
            return Json(Cache_GetAllEthnics());
        }
        
        [HttpPost]
        public ActionResult GetLevelsDisabilityForFilter()
        {
            return Json(Cache_GetAllLevelsDisability());
        }
        
        private ActionResult ExportListProduct()
        {
            var package = new ExcelPackage();

            package.Workbook.Worksheets.Add("Products");
            ExcelWorksheet ws = package.Workbook.Worksheets[1];
            ws.Name = "Products"; //Setting Sheet's name
            ws.Cells.Style.Font.Size = 12; //Default font size for whole sheet
            ws.Cells.Style.Font.Name = "Calibri"; //Default Font name for whole sheet

            //Merging cells and create a center heading for out table
            ws.Cells[1, 1].Value = "List of Photobookmart Products "; // Heading Name
            ws.Cells[1, 1].Style.Font.Size = 22;
            ws.Cells[1, 1, 1, 10].Merge = true; //Merge columns start and end range
            ws.Cells[1, 1, 1, 10].Style.Font.Bold = true; //Font should be bold
            ws.Cells[1, 1, 1, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Aligmnet is center


            var lstProductCat = Db.Select<Product_Category>(x => x.Where(y => (y.Status)));

            var lstProductOption = Db.Select<Product_Option>(x => x.Where(y => (y.Status)));


            List<string> headers = new List<string>() { "", "Name", "Size", "Pages", "Price", "Shipping", "PhotoCreation_Id" };

            headers.AddRange(lstProductOption.Select(x => (x.InternalName)).ToArray<string>());

            int row = 3;

            for (int i = 0; i < headers.Count; i++)
            {
                ws.Cells[row, i + 1].Value = headers[i];

                ws.Cells[row, i + 1].Style.Font.Bold = true;

                ws.Cells[row, i + 1].Style.Font.Size = 13;
            }
            //

            FillAllExcel(ref ws, lstProductCat, lstProductOption, headers.Count);


            for (int i = 0; i < headers.Count; i++)
            {
                if (i < headers.Count - lstProductOption.Count)
                {
                    ws.Column(i + 1).AutoFit();
                }
                else
                {
                    ws.Column(i + 1).Width = 25;
                }
            }

            // footer

            ws.View.FreezePanes(3, 7);

            var memoryStream = package.GetAsByteArray();
            package.Dispose();
            var fileName = string.Format("List product {0:yyyy-MM-dd-HH-mm-ss}.xlsx", DateTime.Now);
            package.Dispose();
            return base.File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);

        }

        private void FillAllExcel(ref ExcelWorksheet ws, List<Product_Category> lstProductCat, List<Product_Option> lstProductOption, int header_count)
        {
            var row = 4;

            foreach (var pc in lstProductCat ?? Enumerable.Empty<Product_Category>())
            {
                InitRowCategory(ref ws, pc, row++, header_count);

                var lstProduct = Db.Select<Product>(x => x.Where(y => (y.Status && y.CatId == pc.Id)).OrderBy(z => (z.Order)));
                int _p_index = 1;
                foreach (var p in lstProduct ?? Enumerable.Empty<Product>())
                {
                    InitRowProduct(ref ws, p, row++, lstProductOption, _p_index);
                    _p_index++;
                }

                // footer
                //row++;
                ws.Cells[row, 2].Value = "Total";
                ws.Cells[row, 2].Style.Font.Bold = true;
                ws.Cells[row, 2].Style.Font.Italic = true;
                ws.Cells[row, 2].Style.Font.Size = 11;
                ws.Cells[row, 2, row, 3].Merge = true; //Merge columns start and end range

                ws.Cells[row, 4].Value = lstProduct == null ? 0 : lstProduct.Count;
                row++; row++;
            }


        }

        private void InitRowCategory(ref ExcelWorksheet ws, Product_Category cat, int row, int numOfCols)
        {
            ws.Cells[row, 1].Value = cat.Name;

            ws.Cells[row, 1].Style.Font.Bold = true;

            ws.Cells[row, 1].Style.Font.Size = 11;

            ws.Cells[row, 1, row, numOfCols].Merge = true;
            ws.Cells[row, 1, row, numOfCols].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, 1, row, numOfCols].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(230, 232, 235));
        }

        private void InitRowProduct(ref ExcelWorksheet ws, Product product, int row, List<Product_Option> lstProductOption, int product_index)
        {
            var rate = Setting_GetExchangeRate();
            ws.Cells[row, 1].Value = product_index;
            ws.Cells[row, 2].Value = product.Name;

            ws.Cells[row, 3].Value = product.Size;

            ws.Cells[row, 4].Value = string.Format("{0} Pages", product.Pages);

            ws.Cells[row, 5].Value = product.getPrice(Enum_Price_MasterType.Product, rate.Code).Value.ToMoneyFormated(rate.CurrencyCode);

            ws.Cells[row, 6].Value = product.isFreeShip ? "Free" : product.getPrice(Enum_Price_MasterType.ProductShippingPrice,rate.Code).Value.ToMoneyFormated(rate.CurrencyCode);

            ws.Cells[row, 7].Value = product.MyPhotoCreationId;

            var lstProductInOption = Db.Select<OptionInProduct>(x => x.Where(y => (y.ProductId == product.Id)).OrderBy(z => (z.Id)));

            for (int i = 0; i < lstProductOption.Count; i++)
            {
                var productInOption = lstProductInOption.Where(x => (x.ProductOptionId == lstProductOption[i].Id)).FirstOrDefault();

                ws.Cells[row, 7 + i + 1].Value = productInOption != null ? lstProductOption[i].getPrice(Enum_Price_MasterType.ProductOption,rate.Code).Value.ToMoneyFormated(rate.CurrencyCode) : "";
            }
        }

        #endregion
    }
}
