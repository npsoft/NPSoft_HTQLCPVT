using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PhotoBookmart.DataLayer
{
    /// <summary>
    /// Some useful extension
    /// </summary>
    public static class StringExtension
    {
        static string ItemSeperatorString = ",";

        /// <summary>
        /// Copyright ServiceStack V3
        /// https://github.com/ServiceStack/ServiceStack/blob/v3/src/ServiceStack.Common/StringExtensions.cs
        /// </summary>
        /// <param name="items"></param>
        public static string Join(this List<string> items)
        {
            return String.Join(ItemSeperatorString, items.ToArray());
        }

        public static string Join(this List<string> items, string delimeter)
        {
            return String.Join(delimeter, items.ToArray());
        }

        /// <summary>
        /// Return SEO URL base on input 
        /// Copyright Immanuel Dang (trungdt@absoft.vn)
        /// </summary>
        public static string ToSeoUrl(this string url)
        {
            url = url.RemoveDiacritics();

            // make the url lowercase
            string encodedUrl = (url ?? "").ToLower();

            encodedUrl = encodedUrl.Replace("®", "r");

            // replace & with and
            encodedUrl = Regex.Replace(encodedUrl, @"\&+", "and");

            // remove characters
            encodedUrl = encodedUrl.Replace("'", "");

            // remove invalid characters
            encodedUrl = Regex.Replace(encodedUrl, @"[^a-z0-9]", "_");

            // remove duplicates
            encodedUrl = Regex.Replace(encodedUrl, @"-+", "-");

            // trim leading & trailing characters
            encodedUrl = encodedUrl.Trim('-');

            return encodedUrl;
        }

        /// <summary>
        /// Remove all Diacritics (non ASCII char)
        /// Copyright Immanuel Dang (trungdt@absoft.vn)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static String RemoveDiacritics(this string s)
        {
            String normalizedString = s.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < normalizedString.Length; i++)
            {
                Char c = normalizedString[i];
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Generate random string 
        /// Copyright Immanuel Dang (trungdt@absoft.vn)
        /// </summary>
        public static string GenerateRandomText(this string text, int length)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            if (!(length > 0))
            {
                length = 3; // 3 by default
            }

            var result = new string(
                Enumerable.Repeat(chars, length)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray());
            return result;
        }
        
        private static class CurrencyVNHelper
        {
            public static string Chu(string gNumber)
            {
                string result = "";
                switch (gNumber)
                {
                    case "0":
                        result = "không";
                        break;
                    case "1":
                        result = "một";
                        break;
                    case "2":
                        result = "hai";
                        break;
                    case "3":
                        result = "ba";
                        break;
                    case "4":
                        result = "bốn";
                        break;
                    case "5":
                        result = "năm";
                        break;
                    case "6":
                        result = "sáu";
                        break;
                    case "7":
                        result = "bảy";
                        break;
                    case "8":
                        result = "tám";
                        break;
                    case "9":
                        result = "chín";
                        break;
                }
                return result;
            }

            public static string Donvi(string so)
            {
                string Kdonvi = "";

                if (so.Equals("1"))
                    Kdonvi = "";
                if (so.Equals("2"))
                    Kdonvi = "nghìn";
                if (so.Equals("3"))
                    Kdonvi = "triệu";
                if (so.Equals("4"))
                    Kdonvi = "tỷ";
                if (so.Equals("5"))
                    Kdonvi = "nghìn tỷ";
                if (so.Equals("6"))
                    Kdonvi = "triệu tỷ";
                if (so.Equals("7"))
                    Kdonvi = "tỷ tỷ";

                return Kdonvi;
            }

            public static string Tach(string tach3)
            {
                string Ktach = "";
                if (tach3.Equals("000"))
                    return "";
                if (tach3.Length == 3)
                {
                    string tr = tach3.Trim().Substring(0, 1).ToString().Trim();
                    string ch = tach3.Trim().Substring(1, 1).ToString().Trim();
                    string dv = tach3.Trim().Substring(2, 1).ToString().Trim();
                    if (tr.Equals("0") && ch.Equals("0"))
                        Ktach = " không trăm lẻ " + Chu(dv.ToString().Trim()) + " ";
                    if (!tr.Equals("0") && ch.Equals("0") && dv.Equals("0"))
                        Ktach = Chu(tr.ToString().Trim()).Trim() + " trăm ";
                    if (!tr.Equals("0") && ch.Equals("0") && !dv.Equals("0"))
                        Ktach = Chu(tr.ToString().Trim()).Trim() + " trăm lẻ " + Chu(dv.Trim()).Trim() + " ";
                    if (tr.Equals("0") && Convert.ToInt32(ch) > 1 && Convert.ToInt32(dv) > 0 && !dv.Equals("5"))
                        Ktach = " không trăm " + Chu(ch.Trim()).Trim() + " mươi " + Chu(dv.Trim()).Trim() + " ";
                    if (tr.Equals("0") && Convert.ToInt32(ch) > 1 && dv.Equals("0"))
                        Ktach = " không trăm " + Chu(ch.Trim()).Trim() + " mươi ";
                    if (tr.Equals("0") && Convert.ToInt32(ch) > 1 && dv.Equals("5"))
                        Ktach = " không trăm " + Chu(ch.Trim()).Trim() + " mươi lăm ";
                    if (tr.Equals("0") && ch.Equals("1") && Convert.ToInt32(dv) > 0 && !dv.Equals("5"))
                        Ktach = " không trăm mười " + Chu(dv.Trim()).Trim() + " ";
                    if (tr.Equals("0") && ch.Equals("1") && dv.Equals("0"))
                        Ktach = " không trăm mười ";
                    if (tr.Equals("0") && ch.Equals("1") && dv.Equals("5"))
                        Ktach = " không trăm mười lăm ";
                    if (Convert.ToInt32(tr) > 0 && Convert.ToInt32(ch) > 1 && Convert.ToInt32(dv) > 0 && !dv.Equals("5"))
                        Ktach = Chu(tr.Trim()).Trim() + " trăm " + Chu(ch.Trim()).Trim() + " mươi " + Chu(dv.Trim()).Trim() + " ";
                    if (Convert.ToInt32(tr) > 0 && Convert.ToInt32(ch) > 1 && dv.Equals("0"))
                        Ktach = Chu(tr.Trim()).Trim() + " trăm " + Chu(ch.Trim()).Trim() + " mươi ";
                    if (Convert.ToInt32(tr) > 0 && Convert.ToInt32(ch) > 1 && dv.Equals("5"))
                        Ktach = Chu(tr.Trim()).Trim() + " trăm " + Chu(ch.Trim()).Trim() + " mươi lăm ";
                    if (Convert.ToInt32(tr) > 0 && ch.Equals("1") && Convert.ToInt32(dv) > 0 && !dv.Equals("5"))
                        Ktach = Chu(tr.Trim()).Trim() + " trăm mười " + Chu(dv.Trim()).Trim() + " ";

                    if (Convert.ToInt32(tr) > 0 && ch.Equals("1") && dv.Equals("0"))
                        Ktach = Chu(tr.Trim()).Trim() + " trăm mười ";
                    if (Convert.ToInt32(tr) > 0 && ch.Equals("1") && dv.Equals("5"))
                        Ktach = Chu(tr.Trim()).Trim() + " trăm mười lăm ";

                }


                return Ktach;

            }

            public static string So_chu(double gNum)
            {
                if (gNum == 0)
                    return "Không đồng";

                string lso_chu = "";
                string tach_mod = "";
                string tach_conlai = "";
                double Num = Math.Round(gNum, 0);
                string gN = Convert.ToString(Num);
                int m = Convert.ToInt32(gN.Length / 3);
                int mod = gN.Length - m * 3;
                string dau = "[+]";

                // Dau [+ , - ]
                if (gNum < 0)
                    dau = "[-]";
                dau = "";

                // Tach hang lon nhat
                if (mod.Equals(1))
                    tach_mod = "00" + Convert.ToString(Num.ToString().Trim().Substring(0, 1)).Trim();
                if (mod.Equals(2))
                    tach_mod = "0" + Convert.ToString(Num.ToString().Trim().Substring(0, 2)).Trim();
                if (mod.Equals(0))
                    tach_mod = "000";
                // Tach hang con lai sau mod :
                if (Num.ToString().Length > 2)
                    tach_conlai = Convert.ToString(Num.ToString().Trim().Substring(mod, Num.ToString().Length - mod)).Trim();

                ///don vi hang mod
                int im = m + 1;
                if (mod > 0)
                    lso_chu = Tach(tach_mod).ToString().Trim() + " " + Donvi(im.ToString().Trim());
                /// Tach 3 trong tach_conlai

                int i = m;
                int _m = m;
                int j = 1;
                string tach3 = "";
                string tach3_ = "";

                while (i > 0)
                {
                    tach3 = tach_conlai.Trim().Substring(0, 3).Trim();
                    tach3_ = tach3;
                    lso_chu = lso_chu.Trim() + " " + Tach(tach3.Trim()).Trim();
                    m = _m + 1 - j;
                    if (!tach3_.Equals("000"))
                        lso_chu = lso_chu.Trim() + " " + Donvi(m.ToString().Trim()).Trim();
                    tach_conlai = tach_conlai.Trim().Substring(3, tach_conlai.Trim().Length - 3);

                    i = i - 1;
                    j = j + 1;
                }
                if (lso_chu.Trim().Substring(0, 1).Equals("k"))
                    lso_chu = lso_chu.Trim().Substring(10, lso_chu.Trim().Length - 10).Trim();
                if (lso_chu.Trim().Substring(0, 1).Equals("l"))
                    lso_chu = lso_chu.Trim().Substring(2, lso_chu.Trim().Length - 2).Trim();
                if (lso_chu.Trim().Length > 0)
                    lso_chu = dau.Trim() + " " + lso_chu.Trim().Substring(0, 1).Trim().ToUpper() + lso_chu.Trim().Substring(1, lso_chu.Trim().Length - 1).Trim() + " đồng chẵn";

                return lso_chu.ToString().Trim();

            }
        }

        public static string GetCurrencyVNText(this double num)
        {
            try
            {
                return CurrencyVNHelper.So_chu(num);
            }
            catch { return "N/A"; }
        }

        public static string GetCurrencyVNNum(this double num)
        {
            return Regex.Replace(num.ToString("#,##0"), @"(,)", ".");
        }

        public static string GetCodeProvince(this string text)
        {
            return string.IsNullOrEmpty(text) || text.Length < 2 ? "" : text.Substring(0, 2);
        }

        public static string GetCodeDistrict(this string text)
        {
            return string.IsNullOrEmpty(text) || text.Length < 5 ? "" : text.Substring(0, 5);
        }

        public static string GetCodeVillage(this string text)
        {
            return string.IsNullOrEmpty(text) || text.Length < 10 ? "" : text.Substring(0, 10);
        }

        public static string GetGender(this string text)
        {
            return text == "Male" ? "Nam" : (text == "Female" ? "Nữ" : "");
        }

        public static string GetDateOfBirth(this string nam_sinh, string thang_sinh, string ngay_sinh)
        {
            return string.Format("{0}{1}{2}",
                string.IsNullOrEmpty(ngay_sinh) ? "" : ngay_sinh + "/",
                string.IsNullOrEmpty(thang_sinh) ? "" : thang_sinh + "/",
                nam_sinh);
        }

        public static string FormalizeName(this string str)
        {
            //Cat bo het cac dau cach thua
            if (str.Trim() == "")
                return str;
            else
            {
                str = str.Trim();
                str = str.ToLower();
                str.ToCharArray();
                for (int i = 0; i < str.Length; i++)
                    if ((str[i].ToString() == " ") && (str[i + 1].ToString() == " "))
                    {
                        str = str.Remove(i, 1).ToString();
                        i = i - 1;
                    }
                //Chuyen ky tu dau tien la chu Hoa
                str = str[0].ToString().ToUpper() + str.Remove(0, 1);
                //Chuyen ky tu dau o moi tu la chu hoa
                for (int i = 0; i < str.Length; i++)
                    if ((str[i].ToString() == " ") && (str[i + 1].ToString() != " "))
                    {
                        str = str.Substring(0, (i + 1) - 0) + str[i + 1].ToString().ToUpper() +
                            str.Substring(i + 2).ToString();
                    }

                return str;
            }
        }

        public static bool ChangeProvince(this string mahc_old, string mahc_new)
        {
            return mahc_new.Length >= 2 && mahc_old.Substring(0, 2) != mahc_new.Substring(0, 2);
        }

        public static bool ChangeDistrict(this string mahc_old, string mahc_new)
        {
            return mahc_new.Length >= 5 && mahc_old.Substring(0, 5) != mahc_new.Substring(0, 5);
        }

        public static bool ChangeVillage(this string mahc_old, string mahc_new)
        {
            return mahc_new.Length >= 10 && mahc_old.Substring(0, 10) != mahc_new.Substring(0, 10);
        }

        public static bool CheckDateOfBirth(this string type, string year, string month, string date)
        {
            int y = int.Parse(year);
            int m = string.IsNullOrEmpty(month) ? 1 : int.Parse(month);
            int d = string.IsNullOrEmpty(date) ? 1 : int.Parse(date);

            int dis = DateTime.Today.Year - y;
            if (DateTime.Today.Month > m ||
                DateTime.Today.Month == m && DateTime.Today.Day > d)
            {
                dis -= 1;
            }
            switch (type)
            {
                case "0101":
                    return dis < 4;
                case "0102":
                    return dis >= 4 && dis < 16;
                case "0103":
                    return dis >= 16 && dis <= 22;
                case "0201":
                    return dis < 4;
                case "0202":
                    return dis >= 4 && dis < 16;
                case "0203":
                    return dis >= 16;
                case "0401":
                    return dis >= 60 && dis < 80;
                case "0402":
                    return dis >= 80;
                case "0403":
                    return dis >= 80;
                case "0601":
                    return dis < 16;
                default:
                    return true;
            }
        }
    }
}
