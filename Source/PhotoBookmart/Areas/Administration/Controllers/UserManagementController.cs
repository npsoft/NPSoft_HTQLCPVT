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
using PhotoBookmart.DataLayer.Models.Users_Management;
using ServiceStack.ServiceInterface;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PhotoBookmart.Common;
using PhotoBookmart.DataLayer.Models.ExtraShipping;
using PhotoBookmart.DataLayer.Models.Sites;
using ServiceStack.ServiceInterface.Auth;
using System.Text.RegularExpressions;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Products;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    [ABRequiresAnyRole(RoleEnum.Admin, RoleEnum.Province, RoleEnum.District)]
    public class UserManagementController : WebAdminController
    {
        [HttpGet]
        public ActionResult Index(int page = 1)
        {
            List<ListModel> roles_lower = GetLowerRoles((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]));
            roles_lower.Insert(0, new ListModel() { Id = "", Name = "- - Cấp - -" });
            
            ViewData["page"] = page;
            ViewData["UserRole"] = new SelectList(roles_lower, "Id", "Name");
            return View();
        }

        [HttpGet]
        public ActionResult Add()
        {
            UserModel model = new UserModel() { Status = true };
            List<ListModel> roles_lower = GetLowerRoles((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]));
            model.rolesId = new string[1] { roles_lower[0].Id };

            ViewData["RolesLower"] = roles_lower;
            return View(model);
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            UserModel model = new UserModel();
            List<ListModel> roles_lower = GetLowerRoles((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]));
            ABUserAuth user = User_GetByID(id);
            if (user == null ||
                !roles_lower.Select(x => x.Id).Contains(user.Roles[0]) ||
                !(RoleEnum.Admin == (RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) || user.MaHC.StartsWith(CurrentUser.MaHC != null ? CurrentUser.MaHC : "")))
            {
                return RedirectToAction("Index");
            }
            else
            {
                user.PasswordHash = "";
                UserModel.ToModel(user, ref model);
            }

            ViewData["RolesLower"] = roles_lower;
            return View("Add", model);
        }

        public ActionResult List(int page)
        {
            if (page < 0) { page = 0; }
            int pages = 0;
            int item_count = 0;

            List<ABUserAuth> c = new List<ABUserAuth>();
            string ma_hc = CurrentUser.MaHC != null ? CurrentUser.MaHC : "";
            List<string> roles_lower = GetLowerRoles((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0])).Select(x => string.Format("[{0}]", x.Id)).ToList();
           
            item_count = (int)Db.Count<ABUserAuth>(x => Sql.In(x.Roles, roles_lower) && (RoleEnum.Admin == (RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) || x.MaHC.StartsWith(ma_hc)));
            pages = item_count / ITEMS_PER_PAGE;
            if (item_count % ITEMS_PER_PAGE > 0) { pages++; }

            List<UserModel> model = new List<UserModel>();
            c = Db.Select<ABUserAuth>(x => x.Where(y => Sql.In(y.Roles, roles_lower) && (RoleEnum.Admin == (RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) || y.MaHC.StartsWith(ma_hc))).Limit((page - 1) * ITEMS_PER_PAGE, ITEMS_PER_PAGE));
            foreach (var item in c)
            {
                UserModel ml = new UserModel();
                UserModel.ToModel(item, ref  ml);
                model.Add(ml);
            }

            ViewData["page"] = page;
            ViewData["pages"] = pages;
            ViewData["items_per_page"] = ITEMS_PER_PAGE;
            ViewData["total_items"] = item_count;
            ViewData["action"] = "Index";
            return PartialView("_List", model);
        }
        
        [HttpPost]
        public ActionResult FullSearch(UserSearchModel req)
        {
            if (req.ResultType == "list")
            {
                int pageSize = ITEMS_PER_PAGE;
                int totalItem = 0;
                int totalPage = 0;
                int currPage = 0;

                string ma_hc = CurrentUser.MaHC != null ? CurrentUser.MaHC : "";
                List<string> roles_lower = GetLowerRoles((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0])).Select(x => string.Format("[{0}]", x.Id)).ToList();
                List<string> roles_filter = roles_lower;
                if (!string.IsNullOrEmpty(req.UserRole))
                {
                    roles_filter = new List<string>();
                    foreach (string role in roles_lower)
                    {
                        if (role == string.Format("[{0}]", req.UserRole))
                        {
                            roles_filter.Add(role);
                            break;
                        }
                    }
                }
                var p = PredicateBuilder.True<ABUserAuth>();
                p = p.And(m => Sql.In(m.Roles, roles_filter) && (RoleEnum.Admin == (RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) || m.MaHC.StartsWith(ma_hc)));
                if (!string.IsNullOrEmpty(req.SearchKey))
                {
                    p = p.And(m => m.UserName.Contains(req.SearchKey) || m.Email.Contains(req.SearchKey) || m.MailAddress.Contains(req.SearchKey) || m.FullName.Contains(req.SearchKey) || m.DisplayName.Contains(req.SearchKey) || m.FirstName.Contains(req.SearchKey) || m.LastName.Contains(req.SearchKey));
                }
                if (!string.IsNullOrEmpty(req.UserStatus))
                {
                    bool active_status = req.UserStatus == "active" ? true : false;
                    p = p.And(m => m.ActiveStatus == active_status);
                }

                totalItem = (int)Db.Count<ABUserAuth>(p);
                totalPage = (int)Math.Ceiling((double)totalItem / pageSize);
                currPage = (req.Page > 0 && req.Page < totalPage + 1) ? req.Page : 1;

                List <ABUserAuth> c = Db.Select<ABUserAuth>(x => x.Where(p).Limit((currPage - 1) * pageSize, pageSize));
                List<UserModel> model = new List<UserModel>();
                foreach (var item in c)
                {
                    UserModel ml = new UserModel();
                    UserModel.ToModel(item, ref ml);
                    model.Add(ml);
                }
                
                ViewData["page"] = currPage;
                ViewData["pages"] = totalPage;
                ViewData["items_per_page"] = pageSize;
                ViewData["total_items"] = totalItem;
                ViewData["action"] = "Index";
                return PartialView("_List", model);
            }
            else if (req.ResultType == "excel")
            {
                return Export_To_Excel(new List<UserModel>());
            }
            else
            {
                return Content("N/A");
            }
        }

        [HttpPost]
        public ActionResult deleteUser(int id)
        {
            try
            {
                string ma_hc = CurrentUser.MaHC != null ? CurrentUser.MaHC : "";
                List<string> roles_lower = GetLowerRoles((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0])).Select(x => string.Format("[{0}]", x.Id)).ToList();
                if (Db.Count<ABUserAuth>(x => Sql.In(x.Roles, roles_lower) && (RoleEnum.Admin == (RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) || x.MaHC.StartsWith(ma_hc)) && x.Id == id) == 0)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                Db.DeleteById<ABUserAuth>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }
        
        [HttpPost]
        public ActionResult Enable(int id)
        {
            try
            {
                string ma_hc = CurrentUser.MaHC != null ? CurrentUser.MaHC : "";
                List<string> roles_lower = GetLowerRoles((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0])).Select(x => string.Format("[{0}]", x.Id)).ToList();
                ABUserAuth user = Db.Select<ABUserAuth>(x => x.Where(y => Sql.In(y.Roles, roles_lower) && (RoleEnum.Admin == (RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) || y.MaHC.StartsWith(ma_hc)) && y.Id == id).Limit(0, 1)).FirstOrDefault();
                if (user == null)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                if (!user.ActiveStatus)
                {
                    user.ActiveStatus = true;
                    Db.Update(user);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Disable(int id)
        {
            try
            {
                string ma_hc = CurrentUser.MaHC != null ? CurrentUser.MaHC : "";
                List<string> roles_lower = GetLowerRoles((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0])).Select(x => string.Format("[{0}]", x.Id)).ToList();
                ABUserAuth user = Db.Select<ABUserAuth>(x => x.Where(y => Sql.In(y.Roles, roles_lower) && (RoleEnum.Admin == (RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) || y.MaHC.StartsWith(ma_hc)) && y.Id == id).Limit(0, 1)).FirstOrDefault();
                if (user == null)
                {
                    return JsonError("Vui lòng không hack ứng dụng.");
                }
                if (user.ActiveStatus)
                {
                    user.ActiveStatus = false;
                    Db.Update(user);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult UpdateUser(UserModel model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            ViewData["RolesLower"] = GetLowerRoles((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0])); ;

            #region VALIDATION: #1
            ABUserAuth user = new ABUserAuth();
            if (model.Id > 0) { user = User_GetByID(model.Id); }
            if (user == null)
            {
                ViewBag.Error = "Vui lòng không hack ứng dụng.";
                return View("Add", model);
            }

            string ma_hc = CurrentUser.MaHC != null ? CurrentUser.MaHC : "";
            List<string> roles_lower = GetLowerRoles((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0])).Select(x => string.Format("[{0}]", x.Id)).ToList();
            if (user.Id > 0 &&
                (!roles_lower.Contains(string.Format("[{0}]", user.Roles[0])) ||
                 !(RoleEnum.Admin == (RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) || user.MaHC.StartsWith(ma_hc))))
            {
                ViewBag.Error = "Vui lòng không hack ứng dụng.";
                return View("Add", model);
            }
            if (!(roles_lower.Contains(string.Format("[{0}]", model.rolesId[0])) &&
                  (RoleEnum.Admin == (RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) || model.MaHC.StartsWith(ma_hc)) &&
                  Db.Count<DanhMuc_HanhChinh>(x => x.MaHC == model.MaHC) > 0))
            {
                ViewBag.Error = "Vui lòng không hack ứng dụng.";
                return View("Add", model);
            }
            #endregion

            #region VALIDATION: #2
            if (user.Id > 0 && string.IsNullOrEmpty(model.NameAddUser)) { model.UserName = user.UserName; }
            if (user.Id > 0 && string.IsNullOrEmpty(model.EmailChange)) { model.Email = user.Email; }
            if (string.IsNullOrEmpty(model.UserName) || string.IsNullOrEmpty(model.UserName.Trim()))
            {
                ViewBag.Error = "Vui lòng nhập tài khoản.";
                return View("Add", model);
            }
            if (user.Id <= 0)
            {
                if (string.IsNullOrEmpty(model.Password))
                {
                    ViewBag.Error = "Vui lòng nhập mật khẩu.";
                    return View("Add", model);
                }
                else if (!new Regex(@"(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).{8,}$", RegexOptions.Compiled).IsMatch(model.Password))
                {
                    ViewBag.Error = "Mật khẩu chứa ít nhất 8 ký tự, bao gồm ký tự hoa/ký tự thường/ký tự số.";
                    return View("Add", model);
                }
            }
            if (!string.IsNullOrEmpty(model.PassNews))
            {
                if (!new Regex(@"(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).{8,}$", RegexOptions.Compiled).IsMatch(model.PassNews))
                {
                    ViewBag.Error = "Mật khẩu (mới) chứa ít nhất 8 ký tự, bao gồm ký tự hoa/ký tự thường/ký tự số.";
                    return View("Add", model);
                }
                model.Password = model.PassNews;
            }
            if (user.Id == 0)
            {
                model.PassNews = model.Password;
            }
            if (string.IsNullOrEmpty(model.Email) || !IsValidEmailAddress(model.Email))
            {
                ViewBag.Error = "Email không đúng định dạng.";
                return View("Add", model);
            }
            if (string.IsNullOrEmpty(model.FullName))
            {
                ViewBag.Error = "Vui lòng nhập họ & tên.";
                return View("Add", model);
            }
            #endregion

            #region VALIDATION: #3
            if (user.Id <= 0 && User_GetByUsername(model.UserName) != null)
            {
                ViewBag.Error = "Tài khoản đã được sử dụng.";
                return View("Add", model);
            }
            if (user.Id > 0 && !string.IsNullOrEmpty(model.NameAddUser))
            {
                if (!model.UserName.Equals(model.NameAddUser) && User_GetByUsername(model.NameAddUser) != null)
                {
                    ViewBag.Error = "Tài khoản (mới) đã được sử dụng.";
                    return View("Add", model);
                }
                else
                {
                    model.UserName = model.NameAddUser;
                }
            }
            if (user.Id <= 0 && User_GetByEmail(model.Email) != null)
            {
                ViewBag.Error = "Email đã được sử dụng.";
                return View("Add", model);
            }
            if (user.Id > 0 && !string.IsNullOrEmpty(model.EmailChange))
            {
                if (!IsValidEmailAddress(model.EmailChange))
                {
                    ViewBag.Error = "Email (mới) không đúng định dạng.";
                    return View("Add", model);
                }
                if (!model.Email.Equals(model.EmailChange) && User_GetByEmail(model.EmailChange) != null)
                {
                    ViewBag.Error = "Email (mới) đã được sử dụng.";
                    return View("Add", model);
                }
                else
                {
                    model.Email = model.EmailChange;
                }
            }
            #endregion
            
            if (user.Id <= 0)
            {
                model.DataCreate = DateTime.Now;
                model.DateUpdate = DateTime.Now;
            }
            else
            {
                model.DataCreate = user.CreatedDate;
                model.DateUpdate = DateTime.Now;
                model.Avatar = user.Avatar;
            }
            model.RoleName = model.rolesId.ToList();
            model.Password = user.PasswordHash;
            UserModel.ToEntity(model, ref user);
            user.Roles = model.RoleName;
            user.Permissions = model.Permission;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.FullName = string.IsNullOrEmpty(model.FullName) ? string.Format("{0} {1}", model.FirstName, model.LastName) : model.FullName;
            user.DisplayName = user.FullName;
            user.Email = model.Email;
            user.UserName = model.UserName;
            user.Gender = model.Gender;
            user.BirthDate = model.BirthDate;
            user.BirthDateRaw = "";
            if (user.BirthDate.HasValue)
            {
                user.BirthDateRaw = user.BirthDate.Value.ToString("MM/dd/yyyy");
            }
            user.MailAddress = model.Address;
            user.Phone = model.Phone;
            user.PostalCode = model.Zipcode;
            user.Country = model.Country;
            user.ActiveStatus = model.Status;
            user.CreatedDate = model.DataCreate;
            user.ModifiedDate = model.DateUpdate;

            if (FileUp != null && FileUp.Count() > 0 && FileUp.First() != null)
                user.Avatar = UploadFile(user.Id, user.UserName, "", FileUp);

            if (!string.IsNullOrEmpty(model.PassNews))
            {
                var p = PasswordGenerate(model.PassNews);
                user.PasswordHash = p.Id;
                user.Salt = p.Name;
            }

            if (model.Id > 0)
            {
                Db.Update<ABUserAuth>(user);
            }
            else
            {
                Db.Insert<ABUserAuth>(user);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult GetDMHCByRole(string role)
        {
            List<DanhMuc_HanhChinh> data = new List<DanhMuc_HanhChinh>();
            string ma_hc = CurrentUser.MaHC != null ? CurrentUser.MaHC : "";
            List<ListModel> roles_lower = GetLowerRoles((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]));
            if (roles_lower.Select(x => x.Id).Contains(role))
            {
                JoinSqlBuilder<DanhMuc_HanhChinh, DanhMuc_HanhChinh> jn = new JoinSqlBuilder<DanhMuc_HanhChinh, DanhMuc_HanhChinh>();
                jn.And<DanhMuc_HanhChinh>(x => ((RoleEnum)Enum.Parse(typeof(RoleEnum), CurrentUser.Roles[0]) == RoleEnum.Admin || x.MaHC.StartsWith(ma_hc)));
                
                SqlExpressionVisitor<DanhMuc_HanhChinh> sql_exp = Db.CreateExpression<DanhMuc_HanhChinh>();
                string st = jn.ToSql();
                int idx = st.IndexOf("WHERE");
                sql_exp.SelectExpression = st.Substring(0, idx);
                sql_exp.WhereExpression = string.Format("{0} AND LEN([MaHC]) = {1}", st.Substring(idx), GetLenMaHCByRole((RoleEnum)Enum.Parse(typeof(RoleEnum), role)));

                string sql = sql_exp.ToSelectStatement();
                data = Db.Select<DanhMuc_HanhChinh>(sql);
            }

            return Json(new {
                Data = data
            });
        }
        
        [Authenticate]
        public ActionResult Export_To_Excel(List<UserModel> model)
        {
            using (var package = new ExcelPackage())
            {
                package.Workbook.Worksheets.Add(string.Format("{0:yyyy-MM-dd}", DateTime.Now));
                ExcelWorksheet ws = package.Workbook.Worksheets[1];
                ws.Name = "User List"; //Setting Sheet's name
                ws.Cells.Style.Font.Size = 12; //Default font size for whole sheet
                ws.Cells.Style.Font.Name = "Calibri"; //Default Font name for whole sheet

                //Merging cells and create a center heading for out table
                ws.Cells[1, 1].Value = "List of Users"; // Heading Name
                ws.Cells[1, 1].Style.Font.Size = 22;
                ws.Cells[1, 1, 1, 10].Merge = true; //Merge columns start and end range
                ws.Cells[1, 1, 1, 10].Style.Font.Bold = true; //Font should be bold
                ws.Cells[1, 1, 1, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Aligmnet is center

                int row_index = 2;

                // header
                List<string> ws_header = new List<string>();
                ws_header.Add("");
                ws_header.Add("ID");
                ws_header.Add("Full Name");
                ws_header.Add("First Name");
                ws_header.Add("Last Name");
                ws_header.Add("Username");
                ws_header.Add("Email");
                ws_header.Add("Role");
                ws_header.Add("Active");
                ws_header.Add("Date Create");
                ws_header.Add("Birth date");
                ws_header.Add("Country");
                ws_header.Add("Gender");
                ws_header.Add("Phone");
                ws_header.Add("Address");
                ws_header.Add("Zip Code");
                ws_header.Add("City");
                ws_header.Add("State");

                for (int i = 1; i < ws_header.Count; i++)
                {
                    ws.Cells[2, i].Value = ws_header[i];
                    ws.Cells[2, i].Style.Font.Bold = true;
                    ws.Cells[2, i].Style.Font.Size = 14;
                }

                // export data, first we need to group it by company
                // get company list in model
                var group_level1 = model.GroupBy(m => m.Status);

                foreach (var item_level1 in group_level1)
                {
                    var firstcol_groupby = item_level1.First().Status;

                    row_index++;
                    if (firstcol_groupby)
                        ws.Cells[row_index, 1].Value = "Active Users";
                    else
                        ws.Cells[row_index, 1].Value = "Not Active Users";

                    ws.Cells[row_index, 1].Style.Font.Bold = true;
                    ws.Cells[row_index, 1].Style.Font.Size = 11;
                    ws.Cells[row_index, 1, row_index, 10].Merge = true; //Merge columns start and end range

                    // list record in detail
                    int row_id = 0;
                    var data = item_level1.OrderBy(m => m.DataCreate);

                    foreach (var item in data) // list all item in each company
                    {
                        row_id++;
                        row_index++;
                        var col_index = 1;
                        // ID
                        ws.Cells[row_index, col_index].Value = row_id;

                        // Name
                        col_index++;
                        ws.Cells[row_index, col_index].Value = item.FullName;

                        // first name
                        col_index++;
                        ws.Cells[row_index, col_index].Value = item.FirstName;

                        // last name
                        col_index++;
                        ws.Cells[row_index, col_index].Value = item.LastName;

                        // username
                        col_index++;
                        ws.Cells[row_index, col_index].Value = item.UserName;

                        // email
                        col_index++;
                        ws.Cells[row_index, col_index].Value = item.Email;

                        // Role
                        col_index++;
                        ws.Cells[row_index, col_index].Value = String.Join(", ", item.RoleName.ToArray());


                        // Active Status
                        col_index++;
                        ws.Cells[row_index, col_index].Value = item.Status;

                        // date create
                        col_index++;
                        ws.Cells[row_index, col_index].Value = string.Format("{0:MM/dd/yyyy HH:mm:ss}", item.DataCreate);

                        // birth day
                        col_index++;
                        ws.Cells[row_index, col_index].Value = string.Format("{0:MM/dd/yyyy HH:mm:ss}", item.BirthDate);

                        // country
                        col_index++;
                        ws.Cells[row_index, col_index].Value = item.Country;

                        // gender
                        col_index++;
                        ws.Cells[row_index, col_index].Value = item.Gender;

                        // phone
                        col_index++;
                        ws.Cells[row_index, col_index].Value = item.Phone;

                        // address
                        col_index++;
                        ws.Cells[row_index, col_index].Value = item.Address;

                        // zip code
                        col_index++;
                        ws.Cells[row_index, col_index].Value = item.Zipcode;

                        col_index++;
                        ws.Cells[row_index, col_index].Value = item.MaHC;
                    } // end record detail

                    row_index++;
                } // end level 1

                //// footer total
                row_index++;
                ws.Cells[row_index, 2].Value = "Total";
                ws.Cells[row_index, 2].Style.Font.Bold = true;
                ws.Cells[row_index, 2].Style.Font.Italic = true;
                ws.Cells[row_index, 2].Style.Font.Size = 11;
                ws.Cells[row_index, 2, row_index, 3].Merge = true; //Merge columns start and end range

                ws.Cells[row_index, 5].Value = "Active Users";
                ws.Cells[row_index, 5].Style.Font.Bold = true;
                ws.Cells[row_index, 5].Style.Font.Size = 11;
                ws.Cells[row_index, 6].Value = model.Count(m => m.Status);

                ws.Cells[row_index, 7].Value = "Not Active Users";
                ws.Cells[row_index, 7].Style.Font.Bold = true;
                ws.Cells[row_index, 7].Style.Font.Size = 11;
                ws.Cells[row_index, 8].Value = model.Count(m => m.Status == false);

                // freeze data
                ws.View.FreezePanes(3, 2);

                // auto adjust the columns width for all columns
                for (int k = 1; k < ws_header.Count + 2; k++)
                    ws.Column(k).AutoFit();

                //var chart = ws.Drawings.AddChart("chart1", eChartType.AreaStacked);
                ////Set position and size
                //chart.SetPosition(0, 630);
                //chart.SetSize(800, 600);

                //// Add the data series. 
                //var series = chart.Series.Add(ws.Cells["A2:A46"], ws.Cells["B2:B46"]);

                var memoryStream = package.GetAsByteArray();
                var fileName = string.Format("UsersManagement-Filter-{0:yyyy-MM-dd-HH-mm-ss}.xlsx", DateTime.Now);
                // mimetype from http://stackoverflow.com/questions/4212861/what-is-a-correct-mime-type-for-docx-pptx-etc
                return base.File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [HttpPost]
        public ActionResult SendEmail(string email, string subject, string body)
        {
            PhotoBookmart.Common.Helpers.SendEmail.SendMail(email, subject, body);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult _GetGroup()
        {
            var user = CurrentUser;
            List<Site_MemberGroup> group = new List<Site_MemberGroup>();

            // check permission
            if (CurrentUser_HasRole(RoleEnum.Administrator, user))
            {
                // allow return sites from all distributors
                group = Cache_GetMembershipGroup();
            }
            //else if (CurrentUser_HasRole(RoleEnum.Merchant, RoleEnum, RoleEnum.Staff, user))
            //{
            //    // only return site of their current distributor ID
            //    group = Cache_GetMembershipGroup().Where(m => m.SiteId == user.SiteId).ToList();
            //}

            return Json(group);
        }

        public ActionResult ChangePassword()
        {
            var user = Db.Where<ABUserAuth>(m => m.Id == AuthenticatedUserID).FirstOrDefault();
            if (user == null)
            {
                return RedirectToAction("Index", "Management");
            }
            return View(user);
        }

        public ActionResult ChangePassword_Update(UserUpdatePassword model)
        {
            if (string.IsNullOrEmpty(model.CurrentPassword))
            {
                return JsonError("Current password is empty");
            }

            if (string.IsNullOrEmpty(model.NewPassword))
            {
                return JsonError("New password is empty");
            }

            if (string.IsNullOrEmpty(model.NewPassword))
            {
                return JsonError("New password (x2) is empty");
            }

            var user = User_GetByID(AuthenticatedUserID);

            // check old password
            var PasswordHasher = new SaltedHash();
            if (PasswordHasher.VerifyHashString(model.CurrentPassword, user.PasswordHash, user.Salt))
            {
                if (model.NewPassword == model.NewPassword2)
                {
                    var p = PasswordGenerate(model.NewPassword);
                    user.PasswordHash = p.Id;
                    user.Salt = p.Name;
                    Db.Update<ABUserAuth>(user);
                }
                else
                {
                    return JsonError("New password is not same");
                }
            }
            else
            {
                return JsonError("Current password is not correct");
            }
            return JsonSuccess("", "Password updated");
        }
    }
}
