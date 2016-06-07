using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web;
using System.Web.Mvc;

namespace PhotoBookmart.Support
{
    public class ABCaptchaImageResult : ActionResult
    {
        public string GetCaptchaString(int length)
        {
            int intZero = '0';
            int intNine = '9';
            int intA = 'A';
            int intZ = 'Z';
            int intCount = 0;
            int intRandomNumber = 0;
            string strCaptchaString = "";

            Random random = new Random(System.DateTime.Now.Millisecond);

            while (intCount < length)
            {
                intRandomNumber = random.Next(intZero, intZ);
                if (((intRandomNumber >= intZero) && (intRandomNumber <= intNine) || (intRandomNumber >= intA) && (intRandomNumber <= intZ)))
                {
                    strCaptchaString = strCaptchaString + (char)intRandomNumber;
                    intCount = intCount + 1;
                }
            }
            return strCaptchaString;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            Bitmap bmp = new Bitmap(125, 30);
            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.Silver);
            string randomString = GetCaptchaString(7);
            context.HttpContext.Session["CaptchaStr"] = randomString;
            g.DrawString(randomString, new Font("Courier", 16, FontStyle.Italic), new SolidBrush(Color.Black), 2, 2);
            HttpResponseBase response = context.HttpContext.Response;
            response.ContentType = "image/jpeg";
            bmp.Save(response.OutputStream, ImageFormat.Jpeg);
            bmp.Dispose();
        }
    }
}
