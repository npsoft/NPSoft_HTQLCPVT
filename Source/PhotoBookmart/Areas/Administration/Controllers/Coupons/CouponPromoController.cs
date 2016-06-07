using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.Controllers;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;
using PhotoBookmart.Areas.Administration.Controllers;
using PhotoBookmart.DataLayer.Models.Products;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using System.Globalization;
using ServiceStack.Common;
using PhotoBookmart.Support;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.System;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    [ABRequiresAnyRole(RoleEnum.Administrator)]
    public class CouponPromoController : WebAdminController
    {
        #region [CONTROLLER] CouponPromo

        public ActionResult Index(int page = 1)
        {
            ViewData["page"] = page;
            return View();
        }

        public ActionResult List(int page)
        {
            int pages = 0;
            int items_per_page = 30;
            int item_count = 0;

            if (page < 0)
            {
                page = 0;
            }

            item_count = (int)Db.Count<CouponPromo>();
            pages = item_count / items_per_page;
            if (item_count % items_per_page > 0)
            {
                pages++;
            }

            List<CouponPromo> c = new List<CouponPromo>();

            c = Db.Select<CouponPromo>(x => x.OrderByDescending(m => m.IssueOn).Limit((page - 1) * items_per_page, items_per_page));

            ViewData["pages"] = pages;
            ViewData["page"] = page;
            ViewData["items_per_page"] = items_per_page;
            ViewData["total_items"] = item_count;
            ViewData["action"] = "Index";

            return PartialView("_List", c);
        }

        public ActionResult Add()
        {
            CouponPromo model = new CouponPromo();

            model.AppliedOptions = new List<long>();

            model.ExceptProducts = new List<long>();

            model.BeginDate = DateTime.Now;

            model.EndDate = DateTime.Now.AddDays(30);

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            var model = Db.Where<CouponPromo>(m => m.Id == id).FirstOrDefault();

            if (model == null)
                return Redirect("/");

            if (model.AppliedOptions == null)
            {
                model.AppliedOptions = new List<long>();
            }
            if (model.ExceptProducts == null)
            {
                model.ExceptProducts = new List<long>();
            }

            return View("Add", model);
        }

        [HttpPost]
        public ActionResult Update(CouponPromo model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            if (model.AppliedOptions == null)
            {
                model.AppliedOptions = new List<long>();
            }
            if (model.ExceptProducts == null)
            {
                model.ExceptProducts = new List<long>();
            }
            if (model.Used < 0)
            {
                model.Used = 0;
            }
            if (model.MaxUse <= 0)
            {
                model.MaxUse = 1;
            }
            if (model.Used > model.MaxUse)
            {
                model.Used = model.MaxUse;
            }
            if (model.AppliedOptions == null)
            {
                model.AppliedOptions = new List<long>();
            }
            if (model.ExceptProducts == null)
            {
                model.ExceptProducts = new List<long>();
            }
            if (string.IsNullOrEmpty(model.CountryCode) || model.CountryCode == "any")
            {
                model.CountryCode = "";
            }

            if (model.CouponTypeEnum == Enum_CouponType.Monthly_PromoCode)
            {
                // for Monthly Promotion code, force always percent discount
                model.isPercentDiscount = true;
            }
            else if (model.CouponTypeEnum == Enum_CouponType.Groupon)
            {
                model.isPercentDiscount = false; // always fix amount for groupon
            }

            model.Code = string.IsNullOrEmpty(model.Code) ? "" : model.Code.ToSeoUrl().ToUpper();

            if (Db.Select<CouponPromo>(x => x.Where(y => (y.Code == model.Code && y.Id != model.Id)).Limit(1)).FirstOrDefault() != null)
            {
                return JsonError("Please enter another coupon code or leave blank.");
            }

            CouponPromo current_item = new CouponPromo();

            if (model.Id > 0)
            {
                var z = Db.Where<CouponPromo>(m => m.Id == model.Id);
                if (z.Count == 0)
                {
                    // the ID is not exist
                    return JsonError("Please dont try to hack us.");
                }
                else
                {
                    current_item = z.First();
                }
            }

            model.IssueOn = model.Id != 0 ? current_item.IssueOn : DateTime.Now;

            if (model.Id == 0)
            {
                Db.Insert<CouponPromo>(model);
                model.Id = Db.GetLastInsertId();
            }
            else
            {
                Db.Update<CouponPromo>(model);
            }

            if (string.IsNullOrEmpty(model.Code))
            {
                // auto generate the CODE
                model.Code = string.Format("PTBM{0}{1}{2}", DateTime.Now.Year, DateTime.Now.Month.ToString("00"), model.Id.ToString("00"));
                Db.Update<CouponPromo>(model);
            }

            return JsonSuccess(Url.Action("Index", "CouponPromo", new { }));
        }

        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<CouponPromo>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult PurgeExpired()
        {
            try
            {
                long count = Db.Count<CouponPromo>(x => (x.EndDate < DateTime.Now));

                Db.Delete<CouponPromo>(x => x.Where(y => (y.EndDate < DateTime.Now)));

                return JsonSuccess(Url.Action("Index", "CouponPromo", new { }), string.Format("{0} expired coupons have been purge.", count));
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Search(SearchCouponModel model)
        {
            var p = PredicateBuilder.True<CouponPromo>();

            bool canPredicate = false;

            if (model.Type != null)
            {
                canPredicate = true;

                p = p.And(x => x.CouponType == model.Type.Value);
            }

            if (!string.IsNullOrEmpty(model.Key))
            {
                canPredicate = true;

                p = p.And(x => x.Code.Contains(model.Key) || x.IssueTo.Contains(model.Key));
            }

            if (model.IssuedOnBefore != null)
            {
                canPredicate = true;

                p = p.And(x => x.BeginDate >= model.IssuedOnBefore.Value);
            }

            if (model.IssuedOnAfter != null)
            {
                canPredicate = true;

                p = p.And(x => x.EndDate <= model.IssuedOnAfter.Value);
            }

            List<CouponPromo> c = new List<CouponPromo>();

            if (canPredicate)
            {
                c = Db.Select<CouponPromo>(p).OrderByDescending(x => (x.IssueOn)).ToList();
            }
            else
            {
                c = Db.Select<CouponPromo>(x => x.OrderByDescending(y => (y.IssueOn)));
            }

            ViewData["page"] = 1;

            ViewData["pages"] = 1;

            ViewData["items_per_page"] = c.Count;

            ViewData["total_items"] = c.Count;

            ViewData["action"] = "Index";

            return PartialView("_List", c);
        }

        /// <summary>
        /// Export all security code to excel
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ExportToExcel(CouponPromo model)
        {
            bool canPredicate = false;

            var p = PredicateBuilder.True<CouponPromo>();

            // search by date
            if (model.SecurityCode == "CreateDate")
            {
                if (model.BeginDate > DateTime.MinValue)
                {
                    canPredicate = true;

                    p = p.And(x => x.IssueOn >= model.BeginDate);
                }
                if (model.EndDate > DateTime.MinValue)
                {
                    canPredicate = true;

                    p = p.And(x => x.IssueOn <= model.EndDate);
                }
            }
            else
                if (model.SecurityCode == "UsedDate")
                {
                    if (model.BeginDate > DateTime.MinValue)
                    {
                        canPredicate = true;

                        p = p.And(x => x.LastUsed >= model.BeginDate);
                    }
                    if (model.EndDate > DateTime.MinValue)
                    {
                        canPredicate = true;

                        p = p.And(x => x.LastUsed <= model.EndDate);
                    }
                }
                else if (model.SecurityCode == "BeginEndDate")
                {
                    if (model.BeginDate > DateTime.MinValue)
                    {
                        canPredicate = true;

                        p = p.And(x => x.BeginDate >= model.BeginDate);
                    }
                    if (model.EndDate > DateTime.MinValue)
                    {
                        canPredicate = true;

                        p = p.And(x => x.EndDate <= model.EndDate);
                    }
                }


            if (model.IssueTo != "ANY")
            {
                canPredicate = true;

                if (string.IsNullOrEmpty(model.IssueTo))
                {
                    p = p.And(x => (x.IssueTo == null || x.IssueTo == ""));
                }
                else
                {
                    p = p.And(x => (x.IssueTo.Contains(model.IssueTo)));
                }
            }

            if (model.CouponType != -1)
            {
                canPredicate = true;

                p = p.And(x => (x.CouponType == model.CouponType));
            }

            List<CouponPromo> data = canPredicate ? Db.Where<CouponPromo>(p) : Db.Where<CouponPromo>(true);

            using (var package = new ExcelPackage())
            {
                package.Workbook.Worksheets.Add("List Coupon Promotion");
                ExcelWorksheet ws = package.Workbook.Worksheets[1];
                ws.Name = "List Coupon Promotion"; //Setting Sheet's name
                ws.Cells.Style.Font.Size = 12; //Default font size for whole sheet
                ws.Cells.Style.Font.Name = "Calibri"; //Default Font name for whole sheet

                //Merging cells and create a center heading for out table
                ws.Cells[1, 1].Value = "List Coupon Promotion"; // Heading Name
                ws.Cells[1, 1].Style.Font.Size = 22;
                ws.Cells[1, 1, 1, 10].Merge = true; //Merge columns start and end range
                ws.Cells[1, 1, 1, 10].Style.Font.Bold = true; //Font should be bold
                ws.Cells[1, 1, 1, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Aligmnet is center

                int row_index = 2;

                // header
                List<string> ws_header = new List<string>();
                ws_header.Add("");
                ws_header.Add("Id");
                ws_header.Add("Type");
                ws_header.Add("Code");
                ws_header.Add("SecurityCode");
                ws_header.Add("Require Security Code");
                ws_header.Add("Used On");
                ws_header.Add("Issued On");
                ws_header.Add("Issued To");
                ws_header.Add("Begin");
                ws_header.Add("End");
                ws_header.Add("Used");
                ws_header.Add("Maximum");
                ws_header.Add("Percent Discount");
                ws_header.Add("Fixed Amount Discount");
                ws_header.Add("Apply to Options");
                ws_header.Add("Apply to Products");
                ws_header.Add("Discount Amount");
                ws_header.Add("Country");
                //ws_header.Add("Discount Amount in Difference Currency");
                //ws_header.Add("Currency");

                for (int i = 1; i < ws_header.Count; i++)
                {
                    ws.Cells[2, i].Value = ws_header[i];
                    ws.Cells[2, i].Style.Font.Bold = true;
                    ws.Cells[2, i].Style.Font.Size = 14;
                }

                var countries = Db.Select<Country>();
                int row_id = 0;
                int backup_row_index = row_index + 1;
                foreach (var item in data) // list all item in each company
                {
                    // get the country
                    Country country = new Country();
                    if (!(string.IsNullOrEmpty(item.CountryCode) || item.CountryCode == "any"))
                    {
                        country = countries.Where(x => x.Code == item.CountryCode).FirstOrDefault();
                    }

                    row_id++;
                    row_index++;

                    // Id
                    var col_index = 1;
                    ws.Cells[row_index, col_index].Value = item.Id;

                    // Type
                    col_index++;
                    ws.Cells[row_index, col_index].Value =
                        (item.CouponType == (int)Enum_CouponType.Monthly_PromoCode) ? "Monthy Promotion Code" : (
                        (item.CouponType == (int)Enum_CouponType.Groupon) ? "Groupon" : "");

                    // Code
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.Code;

                    // SecuirtyCode
                    col_index++;
                    if (item.CouponTypeEnum == Enum_CouponType.Groupon && !string.IsNullOrEmpty(item.SecurityCode))
                    {
                        ws.Cells[row_index, col_index].Value = item.SecurityCode;
                    }
                    else
                    {
                        ws.Cells[row_index, col_index].Value = "";
                    }

                    // Secuirty
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.SecurityCodeRequired;

                    // Used On
                    col_index++;
                    if (item.LastUsed.Year > 2013)
                    {
                        ws.Cells[row_index, col_index].Value = string.Format("{0:MM/dd/yyyy HH:mm:ss}", item.LastUsed);
                    }
                    else
                    {
                        ws.Cells[row_index, col_index].Value = "";
                    }

                    // Issued On
                    col_index++;
                    ws.Cells[row_index, col_index].Value = string.Format("{0:MM/dd/yyyy HH:mm:ss}", item.IssueOn);

                    // Issued To
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.IssueTo;

                    // Begin
                    col_index++;
                    ws.Cells[row_index, col_index].Value = string.Format("{0:MM/dd/yyyy HH:mm:ss}", item.BeginDate);

                    // End
                    col_index++;
                    ws.Cells[row_index, col_index].Value = string.Format("{0:MM/dd/yyyy HH:mm:ss}", item.EndDate);

                    // Used
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.Used;

                    // Maximum
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.MaxUse;

                    // percent discount
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.isPercentDiscount;

                    // fixed amount discount
                    col_index++;
                    ws.Cells[row_index, col_index].Value = !item.isPercentDiscount;

                    // apply to option
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.isApplyToOption;

                    // apply to products
                    col_index++;
                    ws.Cells[row_index, col_index].Value = !item.isApplyToOption;



                    // discount amount
                    col_index++;
                    if (country != null && country.Id > 0)
                    {
                        ws.Cells[row_index, col_index].Value = item.DiscountAmount.ToMoneyFormated(country.CurrencyCode);
                    }
                    else
                    {
                        ws.Cells[row_index, col_index].Value = item.DiscountAmount;
                    }

                    // country
                    col_index++;

                    if (country != null && country.Id > 0)
                    {
                        ws.Cells[row_index, col_index].Value = country.Name;
                    }
                    else
                    {
                        ws.Cells[row_index, col_index].Value = "Deleted Country";
                    }

                } // end record detail

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
                var fileName = string.Format("List Coupon Promotion-{0:yyyy-MM-dd-HH-mm-ss}.xlsx", DateTime.Now);
                // mimetype from http://stackoverflow.com/questions/4212861/what-is-a-correct-mime-type-for-docx-pptx-etc
                return base.File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        /// <summary>
        /// Import groupon code from excel file
        /// </summary>
        /// <param name="model"></param>
        /// <param name="FileUp"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ImportFromExcel(CouponPromo model, IEnumerable<HttpPostedFileBase> FileUp)
        {
            // check the file
            if (FileUp != null && FileUp.FirstOrDefault() != null)
            {
                if (model.AppliedOptions == null)
                {
                    model.AppliedOptions = new List<long>();
                }
                if (model.ExceptProducts == null)
                {
                    model.ExceptProducts = new List<long>();
                }
                if (model.Used < 0)
                {
                    model.Used = 0;
                }
                if (model.MaxUse < 1)
                {
                    model.MaxUse = 1;
                }
                if (model.Used > model.MaxUse)
                {
                    model.Used = model.MaxUse;
                }
                model.IssueOn = DateTime.Now;
                model.CouponType = (int)Enum_CouponType.Groupon;
                model.isPercentDiscount = false; // groupon always fix amount discount
                if (model.AppliedOptions == null)
                {
                    model.AppliedOptions = new List<long>();
                }
                if (model.ExceptProducts == null)
                {
                    model.ExceptProducts = new List<long>();
                }

                // read the file content
                foreach (var file in FileUp)
                {
                    if (file != null)
                    {
                        if (file.ContentLength > 0)
                        {
                            /// check file extension
                            var ext = Path.GetExtension(file.FileName).ToLower();

                            if (ext == ".xls" || ext == ".xlsx")
                            {
                                int i = 1;
                                int imported = 0;
                                // Open and read the XlSX file.
                                using (var package = new ExcelPackage(file.InputStream))
                                {
                                    // Get the work book in the file
                                    ExcelWorkbook workBook = package.Workbook;
                                    if (workBook != null)
                                    {
                                        if (workBook.Worksheets.Count > 0)
                                        {
                                            // Get the first worksheet
                                            ExcelWorksheet currentWorksheet = workBook.Worksheets.First();

                                            // read data by loop until we get null string

                                            do
                                            {
                                                // read some data

                                                string data = currentWorksheet.Cells["A" + i.ToString()].Text;

                                                // remove ' for some excel file
                                                if (data.StartsWith("'"))
                                                {
                                                    data = data.Substring(1);
                                                }

                                                if (string.IsNullOrEmpty(data))
                                                {
                                                    break; // get out of the loop
                                                }

                                                // check duplication
                                                var x = Db.Count<CouponPromo>(w => w.Code == data);

                                                if (x == 0)
                                                {
                                                    // insert coupon
                                                    CouponPromo cp = model.TranslateTo<CouponPromo>();

                                                    cp.Code = data;

                                                    Db.Insert<CouponPromo>(cp);

                                                    imported++;
                                                }
                                                i++;

                                            } while (0 < 1);
                                        }
                                    }
                                }
                                // finish import
                                ViewBag.Notice = string.Format("You have imported {0} coupon security codes.", imported);
                            }
                        }

                    }
                }
            }
            else
            {
                ViewBag.Error = "You did not upload file or system can not read file in correct format.";
            }

            return RedirectToAction("Index", "CouponPromo", new { });
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ImportExcelDelete(IEnumerable<HttpPostedFileBase> FileUps)
        {
            if (FileUps != null && FileUps.FirstOrDefault() != null)
            {
                foreach (var file in FileUps)
                {
                    if (file != null && file.ContentLength != 0)
                    {
                        var ext = Path.GetExtension(file.FileName).ToLower();

                        if (ext == ".xls" || ext == ".xlsx")
                        {
                            int row = 3;

                            int imported = 0;

                            using (var package = new ExcelPackage(file.InputStream))
                            {
                                ExcelWorkbook workBook = package.Workbook;

                                if (workBook != null && workBook.Worksheets.Count != 0)
                                {
                                    ExcelWorksheet currentWorksheet = workBook.Worksheets.First();

                                    do
                                    {
                                        long Id = 0;

                                        string data = currentWorksheet.Cells["A" + row.ToString()].Text;

                                        if (string.IsNullOrEmpty(data) || !long.TryParse(data, out Id))
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            if (Db.Count<CouponPromo>(x => x.Id == Id) > 0)
                                            {
                                                Db.DeleteById<CouponPromo>(Id);

                                                imported++;
                                            }
                                        }

                                        row++;

                                    } while (true);
                                }
                            }

                            ViewBag.Notice = string.Format("You have imported {0} coupon", imported);
                        }
                    }
                }
            }
            else
            {
                ViewBag.Error = "You did not upload file or system can not read file in correct format.";
            }

            return RedirectToAction("Index", "CouponPromo", new { });
        }

        #endregion
    }
}