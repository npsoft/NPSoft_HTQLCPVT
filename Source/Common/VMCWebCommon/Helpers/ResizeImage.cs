using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Web;
using System.IO;

namespace PhotoBookmart.Common.Helpers
{
    public class ResizeImage
    {
        public static Bitmap ResizeBitmap(Bitmap b, int nWidth, int nHeight)
        {
            Bitmap result = new Bitmap(nWidth, nHeight);
            using (Graphics g = Graphics.FromImage((System.Drawing.Image)result))
                g.DrawImage(b, 0, 0, nWidth, nHeight);
            return result;
        }
        public static void Resize(string path,string subfolder,HttpPostedFileBase file, string filenamefinal, int nWidth, int nHeight)
        {
            string dir = string.Empty;
            string filename = file.FileName;
            if (!String.IsNullOrEmpty(subfolder))
            {
                dir = path +"\\"+ subfolder;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            if (!String.IsNullOrEmpty(filenamefinal))
            {
                filename = filenamefinal;
            }
            System.Drawing.Image bm = System.Drawing.Image.FromStream(file.InputStream);
            bm = ResizeBitmap((Bitmap)bm, nWidth,nHeight);
            bm.Save(Path.Combine(dir, filename));
        }
    }
}
