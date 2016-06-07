using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Xml.Linq;
using ServiceStack.Common;
using ServiceStack.Mvc;
using ServiceStack.OrmLite;
using PhotoBookmart.DataLayer;
using PhotoBookmart.Models;

using OpenXmlPowerTools;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing; // using DocumentFormat.OpenXml.Spreadsheet;

namespace PhotoBookmart.Controllers
{
    public class StagingController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        
        public void ExportFileWordToHttpResponse()
        {
            #region Export vn.ms-word
            //Response.Clear();
            //Response.Buffer = true;
            //Response.AddHeader("content-disposition", "attachmnet;filename=GridToWord.doc");
            //Response.Charset = "";
            //Response.ContentType = "application/vn.ms-word";

            //StringWriter sw = new StringWriter();
            //HtmlTextWriter hw = new HtmlTextWriter(sw);
            //hw.Write("<b>Email</b>:<a rel='email' href='mailto:npe.etc@gmail.com'>npe.etc@gmail.com</a>");

            //Response.Output.Write(sw);
            //Response.Flush();
            //Response.Close();
            //Response.End();
            #endregion

            List<Source> srcs = new List<Source>();
            srcs.Add(GetSrcForDistrict());
            srcs.Add(GetSrcForDistrict());
            srcs.Add(GetSrcForDistrict());

            string path = Server.MapPath(string.Format("~/Report/{0}.docx", Guid.NewGuid()));
            DocumentBuilder.BuildDocument(srcs, path);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(System.IO.File.ReadAllBytes(path), 0, System.IO.File.ReadAllBytes(path).Length);
                Response.Clear();
                Response.AddHeader("Content-Disposition", "attachmnet; filename=" + DateTime.Now.ToFileTime() + ".docx");
                Response.ContentType = "application/octet-stream";
                Response.BinaryWrite(ms.ToArray());
                Response.Flush();
                Response.End();
            }
        }
        
        public void ExportFileExcelToHttpResponse()
        {
            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachmnet;filename=GridToExcel.xls");
            Response.Charset = "";
            Response.ContentType = "application/vn.ms-excel";

            StringWriter sw = new StringWriter();
            HtmlTextWriter hw = new HtmlTextWriter(sw);
            hw.Write("<b>Email</b>:<a rel='email' href='mailto:npe.etc@gmail.com'>npe.etc@gmail.com</a>");

            Response.Output.Write(sw);
            Response.Flush();
            Response.Close();
            Response.End();
        }

        public void ExportFileExcelWithOpenXML()
        {
            //MemoryStream ms = new MemoryStream();
            //SpreadsheetDocument xl = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook);
            //WorkbookPart wbp = xl.AddWorkbookPart();
            //WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();
            //Workbook wb = new Workbook();
            //FileVersion fv = new FileVersion();
            //fv.ApplicationName = "Microsoft Office Excel";
            //Worksheet ws = new Worksheet();

            ////First cell
            //SheetData sd = new SheetData();
            //Row r1 = new Row() { RowIndex = (UInt32Value)1u };
            //Cell c1 = new Cell();
            //c1.DataType = CellValues.String;
            //c1.CellValue = new CellValue("some value");
            //r1.Append(c1);

            //// Second cell
            //Cell c2 = new Cell();
            //c2.CellReference = "C1";
            //c2.DataType = CellValues.String;
            //c2.CellValue = new CellValue("other value");
            //r1.Append(c2);
            //sd.Append(r1);

            ////third cell
            //Row r2 = new Row() { RowIndex = (UInt32Value)2u };
            //Cell c3 = new Cell();
            //c3.DataType = CellValues.String;
            //c3.CellValue = new CellValue("some string");
            //r2.Append(c3);
            //sd.Append(r2);

            //ws.Append(sd);
            //wsp.Worksheet = ws;
            //wsp.Worksheet.Save();
            //Sheets sheets = new Sheets();
            //Sheet sheet = new Sheet();
            //sheet.Name = "first sheet";
            //sheet.SheetId = 1;
            //sheet.Id = wbp.GetIdOfPart(wsp);
            //sheets.Append(sheet);
            //wb.Append(fv);
            //wb.Append(sheets);

            //xl.WorkbookPart.Workbook = wb;
            //xl.WorkbookPart.Workbook.Save();
            //xl.Close();
            //string fileName = "testOpenXml.xlsx";
            //Response.Clear();
            //byte[] dt = ms.ToArray();

            //Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            //Response.AddHeader("Content-Disposition", string.Format("attachment; filename={0}", fileName));
            //Response.BinaryWrite(dt);
            //Response.End();
        }

        public void ExportFileWithOpenXML1()
        {
            string path_src1 = Server.MapPath("~/Reports/BienDong.docx");
            string path_src2 = Server.MapPath("~/Reports/Web Brief.docx");
            string path_src3 = Server.MapPath("~/Reports/website.docx");
            string path_out1 = Server.MapPath("~/Report/Out1.docx");
            string path_out2 = Server.MapPath("~/Report/Out2.docx");
            string path_out3 = Server.MapPath("~/Report/Out3.docx");
            string path_out4 = Server.MapPath("~/Report/Out4.docx");
            string path_out5 = Server.MapPath("~/Report/Out5.docx");

            List<Source> srcs = null;
            
            srcs = new List<Source>()
            {
                new Source(new WmlDocument(path_src1), 5, 10, true)
            };
            DocumentBuilder.BuildDocument(srcs, path_out1);

            srcs = new List<Source>()
            {
                new Source(new WmlDocument(path_src1), 0, 1, false),
                new Source(new WmlDocument(path_src1), 4, false)
            };
            DocumentBuilder.BuildDocument(srcs, path_out2);

            srcs = new List<Source>()
            {
                new Source(new WmlDocument(path_src1), true),
                new Source(new WmlDocument(path_src2), false)
            };
            DocumentBuilder.BuildDocument(srcs, path_out3);

            srcs = new List<Source>()
            {
                new Source(new WmlDocument(path_src2), false),
                new Source(new WmlDocument(path_src3), true)
            };
            DocumentBuilder.BuildDocument(srcs, path_out4);

            srcs = new List<Source>()
            {
                new Source(new WmlDocument(path_src2), 0, 5, false),
                new Source(new WmlDocument(path_src3), 0, 5, true)
            };
            WmlDocument out5 = DocumentBuilder.BuildDocument(srcs);
            out5.SaveAs(path_out5);
            
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(System.IO.File.ReadAllBytes(path_out5), 0, System.IO.File.ReadAllBytes(path_out5).Length);
                Response.Clear();
                Response.AddHeader("Content-Disposition", "attachmnet; filename=Out5.docx");
                Response.ContentType = "application/octet-stream";
                Response.BinaryWrite(ms.ToArray());
                Response.Flush();
                Response.End();
            }
        }

        public void ExportFileWithOpenXML2()
        {
            string path_file = Server.MapPath("~/Reports/BienDong.docx");
            using (WordprocessingDocument wDoc = WordprocessingDocument.Open(path_file, true)) // using (WordprocessingDocument wDoc = WordprocessingDocument.Open(MemoryStream, true))
            {
                // TextReplacer.SearchAndReplace(wDoc, "DINH VAN CHINH", "ĐINH VĂN CHINH", true);

                // TextReplacer.SearchAndReplace(wDoc, "{$AFFILIATE_NAME}", "LỮ THỊ NGỌC DUYÊN", true);

                // wDoc.MainDocumentPart.Document.Body.Append(GetParagraph1());

                // wDoc.MainDocumentPart.Document.Body.Elements<Paragraph>().LastOrDefault().InsertAfterSelf(GetParagraph1());

                // wDoc.MainDocumentPart.Document.Body.Append(GetTable1());

                // wDoc.MainDocumentPart.Document.Body.Append(GetTable2());

                // DocumentFormat.OpenXml.Wordprocessing.Table tbl = wDoc.MainDocumentPart.Document.Body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>().FirstOrDefault();

                // TableRow tbl_row = new TableRow();

                // TableCell tbl_cell1 = new TableCell();
                // tbl_cell1.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = "50" }));
                // tbl_cell1.Append(new Paragraph(new Run(new Text("7"))));
                // tbl_row.Append(tbl_cell1);

                // TableCell tbl_cell2 = new TableCell();
                // tbl_cell2.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = "100" }));
                // tbl_cell2.Append(new Paragraph(new Run(new Text("XXX"))));
                // tbl_row.Append(tbl_cell2);

                // TableCell tbl_cell3 = new TableCell();
                // tbl_cell3.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = "150" }));
                // tbl_cell3.Append(new Paragraph(new Run(new Text("npe.etc@gmail.com"))));
                // tbl_row.Append(tbl_cell3);

                // tbl.Append(tbl_row);

                wDoc.MainDocumentPart.Document.Save();
            }
        }

        public void ExportFileWithOpenXML3()
        {
            string path_file_docx = Server.MapPath("~/Reports/BienDong.docx");
            string path_dir_export_relative = "Report/Export";
            string path_dir_export_absolute = Server.MapPath(string.Format("~/{0}", path_dir_export_relative));

            using (MemoryStream ms = new MemoryStream())
            {
                DirectoryInfo dir_info = new DirectoryInfo(path_dir_export_absolute);
                if (!dir_info.Exists)
                {
                    dir_info.Create();
                }

                string name_file_html = string.Format("{0}.html", Guid.NewGuid());
                ConvertToHtml(System.IO.File.ReadAllBytes(path_file_docx), dir_info, name_file_html);
                Response.Redirect(string.Format("/{0}/{1}", path_dir_export_relative, name_file_html));
            }
        }

        public static void ConvertToHtml(byte[] byteArray, DirectoryInfo desDirectory, string htmlFileName)
        {
            FileInfo fiHtml = new FileInfo(Path.Combine(desDirectory.FullName, htmlFileName));
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(byteArray, 0, byteArray.Length);
                using (WordprocessingDocument wDoc = WordprocessingDocument.Open(memoryStream, true))
                {
                    var imageDirectoryFullName = fiHtml.FullName.Substring(0, fiHtml.FullName.Length - fiHtml.Extension.Length) + "_files";
                    var imageDirectoryRelativeName = fiHtml.Name.Substring(0, fiHtml.Name.Length - fiHtml.Extension.Length) + "_files";
                    int imageCounter = 0;
                    var pageTitle = (string)wDoc
                        .CoreFilePropertiesPart
                        .GetXDocument()
                        .Descendants(DC.title)
                        .FirstOrDefault();
                    HtmlConverterSettings settings = new HtmlConverterSettings()
                    {
                        PageTitle = pageTitle,
                        FabricateCssClasses = true,
                        CssClassPrefix = "pt-",
                        RestrictToSupportedLanguages = false,
                        RestrictToSupportedNumberingFormats = false,
                        ImageHandler = imageInfo =>
                        {
                            DirectoryInfo localDirInfo = new DirectoryInfo(imageDirectoryFullName);
                            if (!localDirInfo.Exists) { localDirInfo.Create(); };
                            ++imageCounter;
                            string extensions = imageInfo.ContentType.Split('/')[1].ToLower();
                            System.Drawing.Imaging.ImageFormat imageFormat = null;
                            if (extensions == "png")
                            {
                                extensions = "gif";
                                imageFormat = System.Drawing.Imaging.ImageFormat.Gif;
                            }
                            else if (extensions == "gif")
                            {
                                imageFormat = System.Drawing.Imaging.ImageFormat.Gif;
                            }
                            else if (extensions == "bmp")
                            {
                                imageFormat = System.Drawing.Imaging.ImageFormat.Bmp;
                            }
                            else if (extensions == "jpeg")
                            {
                                imageFormat = System.Drawing.Imaging.ImageFormat.Jpeg;
                            }
                            else if (extensions == "tiff")
                            {
                                extensions = "gif";
                                imageFormat = System.Drawing.Imaging.ImageFormat.Gif;
                            }
                            else if (extensions == "wmf")
                            {
                                extensions = "wmf";
                                imageFormat = System.Drawing.Imaging.ImageFormat.Wmf;
                            }

                            if (imageFormat == null) { return null; }

                            FileInfo imageFileName = new FileInfo(imageDirectoryFullName + "/image" + imageCounter.ToString() + "." + extensions);
                            try
                            {
                                imageInfo.Bitmap.Save(imageFileName.FullName, imageFormat);
                            }
                            catch (System.Runtime.InteropServices.ExternalException)
                            {
                                return null;
                            }
                            XElement img = new XElement(Xhtml.img,
                                new XAttribute(NoNamespace.src, imageDirectoryRelativeName + "/" + imageFileName.Name),
                                imageInfo.ImgStyleAttribute,
                                imageInfo.AltText != null ?
                                    new XAttribute(NoNamespace.alt, imageInfo.AltText) : null);
                            return img;
                        }
                    };
                    XElement html = HtmlConverter.ConvertToHtml(wDoc, settings);
                    var body = html.Descendants(Xhtml.body).First();
                    body.AddFirst(
                        new XElement(Xhtml.p,
                            new XElement(Xhtml.A,
                                new XAttribute("href", "/WebForm1.aspx"), "Go back to Upload Page")));

                    var htmlString = html.ToString(SaveOptions.DisableFormatting);
                    System.IO.File.WriteAllText(fiHtml.FullName, htmlString, Encoding.UTF8);
                }
            }
        }

        public DocumentFormat.OpenXml.Wordprocessing.Paragraph GetParagraph1()
        {
            DocumentFormat.OpenXml.Wordprocessing.Paragraph para = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
                new DocumentFormat.OpenXml.Wordprocessing.Run(
                    new DocumentFormat.OpenXml.Wordprocessing.Text("Hello world!")));

            return para;
        }

        public DocumentFormat.OpenXml.Wordprocessing.Table GetTable1()
        {
            DocumentFormat.OpenXml.Wordprocessing.Table tbl = new DocumentFormat.OpenXml.Wordprocessing.Table();

            TableProperties tbl_pro = new TableProperties(
                new TableBorders(
                    new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Dotted), Size = 10 },
                    new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Dashed), Size = 15 },
                    new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Double), Size = 20 },
                    new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Inset), Size = 25 },
                    new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Outset), Size = 2 },
                    new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.None), Size = 4 }));
            tbl.AppendChild<TableProperties>(tbl_pro);

            TableRow tbl_row = new TableRow();

            TableCell tbl_cell1 = new TableCell();
            tbl_cell1.Append(new TableCellProperties(new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = "100" }));
            tbl_cell1.Append(new Paragraph(new Run(new Text("ĐINH VĂN CHINH"))));
            tbl_row.Append(tbl_cell1);

            TableCell tbl_cell2 = new TableCell(tbl_cell1.OuterXml);
            tbl_row.Append(tbl_cell2);

            tbl.Append(tbl_row);

            return tbl;
        }

        public DocumentFormat.OpenXml.Wordprocessing.Table GetTable2()
        {
            DocumentFormat.OpenXml.Wordprocessing.Table tbl = new DocumentFormat.OpenXml.Wordprocessing.Table();

            TableProperties tbl_pro = new TableProperties(
                new TableBorders(
                    new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                    new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                    new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                    new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                    new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                    new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 }));
            tbl.AppendChild(tbl_pro);

            string[,] tbl_data = new string[,] { { "Texas", "TX" }, { "California", "CA" }, { "New York", "NY" }, { "Massachusetts", "MA" } };
            for (int i = 0; i <= tbl_data.GetUpperBound(0); i++)
            {
                TableRow tbl_row = new TableRow();
                for (int j = 0; j <= tbl_data.GetUpperBound(1); j++)
                {
                    TableCell tbl_cell = new TableCell();
                    tbl_cell.Append(new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Auto }));
                    tbl_cell.Append(new Paragraph(new Run(new Text(tbl_data[i, j]))));
                    tbl_row.Append(tbl_cell);
                }
                tbl.Append(tbl_row);
            }

            return tbl;
        }

        public Source GetSrcForDistrict()
        {
            string path = Server.MapPath("~/Reports/Template-01.docx");
            Source src = new Source(new WmlDocument(path), true);
            return src;
        }

        public void DownloadReportForDistrict()
        {
            string path_rpt = GenerateReportForDistrict("01002", "Huyện A", new string[2] { "Xã Hồng Ngự 1", "Xã Hồng Ngự 2" });

            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", string.Format("attachmnet;filename={0}.docx", DateTime.Now.ToFileTime()));
            Response.ContentType = "application/vn.ms-word";
            Response.Charset = "";
            Response.BinaryWrite(System.IO.File.ReadAllBytes(path_rpt));
            Response.Flush();
            Response.Close();
            Response.End();
        }

        public string GenerateReportForDistrict(string ma_hc, string ten_hc, string[] lst_village)
        {
            List<Source> lst_src = new List<Source>();
            foreach (string village in lst_village)
            {
                lst_src.Add(new Source(new WmlDocument(GenerateReportForVillage(ma_hc, ten_hc, village)), true));
            }

            string path_rpt = Server.MapPath(string.Format("~/Report/{0}.docx", Guid.NewGuid()));
            DocumentBuilder.BuildDocument(lst_src, path_rpt);
            return path_rpt;
        }

        public string GenerateReportForVillage(string ma_hc_district, string ten_hc_district, string ten_hc_village)
        {
            string path_tpl = Server.MapPath("~/Reports/Template-01.docx");
            string path_rpt = Server.MapPath(string.Format("~/Report/{0}.docx", Guid.NewGuid()));
            System.IO.File.Copy(path_tpl, path_rpt);

            using (WordprocessingDocument wDoc = WordprocessingDocument.Open(path_rpt, true))
            {
                TextReplacer.SearchAndReplace(wDoc, "{$MaHC_District}", ma_hc_district, true);
                TextReplacer.SearchAndReplace(wDoc, "{$TenHC_District}", ten_hc_district, true);
                TextReplacer.SearchAndReplace(wDoc, "{$TenHC_Village}", ten_hc_village, true);

                wDoc.MainDocumentPart.Document.Save();
            }
            return path_rpt;
        }
    }
}
