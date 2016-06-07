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
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.System;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    public class ExceptionLogController : WebAdminController
    {
        //
        public ActionResult Index()
        {
            var model = new PhotoBookmart.Areas.Administration.Models.ExceptionFilterModel();
            return View(model);
        }

        public ActionResult List(ExceptionFilterModel model)
        {
            List<Exceptions> c = new List<Exceptions>();

            var p = PredicateBuilder.True<Exceptions>();

            // for date
            if (!(model.BetweenDate == DateTime.MinValue || model.AndDate == DateTime.MinValue))
                p = p.And(m => m.ExceptionOn >= model.BetweenDate && m.ExceptionOn <= model.AndDate);
            else
                p = p.And(m => m.ExceptionOn < DateTime.Now);
            if (!string.IsNullOrEmpty(model.Search))
            {
                p = p.And(m => m.UserName.Contains(model.Search) || m.ContextBrowserAgent.Contains(model.Search) || m.ContextForm.Contains(model.Search) || m.ContextUrl.Contains(model.Search) || m.UserIp.Contains(model.Search) || m.ContextHeader.Contains(model.Search));
            }

            if (!string.IsNullOrEmpty(model.HttpMethod))
            {
                p = p.And(m => m.ContextHttpMethod.Contains(model.HttpMethod));
            }

            if (!string.IsNullOrEmpty(model.Host))
            {
                p = p.And(m => m.ServerHost.Contains(model.Host));
            }

            c = Db.Where<Exceptions>(p);

            if (model.ResultType == 1)
            {
                return Export_To_Excel(c);
            }
            else
            {
                return PartialView("_List", c);
            }
        }

        public ActionResult Detail(int id)
        {
            var item = Db.GetById<Exceptions>(id);
            return PartialView(item);
        }

        [Authenticate]
        public ActionResult Export_To_Excel(List<Exceptions> model)
        {
            using (var package = new ExcelPackage())
            {
                package.Workbook.Worksheets.Add(string.Format("{0:yyyy-MM-dd}", DateTime.Now));
                ExcelWorksheet ws = package.Workbook.Worksheets[1];
                ws.Name = "Exceptions List"; //Setting Sheet's name
                ws.Cells.Style.Font.Size = 12; //Default font size for whole sheet
                ws.Cells.Style.Font.Name = "Calibri"; //Default Font name for whole sheet

                //Merging cells and create a center heading for out table
                ws.Cells[1, 1].Value = "List of Photobookmart Exceptions"; // Heading Name
                ws.Cells[1, 1].Style.Font.Size = 22;
                ws.Cells[1, 1, 1, 10].Merge = true; //Merge columns start and end range
                ws.Cells[1, 1, 1, 10].Style.Font.Bold = true; //Font should be bold
                ws.Cells[1, 1, 1, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center; // Aligmnet is center

                int row_index = 2;

                // header
                List<string> ws_header = new List<string>();
                ws_header.Add("");
                ws_header.Add("ID");
                ws_header.Add("On");
                ws_header.Add("Server Host");
                ws_header.Add("Message");
                ws_header.Add("Source");
                ws_header.Add("StackTrace");
                ws_header.Add("UserAgent");
                ws_header.Add("SessionId");
                ws_header.Add("Http Code");
                ws_header.Add("Request Method");
                ws_header.Add("Raw URL");
                ws_header.Add("Header");
                ws_header.Add("Request Data");
                ws_header.Add("User Id");
                ws_header.Add("User Name");
                ws_header.Add("User IP");

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

                    // On
                    col_index++;
                    ws.Cells[row_index, col_index].Value = string.Format("{0:MM/dd/yyyy HH:mm:ss}", item.ExceptionOn); ;

                    // Server Host
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.ServerHost;

                    // Message
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.ExMessage;

                    // Source
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.ExSource;

                    //  Stacktrace
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.ExStackTrace;

                    // User agent
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.ContextBrowserAgent;

                    // Session id
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.ContextSessionId;

                    // http code
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.ContextHttpCode;

                    // request method
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.ContextHttpMethod;

                    // url
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.ContextUrl;

                    // hjeader
                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.ContextHeader;

                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.ContextForm;

                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.UserId;

                    ws_header.Add("User Name");

                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.UserName;

                    col_index++;
                    ws.Cells[row_index, col_index].Value = item.UserIp;
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
                var fileName = string.Format("Exceptions-Filter-{0:yyyy-MM-dd-HH-mm-ss}.xlsx", DateTime.Now);
                // mimetype from http://stackoverflow.com/questions/4212861/what-is-a-correct-mime-type-for-docx-pptx-etc
                return base.File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

    }
}
