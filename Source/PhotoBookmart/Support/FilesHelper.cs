using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Helper.Files
{
    public class FilesHelper
    {
        #region Get content type(MIME type) from file name
        public static string GetContentType(string fileName)
        {
            string contentType = "application/octetstream";
            string ext = Path.GetExtension(fileName).ToLower();
            Microsoft.Win32.RegistryKey registryKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (registryKey != null && registryKey.GetValue("Content Type") != null)
            {
                contentType = registryKey.GetValue("Content Type").ToString();
            }
            return contentType;
        }
        #endregion

        #region Copy a file to another location
        public static void CopyFile(string orgPath, string desPath, int bufSize = 8 * 1024)
        {
            using (Stream orgStream = File.OpenRead(orgPath))
            {
                using (FileStream fileStream = File.Create(desPath, (int)orgStream.Length))
                {
                    byte[] buffer = new byte[bufSize];
                    int count = 0;
                    while ((count = orgStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, count);
                    }
                    /* For failure (example)
                    byte[] buffer = new byte[bufSize];
                    int count = 0;
                    int index = 1;
                    while ((count = orgStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (index++ == 10) break;
                        fileStream.Write(buffer, 0, count);
                    }*/
                }
            }
        }
        #endregion

        #region Merge a file to another location
        public static void MergeFile(string orgPath, string desPath, int bufSize = 8 * 1024)
        {
            using (Stream orgStream = File.OpenRead(orgPath))
            {
                using (FileStream fileStream = File.OpenWrite(desPath))
                {
                    fileStream.Seek(0, SeekOrigin.End);
                    orgStream.Seek(fileStream.Length, SeekOrigin.Begin);
                    byte[] buffer = new byte[bufSize];
                    int count = 0;
                    while ((count = orgStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, count);
                    }
                }
            }
        }
        #endregion

        #region Create a file to another location
        public static void CreateFile(string path, byte[] bytes)
        {
            using (FileStream fs = File.Create(path, bytes.Length))
            {
                fs.Write(bytes, 0, bytes.Length);
            }
        }
        #endregion

        #region Write log to file & console
        public static void WriteLog(string content, string path = null)
        {
            content = string.Format("{0:MM/dd/yyyy HH:mm:ss}\t:: {1}", DateTime.Now, content);
            Console.WriteLine(content);
            if (!string.IsNullOrEmpty(path))
            {
                UTF8Encoding utf8WithoutBom = new UTF8Encoding(false);
                using (StreamWriter sw = new StreamWriter(path, true, utf8WithoutBom))
                {
                    sw.WriteLine(content);
                    sw.Close();
                    sw.Dispose();
                }
            }
        }
        #endregion

        #region Get data from excel file
        /* using OfficeOpenXml;
        public static List<string> GetDataExcelForExclude(string path, string nameCol, int idxRow)
        {
            List<string> data = new List<string>();
            using (StreamReader sr = new StreamReader(path))
            {
                using (var package = new ExcelPackage(sr.BaseStream))
                {
                    ExcelWorkbook workBook = package.Workbook;
                    if (workBook != null && workBook.Worksheets.Count != 0)
                    {
                        ExcelWorksheet currentWorksheet = workBook.Worksheets.First();
                        do
                        {
                            string item = currentWorksheet.Cells[string.Format("{0}{1}", nameCol, idxRow++)].Text;
                            if (string.IsNullOrEmpty(item)) { break; }
                            data.Add(item);

                        } while (true);
                    }
                }
                sr.Close();
                sr.Dispose();
            }
            return data;
        }

        public static List<Tuple<string, string, string, string>> GetDataExcelForCharacter(string path, string[] nameCols, int idxRow)
        {
            List<Tuple<string, string, string, string>> data = new List<Tuple<string, string, string, string>>();
            using (StreamReader sr = new StreamReader(path))
            {
                using (var package = new ExcelPackage(sr.BaseStream))
                {
                    ExcelWorkbook workBook = package.Workbook;
                    if (workBook != null && workBook.Worksheets.Count != 0)
                    {
                        ExcelWorksheet currentWorksheet = workBook.Worksheets.First();
                        do
                        {
                            string item1 = currentWorksheet.Cells[string.Format("{0}{1}", nameCols[0], idxRow)].Text;
                            string item2 = currentWorksheet.Cells[string.Format("{0}{1}", nameCols[1], idxRow)].Text;
                            string item3 = currentWorksheet.Cells[string.Format("{0}{1}", nameCols[2], idxRow)].Text;
                            string item4 = currentWorksheet.Cells[string.Format("{0}{1}", nameCols[3], idxRow)].Text;
                            if (string.Format("{0}{1}{2}{3}", item1, item2, item3, item4) == "") { break; }
                            data.Add(new Tuple<string, string, string, string>(item1, item2, item3, item4));
                            idxRow++;

                        } while (true);
                    }
                }
                sr.Close();
                sr.Dispose();
            }
            return data;
        }*/
        #endregion

        #region Read file with StreamReader
        public static string ReadFileWithSR(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                string rst = sr.ReadToEnd();
                sr.Close();
                return rst;
            }
        }

        public static List<string> ReadFileWithSRToList(string path)
        {
            List<string> rst = new List<string>();
            using (StreamReader sr = new StreamReader(path))
            {
                while (sr.Peek() > 0)
                {
                    rst.Add(sr.ReadLine());
                }
                sr.Close();
            }
            return rst;
        }
        #endregion

        #region Write file with StreamWriter
        public static void WriteFileWithSW(string path, string content)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.Write(content); // TODO: WriteLine
                sw.Flush();
                sw.Close();
            }
        }
        #endregion

        #region Read file with BinaryReader
        public static void ReadFileWithBR(string path)
        {
            using (BinaryReader br = new BinaryReader(new FileStream(path, FileMode.Open)))
            {
                // TODO:  br.ReadBoolean(), br.ReadInt32(), br.ReadDouble(), br.ReadString()
                br.Close();
            }
        }
        #endregion

        #region Write file with BinaryWriter
        public static void WriteFileWithBW(string path)
        {
            using (BinaryWriter bw = new BinaryWriter(new FileStream(path, FileMode.Create)))
            {
                // TODO: br.Write(bool), br.Write(int), br.Write(double), br.Write(string)
                bw.Close();
            }
        }
        #endregion

        #region Convert Image to Base64
        public static string ImageToBase64(System.Drawing.Image image, System.Drawing.Imaging.ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, format);
                byte[] bytes = ms.ToArray();
                return Convert.ToBase64String(bytes);
            }
        }
        #endregion

        #region Convert Base64 to Image
        public static System.Drawing.Image Base64ToImage(string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            MemoryStream ms = new MemoryStream(bytes, 0, bytes.Length);
            ms.Write(bytes, 0, bytes.Length);
            return System.Drawing.Image.FromStream(ms, true);
        }
        #endregion

        #region Others
        //#region Convert ExcelPackage to byte[]

        //using (var package = new ExcelPackage())
        //{
        //    return package.GetAsByteArray();
        //}

        //#endregion
        
        //#region Export Excel File

        //return base.File(byte[], "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", FileName);

        //#endregion

        //#region Export Csv File

        //Response.Clear();

        //Response.ContentType = "text/csv";

        //Response.AddHeader("Content-Disposition", "attachment;filename=" + FileName);

        //MemoryStream ms = new MemoryStream(byte[]);

        //Response.OutputStream.Write(ms.ToArray(), 0, ms.ToArray().Length);

        //Response.End();

        //#endregion

        //#region Export Text File

        //MemoryStream ms = new MemoryStream();

        //StreamWriter sw = new StreamWriter(ms, Encoding.UTF8);

        //sw.Flush();

        //Response.Clear();

        //Response.ContentType = "text/plain";

        //Response.AddHeader("Content-Disposition", "attachment;filename=" + FileName);

        //Response.OutputStream.Write(ms.ToArray(), 0, ms.ToArray().Length);

        //Response.End();

        //#endregion

        //#region Write file to response
        //public void WriteFileResponse(byte[] bytes, string contentType, string contentLength, string fileName)
        //{
        //    HttpContext.Current.Response.Clear();
        //    HttpContext.Current.Response.ClearHeaders();
        //    HttpContext.Current.Response.ContentType = contentType;
        //    HttpContext.Current.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
        //    HttpContext.Current.Response.AddHeader("Content-Length", contentLength);
        //    HttpContext.Current.Response.OutputStream.Write(bytes, 0, bytes.Length);
        //    HttpContext.Current.Response.Flush();
        //    HttpContext.Current.Response.End();
        //}
        //#endregion

        //#region path -> byte[]
        //return System.IO.File.ReadAllBytes(path);
        //#endregion

        //#region string -> byte[]
        //return Encoding.UTF8.GetBytes(string);
        //#endregion

        //#region Stream -> string
        //StreamReader sr = new StreamReader(Stream);
        //return sr.ReadToEnd();
        //#endregion

        //#region Stream -> byte[]
        //public static byte[] GetBytes(Stream stream)
        //{
        //    byte[] buffer = new byte[16 * 1024];
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        int read;
        //        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        //        {
        //            ms.Write(buffer, 0, read);
        //        }
        //        return ms.ToArray();
        //    }
        //}
        //#endregion

        //#region byte[] -> MemoryStream
        //return new MemoryStream(byte[]);
        //#endregion
        #endregion

        public FilesHelper() { }
    }
}
