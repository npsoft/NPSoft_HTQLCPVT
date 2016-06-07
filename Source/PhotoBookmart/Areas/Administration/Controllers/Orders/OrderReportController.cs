using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;

using OpenXmlPowerTools;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing; // using DocumentFormat.OpenXml.Spreadsheet;
using Pechkin;
using Pechkin.Synchronized;
using System.Drawing.Printing;

using Helper.Files;
using PhotoBookmart.Areas.Administration.Controllers;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.Controllers;
using PhotoBookmart.DataLayer;
using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    [ABRequiresAnyRole(RoleEnum.Admin, RoleEnum.Province, RoleEnum.District)]
    public class OrderReportController : WebAdminController
    {
        [HttpGet]
        public ActionResult Index()
        {
            var model = new BaoCao_DSChiTraTroCapModel();
            return View(model);
        }

        [HttpPost]
        public ActionResult BaoCao_DSChiTraTroCap(BaoCao_DSChiTraTroCapModel model)
        {
            if (model.Thang < 1 || model.Thang > 12 || model.Nam < DateTime.MinValue.Year || model.Nam > DateTime.MaxValue.Year || model.LoaiDTs.Count == 0 || model.Villages.Count == 0 ||
                model.LoaiDTs.Count(x => x.Length != 2) != 0 || model.Villages.Count(x => x.Length != GetLenMaHCByRole(RoleEnum.Village)) != 0)
            {
                return Content("Vui lòng không hack ứng dụng.");
            }

            string code_district = model.Villages.First().Substring(0, GetLenMaHCByRole(RoleEnum.District));
            if (model.Villages.Count(x => x.Substring(0, code_district.Length) != code_district) != 0 || !CurrentUser.HasRole(RoleEnum.Admin) && !code_district.StartsWith(CurrentUser.MaHC) ||
                Db.Count<DanhMuc_LoaiDT>(x => Sql.In(x.MaLDT, model.LoaiDTs)) != model.LoaiDTs.Count || Db.Count<DanhMuc_HanhChinh>(x => Sql.In(x.MaHC, model.Villages)) != model.Villages.Count)
            {
                return Content("Vui lòng không hack ứng dụng.");
            }

            string EXPORT_DIR = Server.MapPath(string.Format("~/{0}", ConfigurationManager.AppSettings["EXPORT_DIR"]));
            string EXPORT_TPL_BCDSCTTC = Server.MapPath(string.Format("~/{0}", ConfigurationManager.AppSettings["EXPORT_TPL_BCDSCTTC"]));
            string EXPORT_NAME_BCDSCTTC = "Bao-Cao-Danh-Sach-Chi-Tra-Tro-Cap";

            Guid guid = Guid.NewGuid();
            string word_path = ExportWord_BaoCao_DSChiTraTroCap(model, guid);
            if (string.IsNullOrEmpty(word_path)) { return Content("Thông báo: Không tìm thấy biến động."); }
            byte[] word_bytes = System.IO.File.ReadAllBytes(word_path);
            if (model.Action == "download")
            {
                string file_name = string.Format("{0}.docx", EXPORT_NAME_BCDSCTTC);
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(word_bytes, 0, word_bytes.Length);
                    Response.Clear();
                    Response.AddHeader("Content-Disposition", "attachmnet; filename=" + file_name);
                    Response.ContentType = "application/octet-stream"; /* "application/pdf" */
                    Response.BinaryWrite(ms.ToArray());
                    Response.Flush();
                    Response.End();
                }
            }
            else if (model.Action == "preview")
            {
                string file_name_html = string.Format("{0}.html", EXPORT_NAME_BCDSCTTC);
                string file_name_pdf = string.Format("{0}.pdf", EXPORT_NAME_BCDSCTTC);
                string dir_path = Path.Combine(EXPORT_DIR, string.Format("{0}_{1}", EXPORT_NAME_BCDSCTTC, guid));
                ConvertToHtml(word_bytes, new DirectoryInfo(dir_path), file_name_html);
                string pdf_html = FilesHelper.ReadFileWithSR(Path.Combine(dir_path, file_name_html));
                byte[] pdf_buf = new SimplePechkin(new GlobalConfig()).Convert(pdf_html);
                FilesHelper.CreateFile(Path.Combine(dir_path, file_name_pdf), pdf_buf);
                Response.Redirect(string.Format("/{0}/{1}/{2}", ConfigurationManager.AppSettings["EXPORT_DIR"], string.Format("{0}_{1}", EXPORT_NAME_BCDSCTTC, guid), file_name_pdf));
            }
            return Content("Vui lòng không hack ứng dụng.");
        }

        private string ExportWord_BaoCao_DSChiTraTroCap(BaoCao_DSChiTraTroCapModel model, Guid guid)
        {
            #region Initialize
            DateTime report_dt = new DateTime(model.Nam, model.Thang, 1, 0, 0, 0, 0);
            string query = string.Format(@"
            SELECT 
	            DT.HoTen,
	            DT.NgaySinh,
	            DT.ThangSinh,
	            DT.NamSinh,
	            DM_HC.TenHC, 
	            DM_DC.TenDiaChi,
	            DM_LDT.TenLDT,
	            (SELECT MaLDT FROM DanhMuc.DanhMuc_LoaiDT WITH (NOLOCK) WHERE MaLDT = LEFT(BD.MaLDT, 2)) AS MaLDT_Parent, 
	            (SELECT TenLDT FROM DanhMuc.DanhMuc_LoaiDT WITH (NOLOCK) WHERE MaLDT = LEFT(BD.MaLDT, 2)) AS TenLDT_Parent, 
	            BD.*
            FROM 
	            DoiTuong.DoiTuong_BienDong AS BD WITH (NOLOCK) 
	            INNER JOIN DoiTuong.DoiTuong AS DT WITH (NOLOCK) ON BD.IDDT = DT.Id 
	            INNER JOIN DanhMuc.DanhMuc_HanhChinh AS DM_HC WITH (NOLOCK) ON BD.MaHC = DM_HC.MaHC 
	            INNER JOIN DanhMuc.DanhMuc_DiaChi AS DM_DC WITH (NOLOCK) ON BD.IDDiaChi = DM_DC.IDDiaChi 
	            INNER JOIN DanhMuc.DanhMuc_LoaiDT AS DM_LDT WITH (NOLOCK) ON BD.MaLDT = DM_LDT.MaLDT 
            WHERE 
	            BD.Id IN ( 
		            SELECT MAX(Id) 
		            FROM 
			            DoiTuong.DoiTuong_BienDong WITH (NOLOCK) 
		            WHERE 
			            NgayHuong < '{0:yyyy-MM-dd HH:mm:ss.fff}' 
		            GROUP BY IDDT) 
	            AND BD.TinhTrang LIKE 'H%' 
	            AND BD.MaHC IN ('{1}') 
	            AND LEFT(BD.MaLDT, 2) IN ('{2}');",
                report_dt.AddMonths(1), string.Join("', '", model.Villages), string.Join("', '", model.LoaiDTs));
            #endregion

            #region Retrieve data
            List<DoiTuong_BienDong> lst_biendong = Db.SqlList<DoiTuong_BienDong>(query);
            List<DanhMuc_HanhChinh> lst_village = Db.Select<DanhMuc_HanhChinh>(x => x.Where(y => Sql.In(y.MaHC, model.Villages)).Limit(0, model.Villages.Count));
            List<DanhMuc_LoaiDT> lst_loaidt = Db.Select<DanhMuc_LoaiDT>(x => x.Where(y => Sql.In(y.MaLDT, model.LoaiDTs)).Limit(0, model.LoaiDTs.Count));
            DanhMuc_HanhChinh obj_district = Db.Select<DanhMuc_HanhChinh>(x => x.Where(y => y.MaHC == model.Villages.First().Substring(0, GetLenMaHCByRole(RoleEnum.District))).Limit(0, 1)).First();
            object obj_setting = DataLayer.Models.System.Settings.Get(Enum_Settings_Key.TL01, obj_district.MaHC, null, Enum_Settings_DataType.Raw);
            #endregion

            #region Prepare data
            if (lst_biendong.Count == 0) { return null; }
            string EXPORT_DIR = Server.MapPath(string.Format("~/{0}", ConfigurationManager.AppSettings["EXPORT_DIR"]));
            string EXPORT_TPL_BCDSCTTC = Server.MapPath(string.Format("~/{0}", ConfigurationManager.AppSettings["EXPORT_TPL_BCDSCTTC"]));
            string EXPORT_NAME_BCDSCTTC = "Bao-Cao-Danh-Sach-Chi-Tra-Tro-Cap";

            string path_dir = Path.Combine(EXPORT_DIR, string.Format("{0}_{1}", EXPORT_NAME_BCDSCTTC, guid));
            string path_rpt = Path.Combine(path_dir, string.Format("{0}.docx", EXPORT_NAME_BCDSCTTC));
            Directory.CreateDirectory(path_dir);

            List<Source> lst_src = new List<Source>();
            foreach (DanhMuc_HanhChinh obj_village in lst_village)
            {
                List<DoiTuong_BienDong> lst_biendong_village = lst_biendong.Where(x => x.MaHC == obj_village.MaHC).ToList();
                if (lst_biendong_village.Count == 0) { continue; }
                string path_village = Path.Combine(path_dir, string.Format("{0}_{1}.docx", EXPORT_NAME_BCDSCTTC, obj_village.MaHC));
                System.IO.File.Copy(EXPORT_TPL_BCDSCTTC, path_village);
                using (WordprocessingDocument wDoc = WordprocessingDocument.Open(path_village, true))
                {
                    TextReplacer.SearchAndReplace(wDoc, "{$Thang}", model.Thang.ToString(), true);
                    TextReplacer.SearchAndReplace(wDoc, "{$Nam}", model.Nam.ToString(), true);
                    TextReplacer.SearchAndReplace(wDoc, "{$MaHC_District}", obj_district.MaHC, true);
                    TextReplacer.SearchAndReplace(wDoc, "{$TenHC_District}", obj_district.TenHC, true);
                    TextReplacer.SearchAndReplace(wDoc, "{$TenHC_Village}", obj_village.TenHC, true);
                    TextReplacer.SearchAndReplace(wDoc, "{$SoNguoi}", lst_biendong_village.Count.ToString(), true);
                    TextReplacer.SearchAndReplace(wDoc, "{$SoTien_So}", ((double)lst_biendong_village.Sum(x => x.MucTC.Value)).GetCurrencyVNNum(), true);
                    TextReplacer.SearchAndReplace(wDoc, "{$SoTien_Chu}", ((double)lst_biendong_village.Sum(x => x.MucTC.Value)).GetCurrencyVNText(), true);
                    TextReplacer.SearchAndReplace(wDoc, "{$NguoiKy}", obj_setting != null ? ((PhotoBookmart.DataLayer.Models.System.Settings)obj_setting).Value : " ", true);
                    Table wTable = wDoc.MainDocumentPart.Document.Body.Elements<Table>().First();
                    foreach(DanhMuc_LoaiDT obj_loaidt in lst_loaidt)
                    {
                        List<DoiTuong_BienDong> lst_biendong_loaidt = lst_biendong_village.Where(x => x.MaLDT.StartsWith(obj_loaidt.MaLDT)).ToList();
                        if (lst_biendong_loaidt.Count == 0) { continue; }
                        wTable.Append(GenerateTableRowLoaiI(obj_loaidt.TenLDT, lst_biendong_loaidt.Sum(x => x.MucTC.Value)));
                        foreach(var obj_biendong_group in lst_biendong_loaidt.GroupBy(x => new { x.MaLDT }))
                        {
                            wTable.Append(GenerateTableRowLoaiII(obj_biendong_group.First().TenLDT, obj_biendong_group.Sum(x => x.MucTC.Value)));
                            for(int i = 0; i < obj_biendong_group.Count(); i++)
                            {
                                wTable.Append(GenerateTableRowBienDong(obj_biendong_group.ElementAt(i), i + 1));
                            }
                        }
                    }
                    wDoc.MainDocumentPart.Document.Save();
                }
                lst_src.Add(new Source(new WmlDocument(path_village), true));
            }
            DocumentBuilder.BuildDocument(lst_src, path_rpt);
            return path_rpt;
            #endregion
        }

        private void ConvertToHtml(byte[] byteArray, DirectoryInfo desDirectory, string htmlFileName)
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
                            // For Web: string src = string.Format("http://localhost:8083/Content/Reports/{0}/{1}/{2}", desDirectory.Name, imageDirectoryRelativeName, imageFileName.Name);
                            // For Web & App: string src = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAMgAAABkCAYAAADDhn8LAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAA4FpVFh0WE1MOmNvbS5hZG9iZS54bXAAAAAAADw/eHBhY2tldCBiZWdpbj0i77u/IiBpZD0iVzVNME1wQ2VoaUh6cmVTek5UY3prYzlkIj8+IDx4OnhtcG1ldGEgeG1sbnM6eD0iYWRvYmU6bnM6bWV0YS8iIHg6eG1wdGs9IkFkb2JlIFhNUCBDb3JlIDUuMC1jMDYxIDY0LjE0MDk0OSwgMjAxMC8xMi8wNy0xMDo1NzowMSAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wUmlnaHRzPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvcmlnaHRzLyIgeG1sbnM6eG1wTU09Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9tbS8iIHhtbG5zOnN0UmVmPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvc1R5cGUvUmVzb3VyY2VSZWYjIiB4bWxuczp4bXA9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC8iIHhtcFJpZ2h0czpNYXJrZWQ9IkZhbHNlIiB4bXBNTTpEb2N1bWVudElEPSJ4bXAuZGlkOjJBNTJFOTk2MDYxMDExRTI4NTdDRjA2RUE0NkExRjNCIiB4bXBNTTpJbnN0YW5jZUlEPSJ4bXAuaWlkOjJBNTJFOTk1MDYxMDExRTI4NTdDRjA2RUE0NkExRjNCIiB4bXA6Q3JlYXRvclRvb2w9IkFkb2JlIFBob3Rvc2hvcCBDUyBXaW5kb3dzIj4gPHhtcE1NOkRlcml2ZWRGcm9tIHN0UmVmOmluc3RhbmNlSUQ9InV1aWQ6MDJjNWUwYWYtZjU3Ni0xMWUxLWI4YTgtYWYxMDgzYzBiMTc0IiBzdFJlZjpkb2N1bWVudElEPSJhZG9iZTpkb2NpZDpwaG90b3Nob3A6MDJjNWUwYWUtZjU3Ni0xMWUxLWI4YTgtYWYxMDgzYzBiMTc0Ii8+IDwvcmRmOkRlc2NyaXB0aW9uPiA8L3JkZjpSREY+IDwveDp4bXBtZXRhPiA8P3hwYWNrZXQgZW5kPSJyIj8+lI6piAAAP8VJREFUeNrsXQd8VFXWP9P7JJNeSSC0EJJQBSnigiJNXRRwd8UuqLv27uoqltVV17rqguJ+a10V66ooCiJI7zVAQkIa6WUyvb/vnDtzx5dhAglF0Z3L75Fk5r37bjn/0++9EkEQIFZiJVaiF2lsCGIlVmIAiZVYiQEkVmIlBpBYiZUYQGIlVmIAiZVYiQEkVmIlBpBYiZUYQGIlVmIAiZVYiRVRkdN/gUCAXeIik8k6/S2RSLqsxOPxgNvtZvdotVqQSqVwtBQWXlfkPfQ5fUb10fvlcjn7O9q7/X4/WK3W8N8Gg4E9E+29vF56htrWVV/oc5fLBV6vl/2u0Wi6rPNYhb8zcuzEdYnviXyW2ko/o40lf87hcLCfdKlUKlAqlUdtK81xV3XSd0cbm56UYNXCUft4FLow4dV+sgi8K/qJ9jlvp7i9DCBtbW1QdagyDBIaqLy+fXHQlcFK8IoEDC8KhQK++OILWLJkCRiNRrjvvvsgMzMT7Hb7keIK66VJpLroXZwIqEH0HX1On/3nP/+BoqIiGDZsWBh44kLEUFZWBnPnzgWn08kI+c0334SBAwcycEUOOn/Htm3bIC8vjz0fOWn0bgLZO++8AytWrGC/33PPPdCrV68j6hT3Rwx2fBM4sN8KHDfqJ7Xd5/Oxuuh76jO1l7dJrVaz7xnBBpDQITgOxBj2798PJpMJUlNT2ZhEvrejowMeeeQRxiSofRdddBG7qP6uiK/iYDnoDXpIS08/ov81tbWQmZERZkrHW+g9AcbkvCCTShh9RDLfrp5DwI9oaW1bmmAy/cVg0C2iZtC8UH/F9BJJD10xXHqGxobm+8d7gI2zEGIW1F8+vnS/mEF2kiD0oRggnDN1p1ClZrM5zKWjNZZ/Jv5cjFjx7zTJ1J6jcQAirJaWljBA6O+u3ssBwvsYrW/8b+LK1Be672h1Rn7eVV+6ek/kmLALfryf3n2ssbRYLOwiINIcHK2tx6qTvjtZiauMCFmfIDzex0aIVN3c0rbYabMmux32he1a3ZjExIRHkOmWc+ZyvBKku5KFzwOXtJ1sEI7MaAg9piET4v50nQwR3R1Rz9HPr+68tzv3nOy+nAgnPtb7eTs5lz2ROk9lX4lGxXMlvlj7se2WDvMFbqe9uNENUGtDQvXYL687XLe1vr7xcZSS8YGAP2akx8qv2BsUYnqRF1OrA4IR1fwnFVIBXthkh8u/bIOPDqB66vfH2cwt99XV1a+1WGzzsBptdxhBDCCx8osqpLaQCsedQeKL1EoEx18kfm/u2sM+WF3tBAdqiwtW22H+12bYXO8DueAb1NLU8GpVVc1yVClHkV1B9f0UEj4GkFg59QBhnk4vI+rIy26z5XaY2/9oR3Nv4TYH+AU5KGVS0CoksKvRDzd+0w5/XmWBig4Ek9dxZn1d3Q9NTU0voz3Zi1Q3brjHABIrv/gitpn41dra/oQCAtrPylywq8kPatmPxK6Ro1oGMvi8zANXfdEK/9jqAIvbr0Aj/o81NbU7ECh3+Xxe7bGcEzGAxMovQIoIIs8RuYIB7A7XrU677ZIGVKne3eMAZRRqlOL9eoUU3D4pLNrugKu/MMN/y9zg9flMHW3NT9XW1m9CkF3GPU8nGygxgMRKWA2icrzqCg9AHs0DyYlXKqWIkQDNzc03kZD49w4rVHb4QCGTdNk2OdZpUMigxiLAg6vNcP0yC2xFFUzqdxc0Nda9WVVVvdzhdF0kQNdxkRhAYuW4ix/tgbq6hnC2AUTxOHV1ISGaDh+u+6CxselRs7njTJfLncJdz0e6loPBUKvVekPA7eizv80Ln5V6QCeXd6udSgSRFu/d2eCHPy3rgAd+sEGdDZvrdU6qqar6qL6u/l9Olys7aOP4mbQ6kSKPkUascFWmw2IBqJWwiDowoha68ZwUnC5PsbmjY7Y/oMK/HQ+o1RKzRq1YpdGoPlOp1aulUtkhuUwWoIAmBXU9Xm9uc0vrswhFWLTNBlZvAPRyGfSE32uQcgOCFD454IR1NS64tFAHswaoQG6zXFXjsJ1nMiU8ZjAa30YwWn92gPA0FB68+6UbkrwvXaXXnI7lWIHF7gYeKTKfmGACkyn+iBSXqABBadDcbJnqD8jBqN+ImFKA3TUg3uk0Xii0+y6Uycx2pUJSbtBrNsjlsvfxHXvazeYbZH6vemWNG1ZU+VAi9AwcP4ITmNrV4QZ4foMdvjzohqsK1TCljzrD3NL0SofFdp0p3nifUhn/FUmSoMfr6EHoyOwHeTdGvlMeTCTRUGU2my2YcyMEoLW1leViRWvEyXTH9bQuSagfXT2LEy1BsSxQmglP+iNi+Tmj6dEmjH8mziSgueFxhWh5ZlRsVitLRzEYDcck+KbmFpxPOWjUavAfJcWDhobe7XA4z0TlB9LjF4JBuwHf0xds7mFg9xSBw36GzunKLnI6PUVSmTC/ta2jCQR/vA/b+H87Hcy2ONERRhseFGjhH2r3wwOrrPD1IR/MK1bDkBRncUuTc6nN7lySYIp7QSaXr5XLlThmXb+R6FgcjOwWu+ceAhpgSo7jIKHJWLp06TWLFi78A00U3XfrLbdKXn75pXsGFRRs9odymYJei0CP8ruOKfpC7wvrpkqWWCkRIl7ACZx+koinzuu0WvjxriCxSaRS4YH773/+22+/LVSTGuD2wBOPP2FfuGjhlTqtrk3MTakvP5nqE8qLEwM1lNg37FDFoaetNitTj1579VXoMHd8M336tCdd2M9wW7HdWq0O2tvaoiZ+RmMkdocDDlVWQ3paCksDEY4K4IDJ4w0MUqsaQK2qQMQAqBUH8feDkCT5AALxBnB6BoLFNRasnmKw24ekGHCu3i6xwa4mD+gVJ0/jUMnI9JezYOPmOhfMztfApYM1kCG1zD5st84wGOMeT0xK/kAqlZcGusjwJTqShjxtPVKxePr5999/D+3t7exvqmzDhg15FRWHJqoRLEFOZYPWltYkhkKpDLgm6/cdXy4NEaaM6gn1R4rvbWpqMjz11FM344QrqE2UObxkyZLaBx544N9ardbHn6OIK+nIRwCGLhwMWYhTUFvp3jU/rBldXVU9ivpCiY0eoqiAIKd+crAHQgmZHPCnWsJw75BYchOXczqdiZWHDk0k6U1j4LA7wKA3NNjxb3cIIGSoxsXFQXFxMdP9u5umQeNCz7Z3WCArI72Tl6tT2/Cf3W6b4vZIEpOM20Eha8KXhsbDz9UgK+jUm0FnwMs+Cmpc/4Yqixte3+5Eglac/PHClupQZSM38hs7nbC8wgF/GBwHv+2v0Mis5kftDudNpvi4h7VazWsoIb00JmLmF0xWFMJj1W2AELemtG1KQUepwaQHVYyT5aEJ4xOoVLF0dr/YRec/AWLiGcKqkNpD7ynZs1f94QdLHjaZTOylMgTBRx9+uKlPnz6L09PTGYFQqnjBoEEgV8mZDz6S6Lx+Lw6mvBPRYJ+cLIGOpFOQm9hJKHUCbMB/RH0/ReHrNUR98IsT/mjcsf1urnIRwJOSkmDIkCGMkXXHnoiUXMTsHE4XxKFadqSqFZxLm93xOxDkoFXuwUZGJ1liRgGvBurbbsO2KuCdvWZotAugV57CCDhWrUe1q8UJ8OSGNviqXAXXD9PD2CxpSltzw8tmhXpOamrKw1oBVhJZisfWT8svjsdIJy7br18/yM7OhtraWgaSYxG3L3BiqQD0rMfrYVyNgxB/CsgRO/DXBD6ZMpnc9vHHH7P0dyImWpcS5CdHSf/2+0ApVXarHT4kMALHT50sFxlD6E4hcPTJy4NRo0ZBG9qEPQWHeOwbG5tQ+qhAIVewNRRigwHrVdjtrsFSmRsMKCWOFkxo6ZgDPs9oKEUb79NSN2gUP41tR/YJzfH+lgDc9q0FzspRoX2igsIkz4S62sPfqXW695KTEp9UqzU7xJgOBHP2ux8H4VFK4kZTpkxhC4l8YRsjugHtPYE1BpGg8vi8xzAYg14achBcfdXVMGz4MCQUz9HVN1rzQcTTDQB78f2BiNWAR1OJOHCPd5WeuB6xutkdSaPWqGHyeZPZ4qjjBYfYbmttbQ8lFgJTP+gKJSAO8vkk2WplFajUB6JLEBTAHn8qNFuuhIDECQu32cHhDYDsJ/R9ULvVaJhTDOW7Q26Yv9QKz2xyQKsrAH637Xc11TXrGpuan8D+JHDVkYEEQgDh3inxxQmOdFe6CBi0Ao4+0+v1cNZZZ3W5xJMTBIl9eobuozr489HEuUqtYro13adQKsKEpdPpwpLD7XGDD7kjqhHuSGMcCSFAKxrnXzsPZs6ciff5WFfp3fyieqkNVB+9j6laSPjCMYBHg0XtUCqCz7MFWl5f1HuJaRAHp/eRxOXLh6lv4pVtXTkeqG76Sffz1Yb0Tu4+PxbB07vzB+Yz9cpLq/oi5rWnEjDoxm1hni02J2QPhoDvdLqu8PmkinjNelRp3BA1KEfSw/I7VFVyYVW1Db6vcrEcq5/FFY6XDiUX4hMWo31yxRdt8EmpF3AmNY6Otntramo3tLV3XIFzZOCeW3lo0A0dHR2p+KEvOM8SCaofdTqZ1rV+3boJmzZvHtna0mLs1SunDSXH2kmTJm5OS0uD/v37w4b166OpI2w54Pp168eU7CsZhYZkHBKHrbCwcM0ZZ5yxQUwkbOLQhtiza3deWdnB4qqqqj779u/Tb9+6HdasWdOABua+osLCDaaEBLfD4ZDs3Lkro6WlJQONcTn3rtFPNLp050yclIcgsTsc9gYiKjRcJau+/340tr8QDf3MgwcPCtgmV5/efaoKCgq2YXsOuNwuZiN1NaZIsPb6w3Upa9etnbZt27acRCS8xITEErR3vhs/4axWd2glH2cIBIgvv/yy2O31DMa+ZOO4qrMyMwW1RnNo1Bmj9uQPyt9GbYtcxkvA2L9vX8LB8vLR2L+BOAZG8hhiG9uSEpPKzhw9emN2Tq82UiH58t5oahjVM2LkSP6R2mw2Z5G9EjLaJQajwZOallbb09hQe7uZGfxarJ+vurNYbSMkkgAzwqNqslKBuXrbrVeB1e2EV7fbQC4R4Od1nAOTXkayT+x+WLCaVD4lzB+ih9EZQr/25qZ/26zWeUaj4QGdTrueAaSuru6Mm2+55QuSKkysejySxa8vnrdl85bivz/11F2+UAyEBgU5aeCOO+985qabb7oH1RlBwjiSaOkpDl91TbXiuWeeffH555+/iTxMYYmCXPXmW27+5+133HEzzRdb/xwQNC8vfPn+hQsX3tTe3m6kdxD35GsIaHKGDh2666GHF9wzfPjwrx968MHPy8rKBiEHU3EhQvU0NzWPWrBgwe5JkyZ9+9IrL1+I9wxf8OBDzyPIxnGvDjdg6bn4+HjXFVdcsfjW22+7l4xxPxzJmZHYnFu2bJn65z//+Zk9e/bkcNcyXUXFxRWv/POVSwcMGLCB6qe+tbW1ZTzzzDPPfvLJJ7MRzFIuBfk7iXgnnXPOlw8/8vAtyGDKeZtIsnz04Ufznvzb3x6oqanpRf3mqyQ/WvIhG4PevXtX333P3Y9MnzHj9aPZKUwKazXs79dee+31t9966yJkJoHQJg+qZ5599tb8QYNe4upxOF3kmN7EAFNZpVi3n7nrA1qX25cjl1lBrSyNDhBBAg3t16EdEAcfHqiHvc0CxCkVAHB6nEmjJMmOP1nayjdtMDVPA9cUaSALHGMdTsfK3rk5o/iadFmH2awmriSRBnXfN/7v309+++23mXJSDZDjhaUDytRHHn74rrS01KrfXjTzZQom0YYDvOi0usCHHyx5AglrGNVDni/Rs/DsM8/egFxxw7Tp09+kSXvir48vevrppy8jFYYISBx34UEvrKvoysuv+PSNt96ciTMtMbebVVqdNlJXlqKk0tDvVdXVwy+/7PKvDpaVJZM6SHXzGEwoXkLGvBrfeyMyhz7PPv/cTKlE4okSVY678847XysvLzeJ+0Fl544dfe65++7X33v//RGoejk7zB1x1159zfJVq1bl071aFmvp/E7q12effjq9vr6+4N3/vDsWOXIdEfQnH318/c033fRPHugjCSMeAyqVlZW9rpt/3eK/P/OM8bLLL3sumvOBnqH+Epcv2bP3jDffeOMPfHERSaMp06bunDJ1yr/F0oupXN2wEynBkHK1XE43qm8JNH4jvV5plkG9D1Ty2iNpXiZAh2M83jcZ6u1meL/EDTq59LQBh1jt0siJQcjgkz02SNVJ4eYRepDJ1Vs1Gu3eqDYIDRqBg/RZEukkBXhQjjgbff/WW2/fHECuggCKZL0yVEWG8R083KKgFd8x48233rqBJg2JbMyiRYsuI4LiUoOeofpZHILiGBTYQwJva29X/e3xJ56z22wppBZFbp8TjOg62PC/8tLLd5SVliaTSkD9oXdRXfR+CnZS2+h3lCLw/vvvT/v2m28voZ1IInVvlAg6VJNM9C6rxdpptwtq84YNGwetXr16JrmF33jj33et+n5VPr2T78LB30ljwJMAyU7auHFD7t+ffvpBsmOqq6rzHn744WeYPoTAoHuojRxQfPwIKNSXF55/fgGquynkXo8GEFKvpHK55smnnnqJdljhLl6U9o4FDy2Yi3/buB3DnQjdJVl6rrGpidGDy+mcSJ5nvXYjVhSIsD8Qvn41NJhvAgWqVO/ssUODHef/NE6N9eKQ9E9RwaX5OnJCuFNSku+RyaR2eZfuWSSq4iFDDkyePPmjw4cP90LON5cIhBuN+0tK+pUfPDgBOZYncncPerb/gP7VxcVD1paWHhi+Z/ee/tzIJEO38tChXCQO+RdffHE5DTbnzjTBl8699IPx48f/A+9Pef655/+xb9++DJpk4oobN24ccP75568cO3bsd598/PEch9Op5EG+vLy8+quvveZrjVrjfeftty816A3htqRnpLfffvvttxoNxoPbt2//w+LFi//E4zL0/EcfffTH6dOnvR0Z86Dv6d0zL5q5IjU1tWbZ18tmHDp0KIlLBAp+frd8+cXnnXfeihXLV8xWhkBG/UBp6Lv++usfHjZs2EqUQGe99NJLD6GEUxGRazVa+GbZNxfcetttN23csOE3tbW1Wj4GBI6p06aunz1r9l9Q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";
                            // png, jpg(x), jpeg, gif(x), bmp(x), tiff, wmf(x)
                            string src = string.Format("data:image/png;base64,{0}", FilesHelper.ImageToBase64(System.Drawing.Image.FromFile(imageFileName.FullName), imageFormat));
                            XElement img = new XElement(Xhtml.img,
                                new XAttribute(NoNamespace.src, src /*imageDirectoryRelativeName + "/" + imageFileName.Name*/),
                                imageInfo.ImgStyleAttribute,
                                imageInfo.AltText != null ?
                                    new XAttribute(NoNamespace.alt, imageInfo.AltText) : null);
                            return img;
                        }
                    };
                    XElement html = HtmlConverter.ConvertToHtml(wDoc, settings);
                    var body = html.Descendants(Xhtml.body).First();
                    body.AddFirst(
                        new XElement(Xhtml.style,
                            "body { width:756px; margin:auto; }"));
                    /*body.AddFirst(
                        new XElement(Xhtml.p,
                            new XElement(Xhtml.A,
                                new XAttribute("href", "/WebForm1.aspx"), "Go back to Upload Page")));*/

                    var htmlString = html.ToString(SaveOptions.DisableFormatting);
                    System.IO.File.WriteAllText(fiHtml.FullName, htmlString, Encoding.UTF8);
                }
            }
        }

        public ActionResult List(OrderReportFilterModel model)
        {
            List<Order> c = new List<Order>();
            List<OrderReportModel> d = new List<OrderReportModel>();

            var p = PredicateBuilder.True<Order>();
            p = p.And(x => x.Payment_isPaid);
            if (model.FromDate.HasValue)
            {

                p = p.And(m => m.CreatedOn >= model.FromDate.Value);
            }
            if (model.ToDate.HasValue)
            {

                p = p.And(m => m.CreatedOn <= model.ToDate.Value);
            }

            c = Db.Select<Order>().OrderBy(x => (x.CreatedOn)).ToList();
            if (c.Count != 0)
            {
                DateTime dtFrom = DateTime.ParseExact(c.First().CreatedOn.ToString("dd/MM/yyyy"), "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                DateTime dtTo = dtFrom.AddHours(23).AddMinutes(59).AddSeconds(59);
                do
                {
                    List<Order> lstOrderTrunc = c.Where(x => (x.CreatedOn >= dtFrom && x.CreatedOn <= dtTo)).OrderBy(x => x.Shipping_DisplayPriceSign).ToList();
                    if (lstOrderTrunc.Count != 0)
                    {
                        string priceSign = lstOrderTrunc.First().Shipping_DisplayPriceSign;
                        int numOrders = 0;
                        double totalMoney = 0;
                        foreach (Order order in lstOrderTrunc)
                        {
                            if (order.Shipping_DisplayPriceSign == priceSign)
                            {
                                numOrders++;
                                totalMoney += order.Bill_Total;
                                continue;
                            }
                            d.Add(new OrderReportModel()
                            {
                                FromDate = dtFrom,
                                ToDate = dtTo,
                                PriceSign = priceSign,
                                NumOrders = numOrders,
                                TotalMoney = totalMoney
                            });
                            priceSign = order.Shipping_DisplayPriceSign;
                            numOrders = 1;
                            totalMoney = order.Bill_Total;
                        }
                        d.Add(new OrderReportModel()
                        {
                            FromDate = dtFrom,
                            ToDate = dtTo,
                            PriceSign = priceSign,
                            NumOrders = numOrders,
                            TotalMoney = totalMoney
                        });
                    }
                    dtFrom = dtFrom.Add(TimeSpan.FromDays(1));
                    dtTo = dtTo.Add(TimeSpan.FromDays(1));
                }
                while (c.Last().CreatedOn >= dtFrom);
            }

            return PartialView("_List", d);
        }

        #region Support OpenXML
        public DocumentFormat.OpenXml.Wordprocessing.TableRow GenerateTableRowLoaiI(string name, decimal money)
        {
            DocumentFormat.OpenXml.Wordprocessing.TableRow tableRow = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
            DocumentFormat.OpenXml.Wordprocessing.TableCell[] tableCell = new DocumentFormat.OpenXml.Wordprocessing.TableCell[3] { new DocumentFormat.OpenXml.Wordprocessing.TableCell(), new DocumentFormat.OpenXml.Wordprocessing.TableCell(), new DocumentFormat.OpenXml.Wordprocessing.TableCell() };
            DocumentFormat.OpenXml.Wordprocessing.TableCellProperties[] tableCellProperties = new DocumentFormat.OpenXml.Wordprocessing.TableCellProperties[3] { new DocumentFormat.OpenXml.Wordprocessing.TableCellProperties(), new DocumentFormat.OpenXml.Wordprocessing.TableCellProperties(), new DocumentFormat.OpenXml.Wordprocessing.TableCellProperties() };
            DocumentFormat.OpenXml.Wordprocessing.TableCellWidth[] tableCellWidth = new DocumentFormat.OpenXml.Wordprocessing.TableCellWidth[3] { new DocumentFormat.OpenXml.Wordprocessing.TableCellWidth(), new DocumentFormat.OpenXml.Wordprocessing.TableCellWidth(), new DocumentFormat.OpenXml.Wordprocessing.TableCellWidth() };
            DocumentFormat.OpenXml.Wordprocessing.GridSpan[] gridSpan = new DocumentFormat.OpenXml.Wordprocessing.GridSpan[3] { new DocumentFormat.OpenXml.Wordprocessing.GridSpan(), new DocumentFormat.OpenXml.Wordprocessing.GridSpan(), new DocumentFormat.OpenXml.Wordprocessing.GridSpan() };
            DocumentFormat.OpenXml.Wordprocessing.TableCellVerticalAlignment[] tableCellVerticalAlignment = new DocumentFormat.OpenXml.Wordprocessing.TableCellVerticalAlignment[3] { new DocumentFormat.OpenXml.Wordprocessing.TableCellVerticalAlignment(), new DocumentFormat.OpenXml.Wordprocessing.TableCellVerticalAlignment(), new DocumentFormat.OpenXml.Wordprocessing.TableCellVerticalAlignment() };
            DocumentFormat.OpenXml.Wordprocessing.Paragraph[] paragraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph[3] { new DocumentFormat.OpenXml.Wordprocessing.Paragraph(), new DocumentFormat.OpenXml.Wordprocessing.Paragraph(), new DocumentFormat.OpenXml.Wordprocessing.Paragraph() };
            DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties[] paragraphProperties = new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties[3] { new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties(), new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties(), new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties() };
            DocumentFormat.OpenXml.Wordprocessing.Justification[] justification = new DocumentFormat.OpenXml.Wordprocessing.Justification[3] { new DocumentFormat.OpenXml.Wordprocessing.Justification(), new DocumentFormat.OpenXml.Wordprocessing.Justification(), new DocumentFormat.OpenXml.Wordprocessing.Justification() };
            DocumentFormat.OpenXml.Wordprocessing.ParagraphMarkRunProperties[] paragraphMarkRunProperties = new DocumentFormat.OpenXml.Wordprocessing.ParagraphMarkRunProperties[3] { new DocumentFormat.OpenXml.Wordprocessing.ParagraphMarkRunProperties(), new DocumentFormat.OpenXml.Wordprocessing.ParagraphMarkRunProperties(), new DocumentFormat.OpenXml.Wordprocessing.ParagraphMarkRunProperties() };
            DocumentFormat.OpenXml.Wordprocessing.Run[] run = new DocumentFormat.OpenXml.Wordprocessing.Run[3] { new DocumentFormat.OpenXml.Wordprocessing.Run(), new DocumentFormat.OpenXml.Wordprocessing.Run(), new DocumentFormat.OpenXml.Wordprocessing.Run() };
            DocumentFormat.OpenXml.Wordprocessing.RunProperties[] runProperties = new DocumentFormat.OpenXml.Wordprocessing.RunProperties[3] { new DocumentFormat.OpenXml.Wordprocessing.RunProperties(), new DocumentFormat.OpenXml.Wordprocessing.RunProperties(), new DocumentFormat.OpenXml.Wordprocessing.RunProperties() };
            DocumentFormat.OpenXml.Wordprocessing.Text[] text= new DocumentFormat.OpenXml.Wordprocessing.Text[3] { new DocumentFormat.OpenXml.Wordprocessing.Text(), new DocumentFormat.OpenXml.Wordprocessing.Text(), new DocumentFormat.OpenXml.Wordprocessing.Text() };
            
            #region Cell #1
            tableCellWidth[0].Type = DocumentFormat.OpenXml.Wordprocessing.TableWidthUnitValues.Auto;
            gridSpan[0].Val = 4;
            tableCellVerticalAlignment[0].Val = DocumentFormat.OpenXml.Wordprocessing.TableVerticalAlignmentValues.Center;
            tableCellProperties[0].Append(tableCellWidth[0]);
            tableCellProperties[0].Append(gridSpan[0]);
            tableCellProperties[0].Append(tableCellVerticalAlignment[0]);

            justification[0].Val = DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Left;
            paragraphMarkRunProperties[0].Append(new DocumentFormat.OpenXml.Wordprocessing.Bold());
            paragraphProperties[0].Append(justification[0]);
            paragraphProperties[0].Append(paragraphMarkRunProperties[0]);
            runProperties[0].Append(new DocumentFormat.OpenXml.Wordprocessing.Bold());
            text[0] = new DocumentFormat.OpenXml.Wordprocessing.Text(name);
            run[0].Append(text[0]);
            run[0].Append(runProperties[0]);
            paragraph[0].Append(paragraphProperties[0]);
            paragraph[0].Append(run[0]);

            tableCell[0].Append(tableCellProperties[0]);
            tableCell[0].Append(paragraph[0]);
            tableRow.Append(tableCell[0]);
            #endregion

            #region Cell #2
            tableCellWidth[1].Type = DocumentFormat.OpenXml.Wordprocessing.TableWidthUnitValues.Auto;
            gridSpan[1].Val = 1;
            tableCellVerticalAlignment[1].Val = DocumentFormat.OpenXml.Wordprocessing.TableVerticalAlignmentValues.Center;
            tableCellProperties[1].Append(tableCellWidth[1]);
            tableCellProperties[1].Append(gridSpan[1]);
            tableCellProperties[1].Append(tableCellVerticalAlignment[1]);

            justification[1].Val = DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Right;
            paragraphMarkRunProperties[1].Append(new DocumentFormat.OpenXml.Wordprocessing.Bold());
            paragraphProperties[1].Append(justification[1]);
            paragraphProperties[1].Append(paragraphMarkRunProperties[1]);
            runProperties[1].Append(new DocumentFormat.OpenXml.Wordprocessing.Bold());
            text[1] = new DocumentFormat.OpenXml.Wordprocessing.Text(((double)money).GetCurrencyVNNum());
            run[1].Append(text[1]);
            run[1].Append(runProperties[1]);
            paragraph[1].Append(paragraphProperties[1]);
            paragraph[1].Append(run[1]);

            tableCell[1].Append(tableCellProperties[1]);
            tableCell[1].Append(paragraph[1]);
            tableRow.Append(tableCell[1]);
            #endregion

            #region Cell #3
            tableCellWidth[2].Type = DocumentFormat.OpenXml.Wordprocessing.TableWidthUnitValues.Auto;
            gridSpan[2].Val = 1;
            tableCellVerticalAlignment[2].Val = DocumentFormat.OpenXml.Wordprocessing.TableVerticalAlignmentValues.Center;
            tableCellProperties[2].Append(tableCellWidth[2]);
            tableCellProperties[2].Append(gridSpan[2]);
            tableCellProperties[2].Append(tableCellVerticalAlignment[2]);

            justification[2].Val = DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Center;
            paragraphMarkRunProperties[2].Append(new DocumentFormat.OpenXml.Wordprocessing.Bold());
            paragraphProperties[2].Append(justification[2]);
            paragraphProperties[2].Append(paragraphMarkRunProperties[2]);
            runProperties[2].Append(new DocumentFormat.OpenXml.Wordprocessing.Bold());
            text[2] = new DocumentFormat.OpenXml.Wordprocessing.Text(null);
            run[2].Append(text[2]);
            run[2].Append(runProperties[2]);
            paragraph[2].Append(run[2]);
            paragraph[2].Append(paragraphProperties[2]);

            tableCell[2].Append(tableCellProperties[2]);
            tableCell[2].Append(paragraph[2]);
            tableRow.Append(tableCell[2]);
            #endregion

            return tableRow;
        }

        public DocumentFormat.OpenXml.Wordprocessing.TableRow GenerateTableRowLoaiII(string name, decimal money)
        {
            DocumentFormat.OpenXml.Wordprocessing.TableRow tableRow = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
            DocumentFormat.OpenXml.Wordprocessing.TableCell[] tableCell = new DocumentFormat.OpenXml.Wordprocessing.TableCell[4] { new DocumentFormat.OpenXml.Wordprocessing.TableCell(), new DocumentFormat.OpenXml.Wordprocessing.TableCell(), new DocumentFormat.OpenXml.Wordprocessing.TableCell(), new DocumentFormat.OpenXml.Wordprocessing.TableCell() };
            DocumentFormat.OpenXml.Wordprocessing.TableCellProperties[] tableCellProperties = new DocumentFormat.OpenXml.Wordprocessing.TableCellProperties[4] { new DocumentFormat.OpenXml.Wordprocessing.TableCellProperties(), new DocumentFormat.OpenXml.Wordprocessing.TableCellProperties(), new DocumentFormat.OpenXml.Wordprocessing.TableCellProperties(), new DocumentFormat.OpenXml.Wordprocessing.TableCellProperties() };
            DocumentFormat.OpenXml.Wordprocessing.TableCellWidth[] tableCellWidth = new DocumentFormat.OpenXml.Wordprocessing.TableCellWidth[4] { new DocumentFormat.OpenXml.Wordprocessing.TableCellWidth(), new DocumentFormat.OpenXml.Wordprocessing.TableCellWidth(), new DocumentFormat.OpenXml.Wordprocessing.TableCellWidth(), new DocumentFormat.OpenXml.Wordprocessing.TableCellWidth() };
            DocumentFormat.OpenXml.Wordprocessing.GridSpan[] gridSpan = new DocumentFormat.OpenXml.Wordprocessing.GridSpan[4] { new DocumentFormat.OpenXml.Wordprocessing.GridSpan(), new DocumentFormat.OpenXml.Wordprocessing.GridSpan(), new DocumentFormat.OpenXml.Wordprocessing.GridSpan(), new DocumentFormat.OpenXml.Wordprocessing.GridSpan() };
            DocumentFormat.OpenXml.Wordprocessing.TableCellVerticalAlignment[] tableCellVerticalAlignment = new DocumentFormat.OpenXml.Wordprocessing.TableCellVerticalAlignment[4] { new DocumentFormat.OpenXml.Wordprocessing.TableCellVerticalAlignment(), new DocumentFormat.OpenXml.Wordprocessing.TableCellVerticalAlignment(), new DocumentFormat.OpenXml.Wordprocessing.TableCellVerticalAlignment(), new DocumentFormat.OpenXml.Wordprocessing.TableCellVerticalAlignment() };
            DocumentFormat.OpenXml.Wordprocessing.Paragraph[] paragraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph[4] { new DocumentFormat.OpenXml.Wordprocessing.Paragraph(), new DocumentFormat.OpenXml.Wordprocessing.Paragraph(), new DocumentFormat.OpenXml.Wordprocessing.Paragraph(), new DocumentFormat.OpenXml.Wordprocessing.Paragraph() };
            DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties[] paragraphProperties = new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties[4] { new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties(), new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties(), new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties(), new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties() };
            DocumentFormat.OpenXml.Wordprocessing.Justification[] justification = new DocumentFormat.OpenXml.Wordprocessing.Justification[4] { new DocumentFormat.OpenXml.Wordprocessing.Justification(), new DocumentFormat.OpenXml.Wordprocessing.Justification(), new DocumentFormat.OpenXml.Wordprocessing.Justification(), new DocumentFormat.OpenXml.Wordprocessing.Justification() };
            DocumentFormat.OpenXml.Wordprocessing.ParagraphMarkRunProperties[] paragraphMarkRunProperties = new DocumentFormat.OpenXml.Wordprocessing.ParagraphMarkRunProperties[4] { new DocumentFormat.OpenXml.Wordprocessing.ParagraphMarkRunProperties(), new DocumentFormat.OpenXml.Wordprocessing.ParagraphMarkRunProperties(), new DocumentFormat.OpenXml.Wordprocessing.ParagraphMarkRunProperties(), new DocumentFormat.OpenXml.Wordprocessing.ParagraphMarkRunProperties() };
            DocumentFormat.OpenXml.Wordprocessing.Run[] run = new DocumentFormat.OpenXml.Wordprocessing.Run[4] { new DocumentFormat.OpenXml.Wordprocessing.Run(), new DocumentFormat.OpenXml.Wordprocessing.Run(), new DocumentFormat.OpenXml.Wordprocessing.Run(), new DocumentFormat.OpenXml.Wordprocessing.Run() };
            DocumentFormat.OpenXml.Wordprocessing.RunProperties[] runProperties = new DocumentFormat.OpenXml.Wordprocessing.RunProperties[4] { new DocumentFormat.OpenXml.Wordprocessing.RunProperties(), new DocumentFormat.OpenXml.Wordprocessing.RunProperties(), new DocumentFormat.OpenXml.Wordprocessing.RunProperties(), new DocumentFormat.OpenXml.Wordprocessing.RunProperties() };
            DocumentFormat.OpenXml.Wordprocessing.Text[] text = new DocumentFormat.OpenXml.Wordprocessing.Text[4] { new DocumentFormat.OpenXml.Wordprocessing.Text(), new DocumentFormat.OpenXml.Wordprocessing.Text(), new DocumentFormat.OpenXml.Wordprocessing.Text(), new DocumentFormat.OpenXml.Wordprocessing.Text() };

            #region Cell #1
            tableCellWidth[0].Type = DocumentFormat.OpenXml.Wordprocessing.TableWidthUnitValues.Dxa;
            tableCellWidth[0].Width = "100";
            gridSpan[0].Val = 1;
            tableCellVerticalAlignment[0].Val = DocumentFormat.OpenXml.Wordprocessing.TableVerticalAlignmentValues.Center;
            tableCellProperties[0].Append(tableCellWidth[0]);
            tableCellProperties[0].Append(gridSpan[0]);
            tableCellProperties[0].Append(tableCellVerticalAlignment[0]);

            justification[0].Val = DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Left;
            paragraphMarkRunProperties[0].Append(new DocumentFormat.OpenXml.Wordprocessing.Bold(), new DocumentFormat.OpenXml.Wordprocessing.Italic());
            paragraphProperties[0].Append(justification[0]);
            paragraphProperties[0].Append(paragraphMarkRunProperties[0]);
            runProperties[0].Append(new DocumentFormat.OpenXml.Wordprocessing.Bold(), new DocumentFormat.OpenXml.Wordprocessing.Italic());
            text[0] = new DocumentFormat.OpenXml.Wordprocessing.Text(null);
            run[0].Append(text[0]);
            run[0].Append(runProperties[0]);
            paragraph[0].Append(run[0]);
            paragraph[0].Append(paragraphProperties[0]);

            tableCell[0].Append(tableCellProperties[0]);
            tableCell[0].Append(paragraph[0]);
            tableRow.Append(tableCell[0]);
            #endregion

            #region Cell #2
            tableCellWidth[1].Type = DocumentFormat.OpenXml.Wordprocessing.TableWidthUnitValues.Dxa;
            tableCellWidth[1].Width = "300";
            gridSpan[1].Val = 3;
            tableCellVerticalAlignment[1].Val = DocumentFormat.OpenXml.Wordprocessing.TableVerticalAlignmentValues.Center;
            tableCellProperties[1].Append(tableCellWidth[1]);
            tableCellProperties[1].Append(gridSpan[1]);
            tableCellProperties[1].Append(tableCellVerticalAlignment[1]);

            justification[1].Val = DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Left;
            paragraphMarkRunProperties[1].Append(new DocumentFormat.OpenXml.Wordprocessing.Bold(), new DocumentFormat.OpenXml.Wordprocessing.Italic());
            paragraphProperties[1].Append(justification[1]);
            paragraphProperties[1].Append(paragraphMarkRunProperties[1]);
            runProperties[1].Append(new DocumentFormat.OpenXml.Wordprocessing.Bold(), new DocumentFormat.OpenXml.Wordprocessing.Italic());
            text[1] = new DocumentFormat.OpenXml.Wordprocessing.Text(name);
            run[1].Append(text[1]);
            run[1].Append(runProperties[1]);
            paragraph[1].Append(run[1]);
            paragraph[1].Append(paragraphProperties[1]);

            tableCell[1].Append(tableCellProperties[1]);
            tableCell[1].Append(paragraph[1]);
            tableRow.Append(tableCell[1]);
            #endregion

            #region Cell #3
            tableCellWidth[2].Type = DocumentFormat.OpenXml.Wordprocessing.TableWidthUnitValues.Dxa;
            tableCellWidth[2].Width = "100";
            gridSpan[2].Val = 1;
            tableCellVerticalAlignment[2].Val = DocumentFormat.OpenXml.Wordprocessing.TableVerticalAlignmentValues.Center;
            tableCellProperties[2].Append(tableCellWidth[2]);
            tableCellProperties[2].Append(gridSpan[2]);
            tableCellProperties[2].Append(tableCellVerticalAlignment[2]);

            justification[2].Val = DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Right;
            paragraphMarkRunProperties[2].Append(new DocumentFormat.OpenXml.Wordprocessing.Bold(), new DocumentFormat.OpenXml.Wordprocessing.Italic());
            paragraphProperties[2].Append(justification[2]);
            paragraphProperties[2].Append(paragraphMarkRunProperties[2]);
            runProperties[2].Append(new DocumentFormat.OpenXml.Wordprocessing.Bold(), new DocumentFormat.OpenXml.Wordprocessing.Italic());
            text[2] = new DocumentFormat.OpenXml.Wordprocessing.Text(((double)money).GetCurrencyVNNum());
            run[2].Append(text[2]);
            run[2].Append(runProperties[2]);
            paragraph[2].Append(run[2]);
            paragraph[2].Append(paragraphProperties[2]);

            tableCell[2].Append(tableCellProperties[2]);
            tableCell[2].Append(paragraph[2]);
            tableRow.Append(tableCell[2]);
            #endregion

            #region Cell #4
            tableCellWidth[3].Type = DocumentFormat.OpenXml.Wordprocessing.TableWidthUnitValues.Dxa;
            tableCellWidth[3].Width = "100";
            gridSpan[3].Val = 1;
            tableCellVerticalAlignment[3].Val = DocumentFormat.OpenXml.Wordprocessing.TableVerticalAlignmentValues.Center;
            tableCellProperties[3].Append(tableCellWidth[3]);
            tableCellProperties[3].Append(gridSpan[3]);
            tableCellProperties[3].Append(tableCellVerticalAlignment[3]);

            justification[3].Val = DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Center;
            paragraphMarkRunProperties[3].Append(new DocumentFormat.OpenXml.Wordprocessing.Bold(), new DocumentFormat.OpenXml.Wordprocessing.Italic());
            paragraphProperties[3].Append(justification[3]);
            paragraphProperties[3].Append(paragraphMarkRunProperties[3]);
            runProperties[3].Append(new DocumentFormat.OpenXml.Wordprocessing.Bold(), new DocumentFormat.OpenXml.Wordprocessing.Italic());
            text[3] = new DocumentFormat.OpenXml.Wordprocessing.Text(null);
            run[3].Append(text[3]);
            run[3].Append(runProperties[3]);
            paragraph[3].Append(run[3]);
            paragraph[3].Append(paragraphProperties[3]);

            tableCell[3].Append(tableCellProperties[3]);
            tableCell[3].Append(paragraph[3]);
            tableRow.Append(tableCell[3]);
            #endregion

            return tableRow;
        }
        
        public DocumentFormat.OpenXml.Wordprocessing.TableRow GenerateTableRowBienDong(DoiTuong_BienDong bien_dong, int no)
        {
            TableRow tableRow = new TableRow();
            TableCell[] tableCell = new TableCell[6] { new TableCell(), new TableCell(), new TableCell(), new TableCell(), new TableCell(), new TableCell() };
            TableCellProperties[] tableCellProperties = new TableCellProperties[6] { new TableCellProperties(), new TableCellProperties(), new TableCellProperties(), new TableCellProperties(), new TableCellProperties(), new TableCellProperties() };
            TableCellWidth[] tableCellWidth = new TableCellWidth[6] { new TableCellWidth(), new TableCellWidth(), new TableCellWidth(), new TableCellWidth(), new TableCellWidth(), new TableCellWidth() };
            GridSpan[] gridSpan = new GridSpan[6] { new GridSpan(), new GridSpan(), new GridSpan(), new GridSpan(), new GridSpan(), new GridSpan() };
            TableCellVerticalAlignment[] tableCellVerticalAlignment = new TableCellVerticalAlignment[6] { new TableCellVerticalAlignment(), new TableCellVerticalAlignment(), new TableCellVerticalAlignment(), new TableCellVerticalAlignment(), new TableCellVerticalAlignment(), new TableCellVerticalAlignment() };
            Paragraph[] paragraph = new Paragraph[6] { new Paragraph(), new Paragraph(), new Paragraph(), new Paragraph(), new Paragraph(), new Paragraph() };
            ParagraphProperties[] paragraphProperties = new ParagraphProperties[6] { new ParagraphProperties(), new ParagraphProperties(), new ParagraphProperties(), new ParagraphProperties(), new ParagraphProperties(), new ParagraphProperties() };
            Justification[] justification = new Justification[6] { new Justification(), new Justification(), new Justification(), new Justification(), new Justification(), new Justification() };
            ParagraphMarkRunProperties[] paragraphMarkRunProperties = new ParagraphMarkRunProperties[6] { new ParagraphMarkRunProperties(), new ParagraphMarkRunProperties(), new ParagraphMarkRunProperties(), new ParagraphMarkRunProperties(), new ParagraphMarkRunProperties(), new ParagraphMarkRunProperties() };
            Run[] run = new Run[6] { new Run(), new Run(), new Run(), new Run(), new Run(), new Run() };
            RunProperties[] runProperties = new RunProperties[6] { new RunProperties(), new RunProperties(), new RunProperties(), new RunProperties(), new RunProperties(), new RunProperties() };
            Text[] text = new Text[6] { new Text(), new Text(), new Text(), new Text(), new Text(), new Text() };

            #region Cell #1
            tableCellWidth[0].Type = TableWidthUnitValues.Dxa;
            tableCellWidth[0].Width = "100";
            gridSpan[0].Val = 1;
            tableCellVerticalAlignment[0].Val = TableVerticalAlignmentValues.Center;
            tableCellProperties[0].Append(tableCellWidth[0]);
            tableCellProperties[0].Append(gridSpan[0]);
            tableCellProperties[0].Append(tableCellVerticalAlignment[0]);

            justification[0].Val = JustificationValues.Center;
            paragraphProperties[0].Append(justification[0]);
            paragraphProperties[0].Append(paragraphMarkRunProperties[0]);
            text[0] = new Text(no.ToString());
            run[0].Append(text[0]);
            run[0].Append(runProperties[0]);
            paragraph[0].Append(run[0]);
            paragraph[0].Append(paragraphProperties[0]);

            tableCell[0].Append(tableCellProperties[0]);
            tableCell[0].Append(paragraph[0]);
            tableRow.Append(tableCell[0]);
            #endregion

            #region Cell #2
            tableCellWidth[1].Type = TableWidthUnitValues.Dxa;
            tableCellWidth[1].Width = "100";
            gridSpan[1].Val = 1;
            tableCellVerticalAlignment[1].Val = TableVerticalAlignmentValues.Center;
            tableCellProperties[1].Append(tableCellWidth[1]);
            tableCellProperties[1].Append(gridSpan[1]);
            tableCellProperties[1].Append(tableCellVerticalAlignment[1]);

            justification[1].Val = JustificationValues.Left;
            paragraphProperties[1].Append(justification[1]);
            paragraphProperties[1].Append(paragraphMarkRunProperties[1]);
            text[1] = new Text(bien_dong.HoTen);
            run[1].Append(text[1]);
            run[1].Append(runProperties[1]);
            paragraph[1].Append(run[1]);
            paragraph[1].Append(paragraphProperties[1]);

            tableCell[1].Append(tableCellProperties[1]);
            tableCell[1].Append(paragraph[1]);
            tableRow.Append(tableCell[1]);
            #endregion

            #region Cell #3
            tableCellWidth[2].Type = TableWidthUnitValues.Dxa;
            tableCellWidth[2].Width = "100";
            gridSpan[2].Val = 1;
            tableCellVerticalAlignment[2].Val = TableVerticalAlignmentValues.Center;
            tableCellProperties[2].Append(tableCellWidth[2]);
            tableCellProperties[2].Append(gridSpan[2]);
            tableCellProperties[2].Append(tableCellVerticalAlignment[2]);

            justification[2].Val = JustificationValues.Left;
            paragraphProperties[2].Append(justification[2]);
            paragraphProperties[2].Append(paragraphMarkRunProperties[2]);
            text[2] = new Text(bien_dong.NamSinh.GetDateOfBirth(bien_dong.ThangSinh, bien_dong.NgaySinh));
            run[2].Append(text[2]);
            run[2].Append(runProperties[2]);
            paragraph[2].Append(run[2]);
            paragraph[2].Append(paragraphProperties[2]);

            tableCell[2].Append(tableCellProperties[2]);
            tableCell[2].Append(paragraph[2]);
            tableRow.Append(tableCell[2]);
            #endregion

            #region Cell #4
            tableCellWidth[3].Type = TableWidthUnitValues.Dxa;
            tableCellWidth[3].Width = "100";
            gridSpan[3].Val = 1;
            tableCellVerticalAlignment[3].Val = TableVerticalAlignmentValues.Center;
            tableCellProperties[3].Append(tableCellWidth[3]);
            tableCellProperties[3].Append(gridSpan[3]);
            tableCellProperties[3].Append(tableCellVerticalAlignment[3]);

            justification[3].Val = JustificationValues.Left;
            paragraphProperties[3].Append(justification[3]);
            paragraphProperties[3].Append(paragraphMarkRunProperties[3]);
            text[3] = new Text(bien_dong.TenDiaChi);
            run[3].Append(text[3]);
            run[3].Append(runProperties[3]);
            paragraph[3].Append(run[3]);
            paragraph[3].Append(paragraphProperties[3]);

            tableCell[3].Append(tableCellProperties[3]);
            tableCell[3].Append(paragraph[3]);
            tableRow.Append(tableCell[3]);
            #endregion

            #region Cell #5
            tableCellWidth[4].Type = TableWidthUnitValues.Dxa;
            tableCellWidth[4].Width = "100";
            gridSpan[4].Val = 1;
            tableCellVerticalAlignment[4].Val = TableVerticalAlignmentValues.Center;
            tableCellProperties[4].Append(tableCellWidth[4]);
            tableCellProperties[4].Append(gridSpan[4]);
            tableCellProperties[4].Append(tableCellVerticalAlignment[4]);

            justification[4].Val = JustificationValues.Right;
            paragraphProperties[4].Append(justification[4]);
            paragraphProperties[4].Append(paragraphMarkRunProperties[4]);
            text[4] = new Text(((double)bien_dong.MucTC.Value).GetCurrencyVNNum());
            run[4].Append(text[4]);
            run[4].Append(runProperties[4]);
            paragraph[4].Append(run[4]);
            paragraph[4].Append(paragraphProperties[4]);

            tableCell[4].Append(tableCellProperties[4]);
            tableCell[4].Append(paragraph[4]);
            tableRow.Append(tableCell[4]);
            #endregion

            #region Cell #6
            tableCellWidth[5].Type = TableWidthUnitValues.Dxa;
            tableCellWidth[5].Width = "100";
            gridSpan[5].Val = 1;
            tableCellVerticalAlignment[5].Val = TableVerticalAlignmentValues.Center;
            tableCellProperties[5].Append(tableCellWidth[5]);
            tableCellProperties[5].Append(gridSpan[5]);
            tableCellProperties[5].Append(tableCellVerticalAlignment[5]);

            justification[5].Val = JustificationValues.Center;
            paragraphProperties[5].Append(justification[5]);
            paragraphProperties[5].Append(paragraphMarkRunProperties[5]);
            text[5] = new Text(null);
            run[5].Append(text[5]);
            run[5].Append(runProperties[5]);
            paragraph[5].Append(run[5]);
            paragraph[5].Append(paragraphProperties[5]);

            tableCell[5].Append(tableCellProperties[5]);
            tableCell[5].Append(paragraph[5]);
            tableRow.Append(tableCell[5]);
            #endregion

            return tableRow;
        }
        #endregion
    }
}
