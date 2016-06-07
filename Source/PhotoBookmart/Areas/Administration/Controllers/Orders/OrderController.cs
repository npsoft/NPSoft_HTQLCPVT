using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
//using System.Web.UI;
//using System.Web.UI.WebControls;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.Controllers;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.System;
//using Ionic.Zip;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.Lib;
using PhotoBookmart.Support;
using PhotoBookmart.DataLayer.Models.Sites;
using PhotoBookmart.DataLayer.Models.Reports;
using PhotoBookmart.DataLayer.Models.SMS;
using PhotoBookmart.Tasks;
using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using System.Collections;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    public class OrderController : WebAdminController
    {
        int items_per_page = 20;

        #region Order Management
        //
        public ActionResult Index(int page = 1)
        {
            var model = new OrderFilterModel();
            //model.BetweenDate = DateTime.Now.AddDays(-1);
            //model.AndDate = DateTime.Now;
            ViewData["page"] = page;
            return View(model);
        }

        /// <summary>
        /// List of all orders based on this users context
        /// </summary>
        /// <returns></returns>
        public ActionResult List(int page)
        {
            // for pagination
            int pages = 0;
            int item_count = 0;

            if (page < 0)
            {
                page = 0;
            }

            var current_user = CurrentUser;

            List<Order> c = new List<Order>();

            // check user role
            var user = CurrentUser;
            bool hasSpecialRole = current_user.HasRole(RoleEnum.OrderManagement) || current_user.HasRole(RoleEnum.Administrator);
            var isAdmin = user.HasRole(RoleEnum.Administrator);
            // get the list of available roles
            var order_status = OrderLib._GetUserAvailableRoleToCheckOrder(current_user.Roles);

            // filter by role
            if (user.HasRole(RoleEnum.OrderManagement) || user.HasRole(RoleEnum.Administrator))
            {
                item_count = (int)Db.Count<Order>();
                pages = item_count / items_per_page;
                if (item_count % items_per_page > 0)
                {
                    pages++;
                }

                c = Db.Select<Order>(x => x.OrderByDescending(m => m.LastUpdate).Limit((page - 1) * items_per_page, items_per_page));
            }
            else if (!hasSpecialRole)
            {
                // calculate the where conditions
                string sqlFilter = "(WorkingStaff_Id = " + current_user.Id + ") OR ((WorkingStaff_Id = 0) AND (";
                string sqlFilter2 = "";
                foreach (var os in order_status)
                {
                    if (sqlFilter2 != "")
                    {
                        sqlFilter2 += " OR ";
                    }
                    sqlFilter2 += string.Format(" (Status = {0}) ", (int)os);
                }
                sqlFilter += sqlFilter2 + "))";

                // pagination
                item_count = Db.CountByWhere<Order>(sqlFilter);
                pages = item_count / items_per_page;
                if (item_count % items_per_page > 0)
                {
                    pages++;
                }

                c = Db.Select<Order>(x => x.Where(sqlFilter).OrderByDescending(m => m.LastUpdate).Limit((page - 1) * items_per_page, items_per_page));
            }
            else
            {
                c = new List<Order>();
            }

            var users = Db.Where<ABUserAuth>(x => x.Roles.Contains(RoleEnum.Employee.ToString()) || x.Roles.Contains(RoleEnum.Administrator.ToString()) || x.Roles.Contains(RoleEnum.OrderManagement.ToString()));

            var tickets = Db.Select<Order_UploadFilesTicket>();

            foreach (var x in c)
            {
                var ux = users.Where(k => k.Id == x.WorkingStaff_Id).FirstOrDefault();
                if (ux != null)
                {
                    x.WorkingStaff_Name = ux.FullName;
                }
                else
                {
                    x.WorkingStaff_Name = "";
                }

                var ticket = tickets.Where(y => y.OrderId == x.Id).FirstOrDefault();

                if (ticket != null)
                {
                    x.UploadFilesTicketStatus = ((Enum_UploadFilesTicketStatus)ticket.Status).ToString();
                }
                else
                {
                    x.UploadFilesTicketStatus = "";
                }
            }

            ViewData["pages"] = pages;
            ViewData["page"] = page;
            ViewData["total_items"] = item_count;
            ViewData["action"] = "Index";

            return PartialView("_List", c);

        }

        /// <summary>
        /// Search order
        /// </summary>
        /// <returns></returns>
        // Now allow all staff can access to this action
        //[ABRequiresAnyRole(RoleEnum.OrderManagement, RoleEnum.Administrator, RoleEnum.Received)]
        [HttpPost]
        public ActionResult Search(OrderFilterModel model, int page = 1)
        {
            // for pagination
            int pages = 0;
            int item_count = 0;

            if (page < 0)
            {
                page = 0;
            }

            var current_user = CurrentUser;

            List<Order> c = new List<Order>();

            // passing the joinsqlbuilder into the expression to solve the pagination
            JoinSqlBuilder<Order, Order> jn = new JoinSqlBuilder<Order, Order>();
            if (model.Condition == "order_with_file_corrupted")
            {
                jn.Join<Order, Order_UploadFilesTicket>(m => m.Id, k => k.OrderId);
            }
            else
            {
                jn = jn.LeftJoin<Order, Order_ProductOptionUsing>(m => m.Id, k => k.Order_Id);
            }
            jn = jn.SelectDistinct();

            // check user role
            var user = CurrentUser;
            bool hasSpecialRole = current_user.HasRole(RoleEnum.OrderManagement) || current_user.HasRole(RoleEnum.Administrator);

            // build condition
            var p = PredicateBuilder.True<Order>();

            //// for date
            //if (!(model.BetweenDate == DateTime.MinValue || model.AndDate == DateTime.MinValue))
            //    p = p.And(m => m.CreatedOn >= model.BetweenDate && m.CreatedOn <= model.AndDate);
            //else
            //    p = p.And(m => m.CreatedOn < DateTime.Now);

            // depend on the search by, we do the search based on
            switch (model.SortBy)
            {
                case "lastupdate":
                    if (!(model.BetweenDate == DateTime.MinValue || model.AndDate == DateTime.MinValue))
                        p = p.And(m => m.LastUpdate >= model.BetweenDate && m.LastUpdate <= model.AndDate);
                    else
                        p = p.And(m => m.LastUpdate < DateTime.Now);
                    break;
                case "paiddate":
                    if (!(model.BetweenDate == DateTime.MinValue || model.AndDate == DateTime.MinValue))
                        p = p.And(m => m.PaidDate >= model.BetweenDate && m.PaidDate <= model.AndDate);
                    else
                        p = p.And(m => m.PaidDate < DateTime.Now);
                    break;
                default:
                    if (!(model.BetweenDate == DateTime.MinValue || model.AndDate == DateTime.MinValue))
                        p = p.And(m => m.CreatedOn >= model.BetweenDate && m.CreatedOn <= model.AndDate);
                    else
                        p = p.And(m => m.CreatedOn < DateTime.Now);
                    break;
            }

            if (!string.IsNullOrEmpty(model.Search))
            {
                p = p.And(m => m.Order_Number.Contains(model.Search) || m.Coupon_Code.Contains(model.Search) || m.Customer_Name.Contains(model.Search) || m.Customer_Email.Contains(model.Search) || m.Product_Name.Contains(model.Search) || m.Shipping_TrackingNumber.Contains(model.Search) || m.ShippingNote.Contains(model.Search));
            }

            // if staff has role Received, then force the order status to Recieved
            if (!hasSpecialRole) //  if admin and manager access then we will show all orders, if not then we will limit the order status
            {
                // get the list of available roles
                var order_status = OrderLib._GetUserAvailableRoleToCheckOrder(current_user.Roles);

                //p = p.And(x => x.Status == (int)Enum_OrderStatus.Received);

                Expression<Func<Order, bool>> pi = (k) => false;
                foreach (var ost in order_status)
                {
                    switch (ost)
                    {
                        case Enum_OrderStatus.Finished:
                            pi = pi.Or(x => x.Status == (int)Enum_OrderStatus.Finished);
                            break;
                        case Enum_OrderStatus.Printing:
                            pi = pi.Or(x => x.Status == (int)Enum_OrderStatus.Printing);
                            break;
                        case Enum_OrderStatus.Processing:
                            pi = pi.Or(x => x.Status == (int)Enum_OrderStatus.Processing);
                            break;
                        case Enum_OrderStatus.Production:
                            pi = pi.Or(x => x.Status == (int)Enum_OrderStatus.Production);
                            break;
                        case Enum_OrderStatus.QC_AfterFilePriting:
                            pi = pi.Or(x => x.Status == (int)Enum_OrderStatus.QC_AfterFilePriting);
                            break;
                        case Enum_OrderStatus.QC_AfterFileProcessing:
                            pi = pi.Or(x => x.Status == (int)Enum_OrderStatus.QC_AfterFileProcessing);
                            break;
                        case Enum_OrderStatus.Received:
                            pi = pi.Or(x => x.Status == (int)Enum_OrderStatus.Received);
                            break;
                        case Enum_OrderStatus.Shipping:
                            pi = pi.Or(x => x.Status == (int)Enum_OrderStatus.Shipping);
                            break;
                        case Enum_OrderStatus.Verify:
                            pi = pi.Or(x => x.Status == (int)Enum_OrderStatus.Verify);
                            break;
                        default:
                            break;
                    }
                }

                // insert the expression into our condition
                if (order_status != null && order_status.Count > 0)
                {
                    p = p.And(pi);
                }
            }
            else
            {
                // status = in processing
                if (model.Status == 1)
                {
                    p = p.And(x => x.Status > (int)Enum_OrderStatus.Received && x.Status < (int)Enum_OrderStatus.Finished && x.Payment_isPaid);
                }
                else if (model.Status == 2)  // finish
                {
                    p = p.And(x => x.Status == 9);
                }
                else if (model.Status == 3) // cancel
                {
                    p = p.And(x => x.Status == (int)Enum_OrderStatus.Canceled);
                }
                else if (model.Status == 4) // refund
                {
                    p = p.And(x => x.Status == (int)Enum_OrderStatus.Refund);
                }
                else if (model.Status == 5) // in shipping
                {
                    p = p.And(x => x.Status == (int)Enum_OrderStatus.Shipping);
                }
            }

            // staff status
            if (model.StaffStatus == 1)
            {
                // working
                p = p.And(x => x.WorkingStaff_Id > 0);
            }
            else if (model.StaffStatus == 2)
            {
                p = p.And(x => x.WorkingStaff_Id == 0);
            }

            // coupon
            if (model.Coupon > -1)
            {
                if (model.Coupon == 1)
                {
                    // use coupon
                    p = p.And(x => x.isUseCoupon);

                    /// coupon type
                    if (model.CouponType == 0 || model.CouponType == 1)
                    {
                        p = p.And(x => x.CouponType == model.CouponType);
                    }
                }
                else if (model.Coupon == 2)
                {
                    // not use 
                    p = p.And(x => x.isUseCoupon == false);
                }
            }

            // currency
            if (!string.IsNullOrEmpty(model.Currency) && model.Currency != "any")
            {
                p = p.And(x => x.Shipping_DisplayPriceSign == model.Currency);
            }

            // filter show only new message from customers
            if (model.Condition == "new_message")
            {
                p = p.And(x => x.FlagHistoryMessage == (int)Enum_FlagOrderMessage.NewMessageFromCustomer);
            }
            else if (model.Condition == "payment")
            {
                p = p.And(x => x.Payment_isPaid && x.PaymentStatus == (int)Enum_PaymentStatus.Paid);
            }
            else if (model.Condition == "payment_unpaid")
            {
                p = p.And(x => (x.Status > 0) && (x.Payment_isPaid == false || (x.PaymentStatus != (int)Enum_PaymentStatus.Paid)));
            }
            else if (model.Condition == "photobook_need_todelete")
            {
                p = p.And(x => (x.Order_Photobook_Deleted == false) && (x.Status == (int)Enum_OrderStatus.Canceled || x.Status == (int)Enum_OrderStatus.Finished || x.Status == (int)Enum_OrderStatus.Refund));
            }
            else if (model.Condition == "order_with_file_corrupted")
            {
                // khách hàng muốn lấy hết tất cả các trường hợp
                jn.Where<Order_UploadFilesTicket>(x => x.Status >= 0);
            }

            // assign the where condition
            jn = jn.Where(p);
            // product options
            if (model.ProductOptions != null && model.ProductOptions.Count > 0)
            {
                jn = jn.And<Order_ProductOptionUsing>(x => Sql.In(x.Option_Id, model.ProductOptions.ToArray()));
            }

            // create the sql expression to query data
            SqlExpressionVisitor<Order> sql_exp = Db.CreateExpression<Order>();
            var st = jn.ToSql();
            if (st.IndexOf("WHERE") > 0)
            {
                var t_where = st.IndexOf("WHERE");
                sql_exp.SelectExpression = st.Substring(0, t_where);
                sql_exp.WhereExpression = st.Substring(t_where);
            }
            else
            {
                sql_exp.SelectExpression = st;
            }

            // sort by
            switch (model.SortBy)
            {
                case "lastupdate":
                    sql_exp = sql_exp.OrderByDescending(m => m.LastUpdate);
                    break;
                case "createon":
                    sql_exp = sql_exp.OrderByDescending(m => m.CreatedOn);
                    break;
                case "status":
                    sql_exp = sql_exp.OrderByDescending(m => m.Status);
                    break;
                case "paiddate":
                    sql_exp = sql_exp.OrderByDescending(m => m.PaidDate);
                    break;
                default:
                    sql_exp = sql_exp.OrderByDescending(m => m.LastUpdate);
                    break;
            }

            if (model.ResultType == 0)
            {
                if (model.Condition == "order_with_file_corrupted")
                {
                    item_count = Db.Select<Order>(jn.ToSql()).Count;
                }
                else
                {
                    item_count = (int)Db.Count<Order>(p);
                }
                pages = item_count / items_per_page;
                if (item_count % items_per_page > 0)
                {
                    pages++;
                }
                sql_exp = sql_exp.Limit((page - 1) * items_per_page, items_per_page);
            }

            // get data
            var sql = sql_exp.ToSelectStatement();
            if (model.ResultType == 0)
            {
                if (page > 1)
                {
                    // we need to replace the SELECT X,Y,Z field to avoid the exception
                    var t_FROM = sql.IndexOf("FROM ");
                    if (t_FROM > 0)
                    {
                        sql = "SELECT DISTINCT * " + sql.Substring(t_FROM);
                    }
                }
                else
                {
                    var t_FROM = sql.IndexOf("DISTINCT");
                    if (t_FROM > 0)
                    {
                        sql = "SELECT DISTINCT TOP  " + items_per_page + sql.Substring(t_FROM + 8); // bypass DISTINCT
                    }
                }
            }

            #region Execute For Order With File Corrupted

            if (model.ResultType == 0 && page > 1 && model.Condition == "order_with_file_corrupted")
            {
                string org1 = "ORDER BY ", new1 = "ORDER BY \"Products\".\"Order\".";
                string org2 = "*", new2 = "\"Products\".\"Order\".*";
                string org3 = "FROM \"Products\".\"Order\" WHERE", new3 = "FROM \"Products\".\"Order\" INNER JOIN \"Products\".\"Order_UploadFilesTicket\" ON \"Products\".\"Order\".\"Id\" = \"Products\".\"Order_UploadFilesTicket\".\"OrderId\" WHERE";
                string org4 = "\"Order\".", new4 = "\"Products\".\"Order\".";

                // 1. Tìm "ORDER BY" đầu tiên, thay thế bằng "ORDER BY Products.[Order]."
                int index = sql.IndexOf(org1);
                sql = sql.Substring(0, index) + new1 + sql.Substring(index + org1.Length, sql.Length - index - org1.Length);

                // 2. Tìm "*" thứ hai, thay thế bằng "Products.[Order].*"
                index += sql.Substring(index, sql.Length - index).IndexOf(org2);
                sql = sql.Substring(0, index) + new2 + sql.Substring(index + org2.Length, sql.Length - index - org2.Length);

                // 3. Tìm "FROM Products.[Order] WHERE", thay thế bằng "FROM Products.[Order] INNER JOIN Products.Order_UploadFilesTicket ON Products.[Order].Id = Products.Order_UploadFilesTicket.OrderId WHERE"
                index += sql.Substring(index, sql.Length - index).IndexOf(org3);
                sql = sql.Substring(0, index) + new3 + sql.Substring(index + org3.Length, sql.Length - index - org3.Length);

                // 4. Tìm "[Order].", thay thế bằng "Products.[Order]."
                string where = sql.Substring(index + new3.Length, sql.Length - index - new3.Length);
                where = where.Replace(org4, new4);
                sql = sql.Substring(0, index + new3.Length) + where;
            }

            #endregion

            c = Db.Select<Order>(sql);

            var users = Db.Where<ABUserAuth>(x => x.Roles.Contains(RoleEnum.Employee.ToString()) || x.Roles.Contains(RoleEnum.Administrator.ToString()) || x.Roles.Contains(RoleEnum.OrderManagement.ToString()));

            var tickets = Db.Select<Order_UploadFilesTicket>();

            foreach (var x in c)
            {
                var ux = users.Where(k => k.Id == x.WorkingStaff_Id).FirstOrDefault();
                if (ux != null)
                {
                    x.WorkingStaff_Name = ux.FullName;
                }
                else
                {
                    x.WorkingStaff_Name = "";
                }

                var ticket = tickets.Where(y => y.OrderId == x.Id).FirstOrDefault();

                if (ticket != null) x.UploadFilesTicketStatus = ((Enum_UploadFilesTicketStatus)ticket.Status).ToString();
            }

            if (model.ResultType == 1)
            {
                return Search_Export_To_Excel(c);
            }
            else if (model.ResultType == 3)
            {
                Response.Clear();
                Response.ClearContent();
                Response.ClearHeaders();

                #region Export Shipping
                string fileName = string.Format("Orders-Shipping-Information-{0}-{1:MM-dd-yyyy}", model.Shipping_Method.ToString(), DateTime.Today);

                if (model.Shipping_Method == Enum_ShippingType.Aramex)
                {
                    fileName += ".xlsx";

                    return base.File(ExportOrderShippingXSLAramexModel.ExportToArrByte(c), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else if (model.Shipping_Method == Enum_ShippingType.TNT)
                {
                    fileName += ".csv";

                    Response.Clear();

                    Response.ClearHeaders();

                    Response.ContentType = "text/csv";

                    Response.AddHeader("Content-Disposition", "attachment;filename=" + fileName);

                    MemoryStream mst = new MemoryStream(ExportOrderShippingCsvTNTModel.ExportToArrByte(c));

                    Response.OutputStream.Write(mst.ToArray(), 0, mst.ToArray().Length);

                    Response.End();
                }
                #endregion
            }
            else if (model.ResultType == 2)
            {
                Response.Clear();
                Response.ClearContent();
                Response.ClearHeaders();

                #region Production Sheet
                return Export_ProductionSheet(c);
                #endregion
            }
            else if (model.ResultType == 4)
            {
                #region Individual Sheet
                OrderLib ol = new OrderLib(new Order());
                Response.Clear();
                Response.ClearContent();
                Response.ClearHeaders();
                Response.ContentType = "application/pdf";
                Response.Charset = string.Empty;
                Response.Cache.SetCacheability(System.Web.HttpCacheability.Public);
                Response.AddHeader("Content-Disposition", "attachment; filename=" + string.Format("individual-sheet-{0:yyyy-MM-dd-HH-mm-ss}.pdf", DateTime.Now));

                var data = ol.IndividualSheet_PDfGenerator(((string)Settings.Get(Enum_Settings_Key.WEBSITE_ADDITIONAL_PAGE_NAME, "Additional Page", Enum_Settings_DataType.String)).Trim(), c);
                Response.OutputStream.Write(data.GetBuffer(), 0, data.GetBuffer().Length);
                Response.OutputStream.Flush();
                Response.OutputStream.Close();
                Response.End();

                #endregion
            }
            else
            {
                ViewData["pages"] = pages;
                ViewData["page"] = page;
                ViewData["total_items"] = item_count;
                ViewData["action"] = "Search";

                return PartialView("_List", c);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Export the search result to excel
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        ActionResult Search_Export_To_Excel(List<Order> model)
        {
            using (var package = new ExcelPackage())
            {
                package.Workbook.Worksheets.Add(string.Format("{0:yyyy-MM-dd}", DateTime.Now));
                ExcelWorksheet ws = package.Workbook.Worksheets[1];
                ws.Name = "Order List"; //Setting Sheet's name
                ws.Cells.Style.Font.Size = 12; //Default font size for whole sheet
                ws.Cells.Style.Font.Name = "Calibri"; //Default Font name for whole sheet

                //Merging cells and create a center heading for out table
                ws.Cells[1, 1].Value = "List of Photobookmart Orders"; // Heading Name
                ws.Cells[1, 1].Style.Font.Size = 22;
                ws.Cells[1, 1, 1, 10].Merge = true; //Merge columns start and end range
                ws.Cells[1, 1, 1, 10].Style.Font.Bold = true; //Font should be bold
                ws.Cells[1, 1, 1, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Aligmnet is center

                int row_index = 2;

                // header
                List<string> ws_header = new List<string>();
                ws_header.Add("");
                ws_header.Add("");
                ws_header.Add("Order Number");
                ws_header.Add("Completed");
                ws_header.Add("Status");
                ws_header.Add("Customer");
                ws_header.Add("Customer Email");
                ws_header.Add("Product Code");
                ws_header.Add("Product");
                ws_header.Add("Bill Total");
                ws_header.Add("Shipping Fee");
                ws_header.Add("Shipping Method");
                ws_header.Add("Shipping Status");
                ws_header.Add("Shipping Tracking Number");
                ws_header.Add("Shipped On");
                ws_header.Add("Shipped Note");
                ws_header.Add("Payment Method");
                ws_header.Add("Payment Status");
                ws_header.Add("Paid On");
                ws_header.Add("Use Coupon");
                ws_header.Add("Coupon Code");
                ws_header.Add("Coupon Security Code");
                ws_header.Add("Total Discount");
                ws_header.Add("Order On");

                for (int i = 1; i < ws_header.Count; i++)
                {
                    ws.Cells[2, i].Value = ws_header[i];
                    ws.Cells[2, i].Style.Font.Bold = true;
                    ws.Cells[2, i].Style.Font.Size = 14;
                }

                int row_id = 0;
                int backup_row_index = row_index + 1;
                foreach (var item in model) // list all item in each company
                {
                    row_id++;
                    row_index++;
                    var col_index = 1;
                    // ID
                    ws.Cells[row_index, col_index].Value = row_id;

                    // Order Number
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.Order_Number;

                    // Completed
                    col_index++;
                    ws.Cells[row_index, col_index].Value = (((double)item.Status + 1) / 10).ToString("0.##%");

                    // Status
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.StatusEnum.DisplayName();

                    //  Customer
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.Customer_Name;

                    // Customer Email
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.Customer_Email;

                    // Product Code
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.Product_Id;

                    // Product
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.Product_Name;

                    // Bill Total
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.Bill_Total.ToMoneyFormated(item.Shipping_DisplayPriceSign);

                    // Shipping Fee
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.Shipping_RealPrice.ToMoneyFormated(item.Shipping_DisplayPriceSign);

                    // Shipping Method
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.Shipping_Method.DisplayName();

                    //Shipping Status
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.Shipping_Status.DisplayName();

                    //Shipping Tracking Number
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.Shipping_TrackingNumber;

                    //Shipped On
                    col_index++;
                    ws.Cells[row_index, col_index].Value = string.Format("{0:MM/dd/yyyy HH:mm:ss}", item.Shipping_ShipOn);

                    //Shipped Note
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.ShippingNote;

                    //Payment Method
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.PaymentMethod.DisplayName();

                    //Payment Status
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.PaymentStatusEnum.DisplayName();

                    //Paid On
                    col_index++;
                    ws.Cells[row_index, col_index].Value = string.Format("{0:MM/dd/yyyy HH:mm:ss}", item.PaidDate);

                    //Use Coupon
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.isUseCoupon;

                    //Coupon Code
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.Coupon_Code;

                    //Coupon Security Code
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.Coupon_SecrectCode;

                    //Total discount
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.Coupon_TotalDiscount.ToMoneyFormated(item.Shipping_DisplayPriceSign);

                    // On
                    col_index++;
                    ws.Cells[row_index, col_index].Value = string.Format("{0:MM/dd/yyyy HH:mm:ss}", item.CreatedOn);
                } // end record detail


                //// footer total
                row_index++;
                ws.Cells[row_index, 2].Value = "Total";
                ws.Cells[row_index, 2].Style.Font.Bold = true;
                ws.Cells[row_index, 2].Style.Font.Italic = true;
                ws.Cells[row_index, 2].Style.Font.Size = 11;
                ws.Cells[row_index, 2, row_index, 3].Merge = true; //Merge columns start and end range

                ws.Cells[row_index, 6].Value = model.Count();


                // freeze data
                ws.View.FreezePanes(3, 3);

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
                var fileName = string.Format("Photobookmart-Order-Search-{0:yyyy-MM-dd-HH-mm-ss}.xlsx", DateTime.Now);
                // mimetype from http://stackoverflow.com/questions/4212861/what-is-a-correct-mime-type-for-docx-pptx-etc
                return base.File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        /// <summary>
        /// Export the orders shipping 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ABRequiresAnyRole(RoleEnum.Administrator, RoleEnum.OrderManagement, RoleEnum.Shipping)]
        public ActionResult ExportOrders(ExportOrderShippingModel model)
        {
            if (model.Orders.Count > 0)
            {

                var sql = "";
                foreach (var x in model.Orders)
                {
                    if (sql.Length > 0)
                    {
                        sql += " OR ";
                    }
                    sql += " Id = " + x.ToString();
                }

                var orders = Db.Select<Order>(sql);

                if (model.ExportResult == "shipping")
                {
                    #region Export Shipping
                    string fileName = string.Format("Orders-Shipping-Information-{0}-{1:MM-dd-yyyy}", model.Shipping_Method.ToString(), DateTime.Today);

                    if (model.Shipping_Method == Enum_ShippingType.Aramex)
                    {
                        fileName += ".xlsx";

                        return base.File(ExportOrderShippingXSLAramexModel.ExportToArrByte(orders), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                    else if (model.Shipping_Method == Enum_ShippingType.TNT)
                    {
                        fileName += ".csv";

                        Response.Clear();

                        Response.ContentType = "text/csv";

                        Response.AddHeader("Content-Disposition", "attachment;filename=" + fileName);

                        MemoryStream mst = new MemoryStream(ExportOrderShippingCsvTNTModel.ExportToArrByte(orders));

                        Response.OutputStream.Write(mst.ToArray(), 0, mst.ToArray().Length);

                        Response.End();
                    }
                    #endregion
                }
                else
                {
                    #region Production Sheet
                    return Export_ProductionSheet(orders);
                    #endregion
                }
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Export the production sheeet
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        [ABRequiresAnyRole(RoleEnum.Administrator, RoleEnum.OrderManagement)]
        ActionResult Export_ProductionSheet(List<Order> orders)
        {
            var fileName = string.Format("ProductionSheet-{0:yyyy-MM-dd-HH-mm-ss}.xlsx", DateTime.Now);
            var extra_field_name = ((string)Settings.Get(Enum_Settings_Key.WEBSITE_ADDITIONAL_PAGE_NAME, "Additional Page", Enum_Settings_DataType.String)).Trim();

            var package = new ExcelPackage();

            package.Workbook.Worksheets.Add("Production Sheet");
            ExcelWorksheet ws = package.Workbook.Worksheets[1];
            ws.Name = "ProductionSheet";
            ws.Cells.Style.Font.Size = 15; //Default font size for whole sheet
            ws.Cells.Style.Font.Name = "Calibri"; //Default Font name for whole sheet

            //Merging cells and create a center heading for out table
            ws.Cells[4, 5].Value = "Production Job Sheet"; // Heading Name
            ws.Cells[4, 5].Style.Font.Size = 22;
            ws.Cells[4, 5, 4, 8].Merge = true; //Merge columns start and end range
            ws.Cells[4, 5, 4, 8].Style.Font.Bold = true; //Font should be bold
            ws.Cells[4, 5, 4, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Aligmnet is center

            // the date
            ws.Cells[4, 1].Value = "Date: " + DateTime.Now.ToString();
            ws.Cells[4, 1, 4, 4].Merge = true; //Merge columns start and end range

            int row_index = 6;

            // header
            List<string> ws_header = new List<string>();
            ws_header.Add("");
            ws_header.Add("No");
            ws_header.Add("File Name");
            ws_header.Add("Paper");
            ws_header.Add("Size");
            ws_header.Add("Orientation");
            ws_header.Add("Style");
            ws_header.Add("Pages");
            ws_header.Add("Quantity");
            ws_header.Add("Cover Material - DEB");
            ws_header.Add("Options");

            for (int i = 1; i < ws_header.Count; i++)
            {
                ws.Cells[6, i].Value = ws_header[i];
                ws.Cells[6, i].Style.Font.Color.SetColor(System.Drawing.Color.White);
                ws.Cells[6, i].Style.Font.Bold = true;
                ws.Cells[6, i].Style.Font.Size = 15;
                ws.Cells[6, i].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[6, i].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Black);
            }

            int row_id = 0;
            int backup_row_index = row_index + 1;
            foreach (var item in orders)
            {
                var product = Db.Select<Product>(x => x.Where(m => m.Id == item.Product_Id).Limit(1)).FirstOrDefault();
                if (product == null)
                {
                    continue;
                }
                var product_cat = Db.Select<Product_Category>(m => m.Where(x => x.Id == product.CatId).Limit(1)).FirstOrDefault();
                var poptions = Db.Select<Order_ProductOptionUsing>(m => m.Where(x => x.Order_Id == item.Id && x.Option_Name.Contains(extra_field_name)).Limit(1)).FirstOrDefault();
                //var poptions_additionalpage = poptions.Where(x =>).FirstOrDefault();
                if (poptions != null)
                {
                    product.Pages += poptions.Option_Quantity;
                }

                row_id++;
                row_index++;

                // Id
                var col_index = 1;
                ws.Cells[row_index, col_index].Value = row_id;
                ws.Cells[row_index, col_index].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // File Name
                col_index++;
                ws.Cells[row_index, col_index].Value = item.Order_Number;
                ws.Cells[row_index, col_index].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Paper
                col_index++;
                ws.Cells[row_index, col_index].Value = product.Paper;
                ws.Cells[row_index, col_index].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Size
                col_index++;
                ws.Cells[row_index, col_index].Value = product.Size;
                ws.Cells[row_index, col_index].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // orientation
                col_index++;
                ws.Cells[row_index, col_index].Value = product.Orientation;
                ws.Cells[row_index, col_index].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Style
                col_index++;
                ws.Cells[row_index, col_index].Value = product_cat.ShortCode;
                ws.Cells[row_index, col_index].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Pages
                col_index++;
                ws.Cells[row_index, col_index].Value = product.Pages;
                ws.Cells[row_index, col_index].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Quantity
                col_index++;
                ws.Cells[row_index, col_index].Value = item.Quantity;
                ws.Cells[row_index, col_index].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Cover Material - DEB
                col_index++;
                if (item.CoverMaterial == 0)
                {
                    // incase no cover then use the category cover
                    ws.Cells[row_index, col_index].Value = product_cat.ShortCode;
                }
                else
                {
                    // get the material
                    var material = Db.Select<ProductCategoryMaterialDetail>(x => x.Where(m => m.Id == item.CoverMaterial).Limit(1)).FirstOrDefault();
                    if (material == null)
                    {
                        ws.Cells[row_index, col_index].Value = "";
                    }
                    else
                    {
                        // get the material cat
                        var mcat = Db.Select<ProductCategoryMaterial>(x => x.Where(m => m.Id == material.ProductCategoryMaterialId).Limit(1)).FirstOrDefault();
                        var name = "";
                        if (mcat != null)
                        {
                            name = mcat.Name + " - ";
                        }
                        name += material.Name;
                        ws.Cells[row_index, col_index].Value = name.ToUpper();
                        material.Dispose();
                        mcat.Dispose();
                    }
                }
                ws.Cells[row_index, col_index].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Options
                col_index++;
                ws.Cells[row_index, col_index].Value = string.Join(", ", TruncOptsByOrderId(item.Id));
                ws.Cells[row_index, col_index].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // dispose
                if (product != null)
                {
                    product.Dispose();
                }
                if (product_cat != null)
                {
                    product_cat.Dispose();
                }
                if (poptions != null)
                {
                    poptions.Dispose();
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
            package.Dispose();
            // mimetype from http://stackoverflow.com/questions/4212861/what-is-a-correct-mime-type-for-docx-pptx-etc
            return base.File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>
        /// View Order detail
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult Detail(int id)
        {
            var order = Db.Select<Order>(x => x.Where(m => m.Id == id)).FirstOrDefault();
            if (order == null)
            {
                return RedirectToAction("Index");
            }

            // check the permission
            var current_user = CurrentUser;
            bool hasSpecialRole = current_user.HasRole(RoleEnum.OrderManagement) || current_user.HasRole(RoleEnum.Administrator);

            // get the list of available roles
            var order_roles = OrderLib._GetUserAvailableRoleToCheckOrder(current_user.Roles);

            // check that if this user can work with this order
            if (!hasSpecialRole && !order_roles.Contains(order.StatusEnum))
            {
                return RedirectToAction("Index");
            }

            // force only the staff who open the ticket can enter the order detail
            if (!hasSpecialRole && order.WorkingStaff_Id != current_user.Id)
            {
                return RedirectToAction("Index");
            }

            var ticket = Db.Select<Order_UploadFilesTicket>(x => x.Where(y => (y.OrderId == order.Id)).Limit(1)).FirstOrDefault();

            ViewData["Ticket"] = ticket;

            return View(order);
        }

        /// <summary>
        /// Approve this step
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Order_Approve(long id)
        {
            var order = Db.Select<Order>(x => x.Where(m => m.Id == id).Limit(1)).FirstOrDefault();

            if (order == null)
            {
                return JsonError("Can not find your order");
            }
            else
            {
                var order_lib = new OrderLib(order);
                //
                var current_user = CurrentUser;
                bool hasSpecialRole = current_user.HasRole(RoleEnum.OrderManagement) || current_user.HasRole(RoleEnum.Administrator);

                // get the list of available roles
                var order_roles = OrderLib._GetUserAvailableRoleToCheckOrder(current_user.Roles);

                // check that if this user can work with this order
                if (!hasSpecialRole && !order_roles.Contains(order_lib.Order.StatusEnum))
                {
                    return JsonError("You dont have permission to approve this order");
                }

                if (!hasSpecialRole && order.WorkingStaff_Id != current_user.Id)
                {
                    return JsonError("You can not approve the steps for this order. Please contact Admin");
                }

                if (!order_lib.CheckBeforeApproveForStatusReceived())
                {
                    return JsonError("Please check this order payment before approve it.");
                }
                else if (!order_lib.CheckBeforeApproveForStatusShipping())
                {
                    return JsonError("Please make sure that you already saved the Tracking Number of the shipping of this order. And also the Shipping Status must be set to Shipped.");
                }

                // update the order production job sheet
                order_lib.ProductionJobSheet_Add(current_user.Id);

                // update this user activities
                var time_amount = (int)DateTime.Now.Subtract(order.WorkingStaff_On).TotalMinutes;
                StaffActivity act = Db.Select<StaffActivity>(x => x.Where(m => m.User_Id == current_user.Id && m.Day == DateTime.Now.Date).Limit(1)).FirstOrDefault();
                if (act == null)
                {
                    act = new StaffActivity() { User_Id = current_user.Id, Day = DateTime.Now.Date, Count = 0, Sum = 0 };
                    Db.Insert<StaffActivity>(act);
                    act.Id = Db.GetLastInsertId();
                }
                act.Count++;
                act.Sum += time_amount;
                Db.Update<StaffActivity>(act);

                // move to next step
                order_lib.Order.Status++;
                if (order_lib.Order.Status > (int)Enum_OrderStatus.Finished)
                {
                    order_lib.Order.StatusEnum = Enum_OrderStatus.Finished;
                }

                // clear staff working status and update new status
                Db.UpdateOnly<Order>(new Order()
                {
                    WorkingStaff_Id = 0,
                    Status = order_lib.Order.Status
                }, ev => ev.Update(p => new
                {
                    p.WorkingStaff_Id,
                    p.Status
                }).Where(m => (m.Id == order.Id)));

                // update last update status
                order.Update_LastUpdate();

                #region send email to customer to update status
                Site_MaillingListTemplate template = null;
                SMSTemplateModel sms_template = null;
                var o_country = order.GetCountry();

                // process some special steps
                switch (order_lib.Order.StatusEnum)
                {
                    case Enum_OrderStatus.Finished:
                        order_lib.CloseOrder();
                        // send email to customer to notify
                        template = Get_MaillingListTemplate("order___status_finished");
                        if (o_country != null)
                        {
                            sms_template = SMS_GetTemplate("sms_order_approve_step9", o_country.Code);
                        }
                        break;
                    case Enum_OrderStatus.Verify:
                        // send email to customer to tell them we are processing their order
                        template = Get_MaillingListTemplate("order___start_processing");
                        break;
                    default:
                        break;
                }

                // send email to customer based on the template
                if (template != null)
                {
                    // now we compile the model into view by replace the tokens
                    order.LoadAddress(0);
                    order.LoadAddress(1);

                    if (string.IsNullOrEmpty(order.Shipping_TrackingNumber))
                    {
                        order.Shipping_TrackingNumber = "";
                    }

                    Dictionary<string, string> dic = order.GetPropertiesListOrValue(true, "");
                    var billing = order.BillingAddressModel.GetPropertiesListOrValue(true, "Billing_");
                    var shipping = order.BillingAddressModel.GetPropertiesListOrValue(true, "Shipping_");
                    dic = dic.MergeDictionary(billing);
                    dic = dic.MergeDictionary(shipping);

                    //
                    var content = template.Body;
                    var title = template.Title;
                    SendMail_RenderBeforeSend(ref content, ref title, dic);
                    var u = CurrentUser;

                    // send email
                    PhotoBookmart.Common.Helpers.SendEmail.SendMail(order.Customer_Email, title, content, CurrentWebsite.Email_Support, "Photobookmart Customer Support");

                    // send sms
                    if (sms_template != null)
                    {
                        content = sms_template.Content;
                        SendMail_RenderBeforeSend(ref content, ref title, dic);

                        var o_u = order.Get_CustomerUserAccount();
                        var number = SMS_Normalize_PhoneNumber(o_u.Phone, o_country.PhoneNumberPrefix, o_country.Code);
                        SMSTransferAgentTask.Send(number, content, sms_template.IsFlash);
                    }

                    order.AddHistory(string.Format("<b>Update order status to customer.</b>\r\nTitle: {0}\r\nContent:{1}", title, content), u.FullName, u.Id, false);
                }
                #endregion

                return JsonSuccess(Url.Action("Index"), "Your working in this order has been saved");
            }
        }

        /// <summary>
        /// Open the production job sheet for staff to work on this order
        /// </summary>
        /// <param name="id"></param>
        /// <param name="staff_id"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Order_StaffOpenWork(long id)
        {
            var order = Db.Select<Order>(x => x.Where(m => m.Id == id).Limit(1)).FirstOrDefault();

            if (order == null)
            {
                return JsonError("Can not find your order");
            }
            else
            {
                //
                var current_user = CurrentUser;
                bool hasSpecialRole = current_user.HasRole(RoleEnum.OrderManagement) || current_user.HasRole(RoleEnum.Administrator);

                // get the list of available roles
                var order_roles = OrderLib._GetUserAvailableRoleToCheckOrder(current_user.Roles);

                // check that if this user can work with this order
                if (!hasSpecialRole && !order_roles.Contains(order.StatusEnum))
                {
                    order.AddHistory(string.Format("Staff {0} try to start working on this order. System denied", current_user.FullName), "System", 0, true);
                    return JsonError("You dont have permission to start working on this order");
                }

                if (order.WorkingStaff_Id > 0)
                {
                    order.AddHistory(string.Format("Staff {0} try to start working on this order. System denied", current_user.FullName), "System", 0, true);
                    return JsonError("This order has been assigned to another staff to work on it");
                }

                // update
                order.AssignStaffWorking(current_user.Id, current_user.FullName);

                return JsonSuccess(Url.Action("Detail", new { id = order.Id }), string.Format("Order {0} has been assigned to you", order.Order_Number));
            }
        }

        /// <summary>
        /// Reset the working status of the user whom has been assigned into this order
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Order_StaffResetWork(long id)
        {
            var order = Db.Select<Order>(x => x.Where(m => m.Id == id).Limit(1)).FirstOrDefault();

            if (order == null)
            {
                return JsonError("Can not find your order");
            }
            else
            {
                //
                var current_user = CurrentUser;
                bool hasSpecialRole = current_user.HasRole(RoleEnum.OrderManagement) || current_user.HasRole(RoleEnum.Administrator);

                if (!hasSpecialRole)
                {
                    return JsonError("You dont have permission to do this action");
                }

                if (order.WorkingStaff_Id > 0)
                {
                    var staff = Db.Select<ABUserAuth>(x => x.Where(m => m.Id == order.WorkingStaff_Id).Limit(1)).FirstOrDefault();
                    var staff_name = order.WorkingStaff_Id.ToString();
                    if (staff != null)
                    {
                        staff_name = staff.FullName;
                    }
                    order.AddHistory(string.Format("User {0} removed staff {1} out of order {2}", current_user.FullName, staff_name, order.Order_Number), current_user.FullName, current_user.Id, true);

                    Db.UpdateOnly<Order>(new Order() { WorkingStaff_Id = 0 }, ev => ev.Update(p => p.WorkingStaff_Id).Where(m => m.Id == order.Id).Limit(1));

                    return JsonSuccess(Url.Action("Index"), string.Format("Order {0} has been reseted", order.Order_Number));
                }
                else
                {
                    return JsonError("This order has not been assigned to any staff");
                }
            }
        }

        /// <summary>
        /// Quick navigate
        /// If user can enter, will redirect, if not, then will return error
        /// </summary>
        /// <param name="OrderId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Order_QuickNavigate(int id)
        {
            var order = Db.Select<Order>(x => x.Where(m => m.Id == id)).FirstOrDefault();
            if (order == null)
            {
                return JsonError("Order is not found");
            }

            // check the permission
            var current_user = CurrentUser;
            bool hasSpecialRole = current_user.HasRole(RoleEnum.OrderManagement) || current_user.HasRole(RoleEnum.Administrator);

            // get the list of available roles
            var order_roles = OrderLib._GetUserAvailableRoleToCheckOrder(current_user.Roles);

            // check that if this user can work with this order
            if (!hasSpecialRole && !order_roles.Contains(order.StatusEnum))
            {
                return JsonError("Found order " + order.Order_Number + ", but you dont have permission to view it in detail");
            }

            // force only the staff who open the ticket can enter the order detail
            if (!hasSpecialRole && order.WorkingStaff_Id != current_user.Id)
            {
                return JsonError("Found order " + order.Order_Number + ", but another staff is working on it.");
            }

            return JsonSuccess(Url.Action("Detail", new { id = order.Id }), "Order is found");
        }

        /// <summary>
        /// Check and valid the order id for automated shipping
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Automated_Shipping_Exists(int id)
        {
            var order = Db.Select<Order>(x => x.Where(m => m.Id == id)).FirstOrDefault();
            if (order == null)
            {
                return JsonError("Order is not found");
            }

            // check the permission
            var current_user = CurrentUser;
            bool hasSpecialRole = current_user.HasRole(RoleEnum.OrderManagement) || current_user.HasRole(RoleEnum.Administrator);

            // get the list of available roles
            var order_roles = OrderLib._GetUserAvailableRoleToCheckOrder(current_user.Roles);

            // check that if this user can work with this order
            if (!hasSpecialRole && !order_roles.Contains(order.StatusEnum))
            {
                return JsonError("Found order " + order.Order_Number + ", but you dont have permission to view it in detail");
            }

            // force only the staff who open the ticket can enter the order detail
            if (!hasSpecialRole && order.WorkingStaff_Id != current_user.Id)
            {
                return JsonError("Found order " + order.Order_Number + ", but another staff is working on it.");
            }

            var ret = new OrderAutomatedShippingModel() { Order_Number = order.Order_Number, Shipping_Method = order.Shipping_Method, Shipping_Status = order.Shipping_Status, Shipping_TrackingNumber = order.Shipping_TrackingNumber };

            return JsonSuccess("", ret.ToJson());
        }

        [HttpPost]
        public ActionResult AutomatedShipping(OrderAutomatedShippingModel model)
        {
            bool can_send_email_update_status = false;
            try
            {
                if (!IsValidOrderNum(model.Order_Number))
                {
                    return JsonError("Enter order number in 6 digits format");
                }

                if (!(CurrentUser.HasRole(RoleEnum.Administrator) ||
                    CurrentUser.HasRole(RoleEnum.OrderManagement) ||
                    CurrentUser.HasRole(RoleEnum.Shipping)))
                {
                    return JsonError("You don't have permission to execute");
                }

                if (model.Shipping_Status == Enum_ShippingStatus.Shipped &&
                    string.IsNullOrEmpty(model.Shipping_TrackingNumber))
                {
                    return JsonError("Please enter for » Tracking Number");
                }

                var order = Db.Select<Order>(x => x.Where(y => (y.Order_Number == model.Order_Number)).Limit(1)).FirstOrDefault();

                if (order == null)
                {
                    return JsonError("Can not find your order number");
                }

                #region Update Order
                var current_user = CurrentUser;
                var o_country = order.GetCountry();
                SMSTemplateModel sms_template = null;

                // Shipping Method
                if (order.Shipping_Method != model.Shipping_Method)
                {
                    can_send_email_update_status = true;

                    order.AddHistory(string.Format("Update Shipping Method from <b>{0}</b> to <b>{1}</b>", order.Shipping_Method.DisplayName(), model.Shipping_Method.DisplayName()), current_user.FullName, current_user.Id);
                    // update it
                    order.Shipping_Method = model.Shipping_Method;

                    Db.UpdateOnly<Order>(new Order() { Shipping_Method = order.Shipping_Method }, ev => ev.Update(p => p.Shipping_Method).Where(m => m.Id == order.Id).Limit(1));
                }

                // Shipping Status
                if (order.Shipping_Status != model.Shipping_Status)
                {
                    can_send_email_update_status = true;

                    order.AddHistory(string.Format("Update Shipping Status from <b>{0}</b> to <b>{1}</b>", order.Shipping_Status.DisplayName(), model.Shipping_Status.DisplayName()), current_user.FullName, current_user.Id);
                    // update it
                    order.Shipping_Status = model.Shipping_Status;
                    if (order.Shipping_Status == Enum_ShippingStatus.Shipped)
                    {
                        order.Shipping_ShipOn = DateTime.Now;
                    }

                    Db.UpdateOnly<Order>(new Order() { Shipping_Status = order.Shipping_Status, Shipping_ShipOn = order.Shipping_ShipOn }, ev => ev.Update(p => new { p.Shipping_Status, p.Shipping_ShipOn }).Where(m => m.Id == order.Id).Limit(1));

                    // status only
                    if (o_country != null)
                    {
                        sms_template = SMS_GetTemplate("order_shipping_status_update", o_country.Code);
                    }
                }

                if (order.Shipping_Status == Enum_ShippingStatus.Shipped && order.Shipping_TrackingNumber != model.Shipping_TrackingNumber)
                {
                    can_send_email_update_status = true;

                    order.AddHistory(string.Format("Update Shipping Tracking Number <b>{0}</b>", model.Shipping_TrackingNumber), current_user.FullName, current_user.Id);
                    // update it
                    order.Shipping_TrackingNumber = model.Shipping_TrackingNumber;

                    Db.UpdateOnly<Order>(new Order() { Shipping_TrackingNumber = order.Shipping_TrackingNumber }, ev => ev.Update(p => p.Shipping_TrackingNumber).Where(m => m.Id == order.Id).Limit(1));

                    // with tracking number
                    if (o_country != null)
                    {
                        sms_template = SMS_GetTemplate("order_shipping_status_and_tracking_number_update", o_country.Code);
                    }
                }

                // Update LastUpdate field for Order
                if (can_send_email_update_status)
                {
                    Db.UpdateOnly<Order>(new Order() { LastUpdate = DateTime.Now },
                        ev => ev.Update(p => new
                        {
                            p.LastUpdate
                        }).Where(m => m.Id == order.Id).Limit(1));
                }

                #endregion

                #region Send email update to customer
                if (can_send_email_update_status)
                {
                    // get the template
                    var template = Get_MaillingListTemplate("order___shipping_status_update");

                    if (template != null)
                    {
                        // now we compile the model into view by replace the tokens
                        order.LoadAddress(0);
                        order.LoadAddress(1);

                        if (string.IsNullOrEmpty(order.Shipping_TrackingNumber))
                        {
                            order.Shipping_TrackingNumber = "";
                        }

                        Dictionary<string, string> dic = order.GetPropertiesListOrValue(true, "");
                        var billing = order.BillingAddressModel.GetPropertiesListOrValue(true, "Billing_");
                        var shipping = order.BillingAddressModel.GetPropertiesListOrValue(true, "Shipping_");
                        dic = dic.MergeDictionary(billing);
                        dic = dic.MergeDictionary(shipping);

                        //
                        var content = template.Body;
                        var title = template.Title;
                        SendMail_RenderBeforeSend(ref content, ref title, dic);
                        var u = CurrentUser;

                        PhotoBookmart.Common.Helpers.SendEmail.SendMail(order.Customer_Email, title, content, CurrentWebsite.Email_Support, "Photobookmart Customer Support");

                        // send sms
                        if (sms_template != null)
                        {
                            content = sms_template.Content;
                            SendMail_RenderBeforeSend(ref content, ref title, dic);

                            var o_u = order.Get_CustomerUserAccount();
                            var number = SMS_Normalize_PhoneNumber(o_u.Phone, o_country.PhoneNumberPrefix, o_country.Code);
                            SMSTransferAgentTask.Send(number, content, sms_template.IsFlash);
                        }

                        order.AddHistory(string.Format("<b>Update shipping status to customer.</b>\r\nTitle: {0}\r\nContent:{1}", title, content), u.FullName, u.Id, false);
                    }

                    // update the last update status
                    order.Update_LastUpdate();
                }

                #endregion

                return JsonSuccess(null);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult ReqUploadFileAgain(long Id)
        {
            try
            {
                if (!(CurrentUser.HasRole(RoleEnum.Administrator) ||
                    CurrentUser.HasRole(RoleEnum.OrderManagement) ||
                    CurrentUser.HasRole(RoleEnum.Received) ||
                    CurrentUser.HasRole(RoleEnum.Verify)))
                {
                    return JsonError("You don't have permission to execute.");
                }

                var order = Db.Select<Order>(x => x.Where(y => (y.Id == Id)).Limit(1)).FirstOrDefault();

                if (order == null)
                {
                    return JsonError("Can't find your order.");
                }

                var ticket = new Order_UploadFilesTicket()
                {
                    OrderId = Id,
                    UserId = (int)order.Customer_Id,
                    Status = (int)Enum_UploadFilesTicketStatus.Default,
                    CreatedOn = DateTime.Now
                };

                Db.Insert<Order_UploadFilesTicket>(ticket);

                var template = Get_MaillingListTemplate("order_request_upload_files_corrupt");

                if (template != null)
                {
                    // now we compile the model into view by replace the tokens
                    order.LoadAddress(0);
                    order.LoadAddress(1);

                    if (string.IsNullOrEmpty(order.Shipping_TrackingNumber))
                    {
                        order.Shipping_TrackingNumber = "";
                    }

                    Dictionary<string, string> dic = order.GetPropertiesListOrValue(true, "");
                    var billing = order.BillingAddressModel.GetPropertiesListOrValue(true, "Billing_");
                    var shipping = order.BillingAddressModel.GetPropertiesListOrValue(true, "Shipping_");
                    dic = dic.MergeDictionary(billing);
                    dic = dic.MergeDictionary(shipping);

                    //
                    var content = template.Body;
                    var title = template.Title;
                    SendMail_RenderBeforeSend(ref content, ref title, dic);
                    var u = CurrentUser;

                    PhotoBookmart.Common.Helpers.SendEmail.SendMail(order.Customer_Email, title, content, CurrentWebsite.Email_Support, "Photobookmart Customer Support");

                    order.AddHistory(string.Format("Send email to customer ({0}):\r\n<b>{1}</b>\r\n{2} ", order.Customer_Email, title, content), CurrentUser.FullName, CurrentUser.Id, true);
                }

                order.AddHistory("Request customer to upload file again because of order's file corrupted.", CurrentUser.FullName, CurrentUser.Id, false);
                order.Update_LastUpdate();
                return JsonSuccess(null);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult ApproveUploadFileRequest(long Id)
        {
            try
            {
                if (!(CurrentUser.HasRole(RoleEnum.Administrator) ||
                    CurrentUser.HasRole(RoleEnum.OrderManagement) ||
                    CurrentUser.HasRole(RoleEnum.Received) ||
                    CurrentUser.HasRole(RoleEnum.Verify)))
                {
                    return JsonError("You don't have permission to execute.");
                }

                var order = Db.Select<Order>(x => x.Where(y => (y.Id == Id)).Limit(1)).FirstOrDefault();

                if (order == null)
                {
                    return JsonError("Can't find your order.");
                }

                var ticket = Db.Select<Order_UploadFilesTicket>(x => x.Where(y => (y.OrderId == Id)).Limit(1)).FirstOrDefault();

                if (ticket == null)
                {
                    return JsonError("Can't find your upload file request.");
                }

                if (ticket.Status != (int)Enum_UploadFilesTicketStatus.DecryptedSuccess)
                {
                    return JsonError("The ticket is not ready for approve");
                }

                // we will not do the file moving any more, because we need to extract the zip file. 
                // we will increase the value to ready to move
                Db.UpdateOnly<Order_UploadFilesTicket>(new Order_UploadFilesTicket() { LastUpdate = DateTime.Now, Status = (int)Enum_UploadFilesTicketStatus.MoveToDataFolder }, ev => ev.Update(p => new
                {
                    p.Status,
                    p.LastUpdate
                }).Where(m => (m.Id == ticket.Id)).Limit(1));

                #region Send Email notify
                var template = Get_MaillingListTemplate("order_approve_upload_files_corrupt");

                if (template != null)
                {
                    // now we compile the model into view by replace the tokens
                    order.LoadAddress(0);
                    order.LoadAddress(1);

                    if (string.IsNullOrEmpty(order.Shipping_TrackingNumber))
                    {
                        order.Shipping_TrackingNumber = "";
                    }

                    Dictionary<string, string> dic = order.GetPropertiesListOrValue(true, "");
                    var billing = order.BillingAddressModel.GetPropertiesListOrValue(true, "Billing_");
                    var shipping = order.BillingAddressModel.GetPropertiesListOrValue(true, "Shipping_");
                    dic = dic.MergeDictionary(billing);
                    dic = dic.MergeDictionary(shipping);

                    //
                    var content = template.Body;
                    var title = template.Title;
                    SendMail_RenderBeforeSend(ref content, ref title, dic);
                    var u = CurrentUser;

                    PhotoBookmart.Common.Helpers.SendEmail.SendMail(order.Customer_Email, title, content, CurrentWebsite.Email_Support, "Photobookmart Customer Support");

                    order.AddHistory(string.Format("Send email to customer ({0}):\r\n<b>{1}</b>\r\n{2} ", order.Customer_Email, title, content), CurrentUser.FullName, CurrentUser.Id, true);
                }
                #endregion
                order.AddHistory("Approved file " + ticket.FileName + ", system will move the data file shortly", CurrentUser.FullName, CurrentUser.Id, false);

                return JsonSuccess(null);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult CancelUploadFileRequest(long Id, string Reason)
        {
            try
            {
                if (!(CurrentUser.HasRole(RoleEnum.Administrator) ||
                    CurrentUser.HasRole(RoleEnum.OrderManagement) ||
                    CurrentUser.HasRole(RoleEnum.Received) ||
                    CurrentUser.HasRole(RoleEnum.Verify)))
                {
                    return JsonError("You don't have permission to execute.");
                }

                var order = Db.Select<Order>(x => x.Where(y => (y.Id == Id)).Limit(1)).FirstOrDefault();

                if (order == null)
                {
                    return JsonError("Can't find your order.");
                }

                var ticket = Db.Select<Order_UploadFilesTicket>(x => x.Where(y => (y.OrderId == Id)).Limit(1)).FirstOrDefault();

                if (ticket == null)
                {
                    return JsonError("Can't find your upload file request.");
                }

                Db.Delete<Order_UploadFilesTicket>(x => x.Where(y => (y.Id == ticket.Id)).Limit(1));

                order.AddHistory(string.Format("Cancel file upload request (request status = {0}), reason: {1}.", ((Enum_UploadFilesTicketStatus)ticket.Status).DisplayName(), Reason), CurrentUser.FullName, CurrentUser.Id, false);

                order.Update_LastUpdate();
                return JsonSuccess(null);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        #endregion

        #region Functions

        private bool IsValidOrderNum(string orderNum)
        {
            Regex regex = new Regex(@"/[^0-9]/g", RegexOptions.Compiled);

            if (string.IsNullOrEmpty(orderNum) ||
                orderNum.Length > 6 ||
                regex.IsMatch(orderNum))
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Common Order Info
        /// <summary>
        /// Update order info
        /// - Assign order to difference customer
        /// - Update customer username
        /// - Update customer full name
        /// - Update customer email
        /// - Assign new product (no update price)
        /// - Update order status
        /// 
        /// Require role: Admin or order management
        /// </summary>
        /// <returns></returns>
        [ABRequiresAnyRole(RoleEnum.Administrator, RoleEnum.OrderManagement)]
        public ActionResult Order_UpdateInfo(Order model, bool DontDeletePhotobookFiles = false)
        {
            var order = Db.Select<Order>(x => x.Where(m => m.Id == model.Id).Limit(1)).FirstOrDefault();

            if (order == null)
            {
                return JsonError("Can not find your order");
            }
            else
            {
                var current_user = CurrentUser;

                #region Assign difference user
                // assign order to difference user
                if (model.Customer_Id > 0 && model.Customer_Id != order.Customer_Id)
                {
                    var u = Db.Select<ABUserAuth>(x => x.Where(m => m.Id == model.Customer_Id).Limit(1)).FirstOrDefault();
                    if (u != null)
                    {
                        order.AddHistory(string.Format("Order {0} has been assigned to customer {1}({2}) - {3}", order.Order_Number, u.FullName, u.UserName, u.Email), current_user.FullName, current_user.Id, true);

                        Db.UpdateOnly<Order>(new Order() { Customer_Id = model.Customer_Id }, ev => ev.Update(p => p.Customer_Id).Where(m => m.Id == order.Id).Limit(1));

                        Db.UpdateOnly<Order>(new Order() { Customer_Email = u.Email }, ev => ev.Update(p => p.Customer_Email).Where(m => m.Id == order.Id).Limit(1));

                        Db.UpdateOnly<Order>(new Order() { Customer_Username = u.UserName }, ev => ev.Update(p => p.Customer_Username).Where(m => m.Id == order.Id).Limit(1));

                        Db.UpdateOnly<Order>(new Order() { Customer_Name = u.FullName }, ev => ev.Update(p => p.Customer_Name).Where(m => m.Id == order.Id).Limit(1));

                        order.Customer_Id = model.Customer_Id;
                        order.Customer_Email = u.Email;
                        order.Customer_Name = u.FullName;
                        order.Customer_Username = u.UserName;
                    }
                }
                else
                {
                    // only update customer username, customer fullname, customer email if not assign user
                    // customer username
                    if (!string.IsNullOrEmpty(model.Customer_Username) && model.Customer_Username != order.Customer_Username)
                    {
                        order.AddHistory(string.Format("Update Customer_Username from <b>{0}</b> to <b>{1}</b>", order.Customer_Username, model.Customer_Username), current_user.FullName, current_user.Id, true);
                        order.Customer_Username = model.Customer_Username;
                        Db.UpdateOnly<Order>(new Order() { Customer_Username = model.Customer_Username }, ev => ev.Update(p => p.Customer_Username).Where(m => m.Id == order.Id).Limit(1));
                    }

                    // customer name
                    if (!string.IsNullOrEmpty(model.Customer_Name) && model.Customer_Name != order.Customer_Name)
                    {
                        order.AddHistory(string.Format("Update Customer_Name from <b>{0}</b> to <b>{1}</b>", order.Customer_Name, model.Customer_Name), current_user.FullName, current_user.Id, true);
                        order.Customer_Name = model.Customer_Name;
                        Db.UpdateOnly<Order>(new Order() { Customer_Name = model.Customer_Name }, ev => ev.Update(p => p.Customer_Name).Where(m => m.Id == order.Id).Limit(1));
                    }

                    // customer name
                    if (!string.IsNullOrEmpty(model.Customer_Email) && model.Customer_Email != order.Customer_Email)
                    {
                        order.AddHistory(string.Format("Update Customer_Email from <b>{0}</b> to <b>{1}</b>", order.Customer_Email, model.Customer_Email), current_user.FullName, current_user.Id, true);

                        order.Customer_Email = model.Customer_Email;
                        Db.UpdateOnly<Order>(new Order() { Customer_Email = model.Customer_Email }, ev => ev.Update(p => p.Customer_Email).Where(m => m.Id == order.Id).Limit(1));
                    }
                }
                #endregion

                // assign new product
                if (model.Product_Id > 0 && model.Product_Id != order.Product_Id)
                {
                    var product = Db.Select<Product>(x => x.Where(k => k.Id == model.Product_Id && k.Status).Limit(1)).FirstOrDefault();
                    var country = Db.Select<Country>(x => x.Where(m => m.CurrencyCode == order.Shipping_DisplayPriceSign).Limit(1)).FirstOrDefault();
                    if (product != null && country != null)
                    {
                        var product_price = product.getPrice(Enum_Price_MasterType.Product, country.Code);
                        if (product != null)
                        {
                            order.AddHistory(string.Format("Change order product from <b>{0}</b> to <b>{1}</b>", order.Product_Name, product.Name), current_user.FullName, current_user.Id, true);
                            order.AddHistory(string.Format("Change product price from <b>{0}</b> to <b>{1}</b>", order.Product_Price, product_price.Value), current_user.FullName, current_user.Id, true);

                            order.Product_Id = product.Id;
                            order.Product_Name = product.Name;
                            order.Product_Price = product_price.Value;

                            Db.UpdateOnly<Order>(new Order()
                            {
                                Product_Name = order.Product_Name,
                                Product_Price = order.Product_Price,
                                Product_Id = order.Product_Id
                            }, ev => ev.Update(p => new { p.Product_Name, p.Product_Price, p.Product_Id }).Where(m => m.Id == order.Id).Limit(1));
                        }
                    }
                }

                // order status
                if (model.Status != order.Status)
                {
                    // allow Admin or Manager update the order status 
                    // OR staff who has role Shipping to update status from Shipping to Finished
                    if (current_user.HasRole(RoleEnum.Administrator) || current_user.HasRole(RoleEnum.OrderManagement))
                    {
                        if (model.StatusEnum == Enum_OrderStatus.Refund && order.PaymentStatusEnum != Enum_PaymentStatus.Paid)
                        {
                            return JsonError("Customer has not yet paid for this order. Can not change order status to Refund.");
                        }
                        else
                        {
                            order.AddHistory(string.Format("Order status change from <b>{0}</b> to <b>{1}</b>", order.StatusEnum.DisplayName(), model.StatusEnum.DisplayName()), current_user.FullName, current_user.Id, true);

                            order.Status = model.Status;
                            Db.UpdateOnly<Order>(new Order() { Status = order.Status, LastUpdate = DateTime.Now }, ev => ev.Update(p => new { p.Status, p.LastUpdate }).Where(m => m.Id == order.Id).Limit(1));
                            if (model.StatusEnum == Enum_OrderStatus.Refund)
                            {
                                order.AddHistory("Warning: Currently Photobookmart system does not support to refund money from iPay88/Paypal. Please do it manually.", "System", 0, true);
                            }

                            // return the coupons
                            // only admin / manager can cancel or refund coupon
                            if ((model.StatusEnum == Enum_OrderStatus.Canceled || model.StatusEnum == Enum_OrderStatus.Refund) && order.isUseCoupon)
                            {
                                var coupon = Db.Select<CouponPromo>(x => x.Where(y => y.Code == order.Coupon_Code).Limit(1)).FirstOrDefault();
                                if (coupon != null)
                                {
                                    Db.UpdateOnly<CouponPromo>(new CouponPromo() { Used = coupon.Used - 1 }, ev => ev.Update(p => new { p.Used }).Where(m => m.Id == coupon.Id).Limit(1));
                                    order.AddHistory(string.Format("Update coupon {0}, set used count from {1} to {2} ", coupon.Code, coupon.Used, coupon.Used - 1), "System", 0, true);
                                }
                            }
                        }
                    }
                    else
                    {
                        order.AddHistory(string.Format("Staff {1} trying to update order status but not success. System denied", current_user.FullName), "System", 0, true);
                    }
                }


                // Delete photobook
                #region Delete Photobook
                if ((current_user.HasRole(RoleEnum.Administrator) || current_user.HasRole(RoleEnum.OrderManagement)) &&
                    !DontDeletePhotobookFiles &&
                    (order.StatusEnum == Enum_OrderStatus.Canceled || order.StatusEnum == Enum_OrderStatus.Refund || order.StatusEnum == Enum_OrderStatus.Finished) && !order.Order_Photobook_Deleted
                    )
                {
                    var path = "";
                    if (order.PaymentStatusEnum == Enum_PaymentStatus.Paid)
                    {
                        path = Settings.Get(Enum_Settings_Key.WEBSITE_UPLOAD_PATH_DEFAULT, System.IO.Path.GetTempPath(), Enum_Settings_DataType.String).ToString();
                    }
                    else
                    {
                        path = Settings.Get(Enum_Settings_Key.WEBSITE_ORDERS_FOLDER_NOTYETPAID_PATH, System.IO.Path.GetTempPath(), Enum_Settings_DataType.String).ToString();
                    }

                    // check this folder existing
                    path = System.IO.Path.Combine(path, order.Order_Number);
                    if (System.IO.Directory.Exists(path))
                    {
                        try
                        {
                            System.IO.Directory.Delete(path, true);
                            order.AddHistory("Deleted photobook folder in " + path, "System", 0, true);

                            Db.UpdateOnly<Order>(new Order() { Order_Photobook_Deleted = true }, ev => ev.Update(p => new { p.Order_Photobook_Deleted }).Where(m => m.Id == order.Id).Limit(1));
                        }
                        catch (Exception ex)
                        {
                            order.AddHistory("Error while deleting photobook folder: " + ex.Message, "System", 0, true);
                        }
                    }
                    else
                    {
                        order.AddHistory("Warning: System can not find photobook folder in " + path, "System", 0, true);
                    }
                }
                #endregion

                return JsonSuccess("", "Order has been updated successfully");
            }
        }
        #endregion

        #region Search order
        public ActionResult OrderSearchs(OrderFilterModel model)
        {
            List<Order> c = new List<Order>();

            var p = PredicateBuilder.True<Order>();

            // for date
            if (!(model.BetweenDate == DateTime.MinValue || model.AndDate == DateTime.MinValue))
                p = p.And(m => m.CreatedOn >= model.BetweenDate && m.CreatedOn <= model.AndDate);
            else
                p = p.And(m => m.CreatedOn < DateTime.Now);

            if (!string.IsNullOrEmpty(model.Search))
            {
                p = p.And(m => m.Product_Name.Contains(model.Search) || m.Customer_Username.Contains(model.Search) || m.Coupon_Code.Contains(model.Search));
            }

            //if (!string.IsNullOrEmpty(model.Host))
            //{
            //    p = p.And(m => m.ServerHost.Contains(model.Host));
            //}

            c = Db.Where<Order>(p);

            if (model.ResultType == 1)
            {
                //return Export_To_Excel(c);
                return Content("Not yet implemented");
            }
            else
            {
                return PartialView("_List", c);
            }
        }

        #endregion

        #region Payment
        /// <summary>
        /// Update payment status
        /// Role: Admin, Order Management, Received, Verify
        /// </summary>
        /// <param name="OrderId"></param>
        /// <param name="PaymentStatus"></param>
        /// <returns></returns>
        //[ABRequiresAnyRole(RoleEnum.Administrator, RoleEnum.OrderManagement, RoleEnum.Received, RoleEnum.Verify)]
        public ActionResult Payment_UpdateStatus(Order model)
        {
            // check the order
            var order = Db.Select<Order>(m => m.Where(x => x.Id == model.Id).Limit(1)).FirstOrDefault();
            if (order == null)
            {
                return JsonError("Can not find your order");
            }

            var can_update_lastupdate = false;
            var current_user = CurrentUser;

            // payment status
            if (order.PaymentStatus != model.PaymentStatus)
            {
                order.AddHistory(string.Format("Update payment status from <b>{0}</b> to <b>{1}</b>", order.PaymentStatusEnum, model.PaymentStatusEnum), current_user.FullName, current_user.Id);
                // update it
                order.Set_PaymentStatus(model.PaymentStatusEnum);

                can_update_lastupdate = true;
            }

            // payment method
            if (order.PaymentMethod != model.PaymentMethod)
            {
                order.AddHistory(string.Format("Update payment method from <b>{0}</b> to <b>{1}</b>", order.PaymentMethod, model.PaymentMethod), current_user.FullName, current_user.Id);
                // update it
                order.PaymentMethod = model.PaymentMethod;

                Db.UpdateOnly<Order>(new Order() { PaymentMethod = order.PaymentMethod }, ev => ev.Update(p => p.PaymentMethod).Where(m => m.Id == order.Id).Limit(1));

                if (order.PaymentMethod == Enum_PaymentMethod.Cheque || order.PaymentMethod == Enum_PaymentMethod.Bank_Check || order.PaymentMethod == Enum_PaymentMethod.Other)
                {
                    // payment method - cheque/check 
                    if (string.IsNullOrEmpty(model.Payment_ChequeCheckNumber))
                    {
                        model.Payment_ChequeCheckNumber = "";
                    }
                    if (model.Payment_ChequeCheckNumber != order.Payment_ChequeCheckNumber)
                    {
                        order.AddHistory(string.Format("Update Check/Cheque number from <b>{0}</b> to <b>{1}</b>", order.Payment_ChequeCheckNumber, model.Payment_ChequeCheckNumber), current_user.FullName, current_user.Id);
                        // update it
                        order.Payment_ChequeCheckNumber = model.Payment_ChequeCheckNumber;

                        Db.UpdateOnly<Order>(new Order() { Payment_ChequeCheckNumber = order.Payment_ChequeCheckNumber }, ev => ev.Update(p => p.Payment_ChequeCheckNumber).Where(m => m.Id == order.Id).Limit(1));
                    }
                }

                can_update_lastupdate = true;
            }

            // Product price
            if (order.Product_Price != model.Product_Price && model.Product_Price > 0)
            {
                order.AddHistory(string.Format("Update product price from <b>{2} {0:0.##}</b> to <b>{2} {1:0.##}</b>", order.Product_Price, model.Product_Price, DefaultCurrency), current_user.FullName, current_user.Id);
                // update it
                order.Product_Price = model.Product_Price;

                Db.UpdateOnly<Order>(new Order() { Product_Price = order.Product_Price }, ev => ev.Update(p => p.Product_Price).Where(m => m.Id == order.Id).Limit(1));

                //
                can_update_lastupdate = true;
            }

            // Grand Total
            if (order.Bill_GrandTotal != model.Bill_GrandTotal && model.Bill_GrandTotal > 0)
            {
                double gst = 6 * model.Bill_GrandTotal / 100;
                order.AddHistory(string.Format("Update product grand total from <b>{3} {0:0.##}</b> to <b>{3} {1:0.##}</b>, update the GST to {3} {2:0.##}", order.Bill_GrandTotal, model.Bill_GrandTotal, gst, DefaultCurrency), current_user.FullName, current_user.Id);
                // update it
                order.Bill_GrandTotal = model.Bill_GrandTotal;
                order.Bill_GST = gst;

                Db.UpdateOnly<Order>(new Order() { Bill_GrandTotal = order.Bill_GrandTotal }, ev => ev.Update(p => p.Bill_GrandTotal).Where(m => m.Id == order.Id).Limit(1));

                // 
                can_update_lastupdate = true;
            }

            // Bill Total
            if (order.Bill_Total != model.Bill_Total && model.Bill_Total > 0)
            {
                order.AddHistory(string.Format("Update order bill total from <b>{2} {0:0.##}</b> to <b>{2} {1:0.##}</b>", order.Bill_Total, model.Bill_Total, DefaultCurrency), current_user.FullName, current_user.Id);
                // update it
                order.Bill_Total = model.Bill_Total;

                Db.UpdateOnly<Order>(new Order() { Bill_Total = order.Bill_Total }, ev => ev.Update(p => p.Bill_Total).Where(m => m.Id == order.Id).Limit(1));

                // 
                can_update_lastupdate = true;

            }

            // coupon
            if (order.isUseCoupon && !model.isUseCoupon) // coupon has been turn off
            {
                order.AddHistory(string.Format("Set order to not use the coupon"), current_user.FullName, current_user.Id);
                // update it
                order.isUseCoupon = false;

                Db.UpdateOnly<Order>(new Order() { isUseCoupon = order.isUseCoupon }, ev => ev.Update(p => p.isUseCoupon).Where(m => m.Id == order.Id).Limit(1));
                order.AddHistory(string.Format("Remove COUPON CODE {0} , SECREC CODE {1} from order", order.Coupon_Code, order.Coupon_SecrectCode), current_user.FullName, current_user.Id);
                Db.UpdateOnly<Order>(new Order() { Coupon_Code = "", Coupon_SecrectCode = "" }, ev => ev.Update(p => new
                {
                    p.Coupon_Code,
                    p.Coupon_SecrectCode
                }).Where(m => m.Id == order.Id).Limit(1));

                can_update_lastupdate = true;
            }
            else if (order.isUseCoupon && model.isUseCoupon && order.Coupon_TotalDiscount != model.Coupon_TotalDiscount && model.Coupon_TotalDiscount > 0)
            {
                order.AddHistory(string.Format("Update order discount amount from <b>{2} {0:0.##}</b> to <b>{2} {1:0.##}</b>", order.Coupon_TotalDiscount, model.Coupon_TotalDiscount, DefaultCurrency), current_user.FullName, current_user.Id);
                // update it
                order.Coupon_TotalDiscount = model.Coupon_TotalDiscount;

                Db.UpdateOnly<Order>(new Order() { Coupon_TotalDiscount = order.Coupon_TotalDiscount }, ev => ev.Update(p => p.Coupon_TotalDiscount).Where(m => m.Id == order.Id).Limit(1));

                //
                can_update_lastupdate = true;
            }

            // 
            if (can_update_lastupdate)
            {
                // update last update
                order.Update_LastUpdate();
            }
            return JsonSuccess("", "Your order has been updated successfully");
        }
        #endregion

        #region History
        /// <summary>
        /// Load next page for order history
        /// </summary>
        /// <param name="id"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult History_LoadNext(long id, int page)
        {
            var order = Db.Select<Order>(m => m.Where(x => x.Id == id).Limit(1)).FirstOrDefault();
            if (order == null)
            {
                return RedirectToAction("Index");
            }

            int page_size = 9;

            // update the new message flag when page 1 only, because we list the new message first
            if (page == 1 && order.FlagHistoryMessage == (int)Enum_FlagOrderMessage.NewMessageFromCustomer)
            {
                var u = CurrentUser;
                order.AddHistory(string.Format("Staff {0} read new message from customer", u.FullName), "System", 0, true);
                // update order to let them know we have new message from customer
                Db.UpdateOnly<Order>(new Order() { FlagHistoryMessage = (int)Enum_FlagOrderMessage.No_NewMessage },
                   ev => ev.Update(p => new
                   {
                       p.FlagHistoryMessage
                   }).Where(m => m.Id == order.Id));
            }

            // count fist
            var count = (int)Db.Count<Order_History>(x => x.Order_Id == id);
            var pages = count / page_size;
            if (count % page_size > 0)
            {
                pages++;
            }

            var ret = Db.Select<Order_History>(x => x.Where(m => m.Order_Id == id).OrderByDescending(k => k.OnDate).Limit((page - 1) * page_size, page_size));
            var users = Db.Select<ABUserAuth>();

            // get user who submit order
            var order_user = users.Where(x => x.Id == order.Customer_Id).FirstOrDefault();
            if (order_user == null)
            {
                order_user = new ABUserAuth();
            }
            foreach (var item in ret)
            {
                // check usertype and avatar
                item.UserAvatar = "";
                if (item.UserId == order_user.Id)
                {
                    item.Direction = "left";
                    item.UserAvatar = order_user.Avatar;
                    item.UserName = string.Format("{0} {1}", order_user.FirstName, order_user.LastName);
                }
                else
                {
                    // system or staff
                    item.Direction = "right";
                    item.UserAvatar = "Content/default_system_orderhistory_logo.png";
                    // 
                    var u = users.Where(x => x.Id == item.UserId).FirstOrDefault();
                    if (u != null)
                    {
                        item.UserName = string.Format("{0} {1}", u.FirstName, u.LastName);
                        item.UserAvatar = u.Avatar;
                        // incase this user does not have avatar
                        if (string.IsNullOrEmpty(item.UserAvatar))
                        {
                            item.UserAvatar = "content/default_chat_avatar.png";
                        }
                    }
                }

                // date format
                var dif = (int)DateTime.Now.Subtract(item.OnDate).TotalMinutes;
                if (dif < 2)
                {
                    item.OnDateFormat = "Now";
                }
                else if (dif < 60)
                {
                    item.OnDateFormat = string.Format("{0} minutes ago", dif);
                }
                else if (dif <= 60 * 8) // 8 hours
                {
                    dif = dif / 60;
                    item.OnDateFormat = string.Format("About {0} hours ago", dif);
                }
                else
                {
                    item.OnDateFormat = string.Format("{0:dddd, MMMM dd, yyyy HH:mm:ss}", item.OnDate);
                }

            }

            Db.Close();
            if (ret.Count == 0)
            {
                return Content("");
            }
            else
            {
                return PartialView(ret);
            }
        }

        /// <summary>
        /// Insert a message into order history
        /// </summary>
        /// <param name="OrderId"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult History_InsertComment(OrderHistory_Add model)
        {
            var order = Db.Select<Order>(m => m.Where(x => x.Id == model.OrderId).Limit(1)).FirstOrDefault();
            if (order == null)
            {
                return JsonError("Order is not found");
            }

            Order_History k = new Order_History();
            k.Content = model.Message;
            k.isPrivate = model.isPrivate;
            k.OnDate = DateTime.Now;
            k.Order_Id = model.OrderId;
            k.UserName = CurrentUser.FullName;
            k.UserId = CurrentUser.Id;
            Db.Insert<Order_History>(k);

            // update order to let them know we have new message from photobookmart
            Db.UpdateOnly<Order>(new Order() { FlagHistoryMessage = (int)Enum_FlagOrderMessage.NewMessageFromPhotobookmart },
               ev => ev.Update(p => new
               {
                   p.FlagHistoryMessage
               }).Where(m => m.Id == order.Id));

            return JsonSuccess("", "Your message has been added");
        }
        #endregion

        #region Invoice
        public ActionResult Invoice_SendToEmail(long id)
        {
            Order ret = new Order();
            var user = CurrentUser;
            ret = Db.Select<Order>(x => x.Where(m => m.Id == id).Limit(1)).FirstOrDefault();


            if (ret != null && ret.Id > 0)
            {
                new OrderLib(ret).Invoice_SendEmail();
                ret.AddHistory("Resend invoice to customer email " + ret.Customer_Email, CurrentUser.FullName, CurrentUser.Id);
                return JsonSuccess("", string.Format("Invoice {0} has been sent to email {1}", ret.Order_Number, ret.Customer_Email));
            }
            else
            {
                return JsonError("Can not find your order");
            }
        }
        #endregion

        #region Email Template
        /// <summary>
        /// Get email template by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult Email_Get_Template(int id, long order_id)
        {
            var order = Db.Select<Order>(x => x.Where(m => m.Id == order_id).Limit(1)).FirstOrDefault();

            var item = Db.Where<Site_MaillingListTemplate>(m => m.Id == id).FirstOrDefault();
            if (item == null || order == null)
            {
                return Json(new { Title = "", Body = "" });
            }
            else
            {
                // now we compile the model into view by replace the tokens
                var title = item.Title;
                var body = item.Body;
                order.LoadAddress(0);
                order.LoadAddress(1);
                Dictionary<string, string> dic = order.GetPropertiesListOrValue(true);
                var billing = order.BillingAddressModel.GetPropertiesListOrValue(true, "Billing_");
                var shipping = order.BillingAddressModel.GetPropertiesListOrValue(true, "Shipping_");
                dic = dic.MergeDictionary(billing);
                dic = dic.MergeDictionary(shipping);

                //
                SendMail_RenderBeforeSend(ref body, ref title, dic);

                return Json(new { Title = title, Body = body });
            }
        }

        /// <summary>
        /// Preview email body content
        /// </summary>
        /// <param name="id">Order Id</param>
        /// <param name="content">Email body content</param>
        /// <returns></returns>
        [ValidateInput(false)]
        [HttpPost]
        public ActionResult Email_Preview(int id, string content)
        {
            var order = Db.Select<Order>(x => x.Where(m => m.Id == id).Limit(1)).FirstOrDefault();

            if (order == null)
            {
                return Content("");
            }
            else
            {
                // now we compile the model into view by replace the tokens
                var title = "";
                var body = content;
                order.LoadAddress(0);
                order.LoadAddress(1);
                Dictionary<string, string> dic = order.GetPropertiesListOrValue(true, "");
                var billing = order.BillingAddressModel.GetPropertiesListOrValue(true, "Billing_");
                var shipping = order.BillingAddressModel.GetPropertiesListOrValue(true, "Shipping_");
                dic = dic.MergeDictionary(billing);
                dic = dic.MergeDictionary(shipping);

                //
                SendMail_RenderBeforeSend(ref body, ref title, dic);

                return Content(body);
            }
        }

        /// <summary>
        /// Send email to customer
        /// </summary>
        /// <param name="id">Order id</param>
        /// <param name="content"></param>
        /// <returns></returns>
        [ValidateInput(false)]
        [HttpPost]
        public ActionResult Email_Sendmail_Customer(int id, string title, string content)
        {
            var order = Db.Select<Order>(x => x.Where(m => m.Id == id).Limit(1)).FirstOrDefault();

            if (order == null)
            {
                return JsonError("Order is not found");
            }
            else
            {
                // now we compile the model into view by replace the tokens
                order.LoadAddress(0);
                order.LoadAddress(1);
                Dictionary<string, string> dic = order.GetPropertiesListOrValue(true, "");
                var billing = order.BillingAddressModel.GetPropertiesListOrValue(true, "Billing_");
                var shipping = order.BillingAddressModel.GetPropertiesListOrValue(true, "Shipping_");
                dic = dic.MergeDictionary(billing);
                dic = dic.MergeDictionary(shipping);

                //
                SendMail_RenderBeforeSend(ref content, ref title, dic);
                var u = CurrentUser;

                PhotoBookmart.Common.Helpers.SendEmail.SendMail(order.Customer_Email, title, content, CurrentWebsite.Email_Support, "Photobookmart Customer Support");

                order.AddHistory(string.Format("<b>Send message to customer</b>\r\nTitle: {0}\r\nContent:{1}", title, content), u.FullName, u.Id, false);

                return JsonSuccess("", string.Format("Your email has been sent to {0} under your name ({1} - {2})", order.Customer_Email, u.FullName, u.Email));
            }
        }
        #endregion

        #region Shipping
        public ActionResult Shipping_UpdateNote(Order model)
        {
            var can_send_email_update_status = false;
            var order = Db.Select<Order>(x => x.Where(m => m.Id == model.Id).Limit(1)).FirstOrDefault();

            if (order == null)
            {
                return JsonError("Can not find your order");
            }
            else
            {
                var current_user = CurrentUser;
                // Shipping note
                if (string.IsNullOrEmpty(model.ShippingNote))
                {
                    model.ShippingNote = "";
                }

                if (string.IsNullOrEmpty(order.ShippingNote))
                {
                    order.ShippingNote = "";
                }

                if (model.ShippingNote.Trim() != order.ShippingNote.Trim())
                {

                    Db.UpdateOnly<Order>(new Order() { ShippingNote = model.ShippingNote }, ev => ev.Update(p => p.ShippingNote).Where(m => m.Id == order.Id).Limit(1));
                    // add history
                    order.AddHistory(string.Format("Update shipping note\r\n<b>Old value</b>:{0}\r\n<b>Newvalue</b>:{1}", order.ShippingNote, model.ShippingNote), current_user.FullName, current_user.Id);
                }

                #region Adjust shipping
                // Shipping Real Price
                if (order.Shipping_RealPrice != model.Shipping_RealPrice && model.Shipping_RealPrice >= 0)
                {
                    order.AddHistory(string.Format("Update Shipping Real price from <b>{2} {0:0.##}</b> to <b>{2} {1:0.##}</b>", order.Shipping_RealPrice, model.Shipping_RealPrice, DefaultCurrency), current_user.FullName, current_user.Id, true);
                    // update it
                    order.Shipping_RealPrice = model.Shipping_RealPrice;

                    Db.UpdateOnly<Order>(new Order() { Shipping_RealPrice = order.Shipping_RealPrice }, ev => ev.Update(p => p.Shipping_RealPrice).Where(m => m.Id == order.Id).Limit(1));
                }

                // Shipping Display price sign
                if (string.IsNullOrEmpty(model.Shipping_DisplayPriceSign))
                {
                    model.Shipping_DisplayPriceSign = "";
                }
                if (order.Shipping_DisplayPriceSign != model.Shipping_DisplayPriceSign)
                {
                    order.AddHistory(string.Format("Update Shipping Price Display currency sign from <b>{0}</b> to <b>{1}</b>", order.Shipping_DisplayPriceSign, model.Shipping_DisplayPriceSign), current_user.FullName, current_user.Id, true);
                    // update it
                    order.Shipping_DisplayPriceSign = model.Shipping_DisplayPriceSign;

                    Db.UpdateOnly<Order>(new Order() { Shipping_DisplayPriceSign = order.Shipping_DisplayPriceSign }, ev => ev.Update(p => p.Shipping_DisplayPriceSign).Where(m => m.Id == order.Id).Limit(1));
                }

                // Shipping Display Price
                if (order.Shipping_DisplayPrice != model.Shipping_DisplayPrice && model.Shipping_DisplayPrice >= 0)
                {
                    order.AddHistory(string.Format("Update Shipping Display Price from <b>{2} {0:0.##}</b> to <b>{2} {1:0.##}</b>", order.Shipping_DisplayPrice, model.Shipping_DisplayPrice, DefaultCurrency), current_user.FullName, current_user.Id, true);
                    order.AddHistory(string.Format("Update Shipping Price from <b>{2} {0:0.##}</b> to <b>{2} {1:0.##}</b>", order.Shipping_DisplayPrice, model.Shipping_DisplayPrice, DefaultCurrency), current_user.FullName, current_user.Id);
                    // update it
                    order.Shipping_DisplayPrice = model.Shipping_DisplayPrice;

                    Db.UpdateOnly<Order>(new Order() { Shipping_DisplayPrice = order.Shipping_DisplayPrice }, ev => ev.Update(p => p.Shipping_DisplayPrice).Where(m => m.Id == order.Id).Limit(1));
                }
                #endregion

                var o_country = order.GetCountry();
                SMSTemplateModel sms_template = null;

                // Shipping Method
                if (order.Shipping_Method != model.Shipping_Method)
                {
                    can_send_email_update_status = true;
                    order.AddHistory(string.Format("Update Shipping Method from <b>{0}</b> to <b>{1}</b>", order.Shipping_Method.DisplayName(), model.Shipping_Method.DisplayName()), current_user.FullName, current_user.Id);
                    // update it
                    order.Shipping_Method = model.Shipping_Method;

                    Db.UpdateOnly<Order>(new Order() { Shipping_Method = order.Shipping_Method }, ev => ev.Update(p => p.Shipping_Method).Where(m => m.Id == order.Id).Limit(1));
                }

                // Shipping Status
                if (order.Shipping_Status != model.Shipping_Status)
                {
                    can_send_email_update_status = true;
                    order.AddHistory(string.Format("Update Shipping Status from <b>{0}</b> to <b>{1}</b>", order.Shipping_Status.DisplayName(), model.Shipping_Status.DisplayName()), current_user.FullName, current_user.Id);
                    // update it
                    order.Shipping_Status = model.Shipping_Status;
                    if (order.Shipping_Status == Enum_ShippingStatus.Shipped)
                    {
                        order.Shipping_ShipOn = DateTime.Now;
                    }

                    Db.UpdateOnly<Order>(new Order() { Shipping_Status = order.Shipping_Status, Shipping_ShipOn = order.Shipping_ShipOn }, ev => ev.Update(p => new { p.Shipping_Status, p.Shipping_ShipOn }).Where(m => m.Id == order.Id).Limit(1));

                    // status only
                    if (o_country != null)
                    {
                        sms_template = SMS_GetTemplate("order_shipping_status_update", o_country.Code);
                    }
                }

                if (order.Shipping_Status == Enum_ShippingStatus.Shipped && order.Shipping_TrackingNumber != model.Shipping_TrackingNumber)
                {
                    can_send_email_update_status = true;

                    order.AddHistory(string.Format("Update Shipping Tracking Number <b>{0}</b>", model.Shipping_TrackingNumber), current_user.FullName, current_user.Id);
                    // update it
                    order.Shipping_TrackingNumber = model.Shipping_TrackingNumber;

                    Db.UpdateOnly<Order>(new Order() { Shipping_TrackingNumber = order.Shipping_TrackingNumber }, ev => ev.Update(p => p.Shipping_TrackingNumber).Where(m => m.Id == order.Id).Limit(1));

                    // with tracking number
                    if (o_country != null)
                    {
                        sms_template = SMS_GetTemplate("order_shipping_status_and_tracking_number_update", o_country.Code);
                    }
                }

                if (order.TotalWeight != model.TotalWeight)
                {
                    order.AddHistory(string.Format("Update product total weight from <b>{0}</b> to <b>{1}</b>", order.TotalWeight.ToWeightDimentionFormated(), model.TotalWeight.ToWeightDimentionFormated()), current_user.FullName, current_user.Id);
                    // update it
                    order.Shipping_TrackingNumber = model.Shipping_TrackingNumber;

                    Db.UpdateOnly<Order>(new Order() { Shipping_TrackingNumber = order.Shipping_TrackingNumber }, ev => ev.Update(p => p.Shipping_TrackingNumber).Where(m => m.Id == order.Id).Limit(1));
                }

                // Update LastUpdate field for Order
                if (can_send_email_update_status)
                {
                    Db.UpdateOnly<Order>(new Order() { LastUpdate = DateTime.Now },
                        ev => ev.Update(p => new
                        {
                            p.LastUpdate
                        }).Where(m => m.Id == order.Id).Limit(1));
                }

                #region Send email update to customer
                if (can_send_email_update_status)
                {
                    // get the template
                    var template = Get_MaillingListTemplate("order___shipping_status_update");

                    if (template != null)
                    {
                        // now we compile the model into view by replace the tokens
                        order.LoadAddress(0);
                        order.LoadAddress(1);

                        if (string.IsNullOrEmpty(order.Shipping_TrackingNumber))
                        {
                            order.Shipping_TrackingNumber = "";
                        }

                        Dictionary<string, string> dic = order.GetPropertiesListOrValue(true, "");
                        var billing = order.BillingAddressModel.GetPropertiesListOrValue(true, "Billing_");
                        var shipping = order.BillingAddressModel.GetPropertiesListOrValue(true, "Shipping_");
                        dic = dic.MergeDictionary(billing);
                        dic = dic.MergeDictionary(shipping);

                        //
                        var content = template.Body;
                        var title = template.Title;
                        SendMail_RenderBeforeSend(ref content, ref title, dic);
                        var u = CurrentUser;

                        PhotoBookmart.Common.Helpers.SendEmail.SendMail(order.Customer_Email, title, content, CurrentWebsite.Email_Support, "Photobookmart Customer Support");

                        // send sms
                        if (sms_template != null)
                        {
                            content = sms_template.Content;
                            SendMail_RenderBeforeSend(ref content, ref title, dic);

                            var o_u = order.Get_CustomerUserAccount();
                            var number = SMS_Normalize_PhoneNumber(o_u.Phone, o_country.PhoneNumberPrefix, o_country.Code);
                            SMSTransferAgentTask.Send(number, content, sms_template.IsFlash);
                        }

                        order.AddHistory(string.Format("<b>Update shipping status to customer.</b>\r\nTitle: {0}\r\nContent:{1}", title, content), u.FullName, u.Id, false);

                        order.Update_LastUpdate();
                    }
                }

                #endregion

                return JsonSuccess("", "Shipping has been updated");
            }

        }
        #endregion

        #region Address
        /// <summary>
        /// Update the billing address or shipping address
        /// </summary>
        /// <param name="model"></param>
        /// <param name="OrderId"></param>
        /// <returns></returns>
        public ActionResult Address_Update(AddressModel model, long OrderId)
        {
            var order = Db.Select<Order>(x => x.Where(m => m.Id == OrderId).Limit(1)).FirstOrDefault();

            if (order == null)
            {
                return JsonError("Can not find your order");
            }
            else
            {
                if (model.Id != order.Payment_BillingAddress && model.Id != order.ShippingAddress)
                {
                    return JsonError("You have no permission to update this address");
                }

                string address_name = "";
                AddressModel compare = new AddressModel();
                if (model.Id == order.Payment_BillingAddress)
                {
                    order.LoadAddress(0);
                    compare = order.BillingAddressModel;
                    address_name = "Billing Address";
                }
                else
                {
                    order.LoadAddress(1);
                    compare = order.ShippingAddressModel;
                    address_name = "Shipping Address";
                }

                string content = "";
                // first name
                if (model.FirstName != compare.FirstName)
                {
                    content += string.Format("\r\n First name change from {0} to {1}", compare.FirstName, model.FirstName);
                }
                // LastName
                if (model.LastName != compare.LastName)
                {
                    content += string.Format("\r\n Last name change from {0} to {1}", compare.LastName, model.LastName);
                }
                // Email
                if (model.Email != compare.Email)
                {
                    content += string.Format("\r\n Email change from {0} to {1}", compare.Email, model.Email);
                }
                // Company
                if (model.Company != compare.Company)
                {
                    content += string.Format("\r\n Company change from {0} to {1}", compare.Company, model.Company);
                }
                // Country
                if (model.Country != compare.Country)
                {
                    content += string.Format("\r\n Country change from {0} to {1}", compare.Country, model.Country);
                }
                // City
                if (model.City != compare.City)
                {
                    content += string.Format("\r\n City change from {0} to {1}", compare.City, model.City);
                }
                // Address
                if (model.Address != compare.Address)
                {
                    content += string.Format("\r\n Address change from {0} to {1}", compare.Address, model.Address);
                }

                // State
                if (model.State != compare.State)
                {
                    content += string.Format("\r\n State change from {0} to {1}", compare.State, model.State);
                }

                // ZipPostalCode
                if (model.ZipPostalCode != compare.ZipPostalCode)
                {
                    content += string.Format("\r\n Zip / PostalCode change from {0} to {1}", compare.ZipPostalCode, model.ZipPostalCode);
                }
                // PhoneNumber
                if (model.PhoneNumber != compare.PhoneNumber)
                {
                    content += string.Format("\r\n Phone number change from {0} to {1}", compare.PhoneNumber, model.PhoneNumber);
                }
                // Fax number
                if (model.FaxNumber != compare.FaxNumber)
                {
                    content += string.Format("\r\n Fax number change from {0} to {1}", compare.FaxNumber, model.FaxNumber);
                }

                // ------------------
                if (content != "")
                {
                    content = "Update " + address_name + content;
                    model.Id = compare.Id;
                    model.CreatedOn = compare.CreatedOn;

                    Db.Update<AddressModel>(model);

                    order.AddHistory(content, CurrentUser.FullName, CurrentUser.Id);

                    order.Update_LastUpdate();
                }

                return JsonSuccess("", "Address updated successfully");
            }
        }
        #endregion

        #region support

        string _defaultCurrency = "";

        public string DefaultCurrency
        {
            get
            {
                if (string.IsNullOrEmpty(_defaultCurrency))
                {
                    _defaultCurrency = (string)Settings.Get(Enum_Settings_Key.WEBSITE_CURRENCY, "", Enum_Settings_DataType.String);
                }
                return _defaultCurrency;
            }
        }

        /// <summary>
        /// Get the list of customer to assign order to difference customer
        /// </summary>
        /// <param name="KeyWords"></param>
        /// <param name="CustomerId"></param>
        /// <returns></returns>
        public ActionResult SearchForAssignToCus(string KeyWords, int CustomerId)
        {
            List<ABUserAuth> users = new List<ABUserAuth>();

            users.Add(new ABUserAuth() { Id = 0, FullName = "Don't change" });

            users.AddRange(Db.Select<ABUserAuth>(x => x.Where(y => (y.Id != CustomerId && y.ActiveStatus && (y.FullName.Contains(KeyWords) || y.DisplayName.Contains(KeyWords) || y.UserName.Contains(KeyWords)))).Limit(100)));

            var result = users.Select(x => new
            {
                Id = x.Id,
                Name = x.FullName + ((!string.IsNullOrEmpty(x.Email)) ? (" (" + x.Email + ")") : "")
            }).ToList();


            return Json(new
            {
                data = result
            });
        }

        private List<string> TruncOptsByOrderId(long orderId)
        {
            List<string> result = new List<string>();
            var data_options  = Db.Where<Order_ProductOptionUsing>(x => (x.Order_Id == orderId));
            var data = new List<string>();
            data_options.ForEach(x =>
            {
                data.Add(x.Option_Name);
            });
            if (data != null)
            {
                string[] keysTrunc = new string[] { "Fine silk", "Premium textured", "Superior", "Priority" };
                string[] keysHidden = new string[] { "Additional Page" };
                foreach (var d in data)
                {
                    bool isHidden = false;
                    foreach (var h in keysHidden)
                    {
                        if (d.ToLower().IndexOf(h.ToLower()) >= 0) { isHidden = true; break; }
                    }
                    if (isHidden) { continue; }

                    string valTrunc = d;
                    foreach (var t in keysTrunc)
                    {
                        if (d.ToLower().IndexOf(t.ToLower()) >= 0) { valTrunc = t; break; }
                    }
                    result.Add(valTrunc);
                }
            }
            return result;
        }

        #endregion
    }
}