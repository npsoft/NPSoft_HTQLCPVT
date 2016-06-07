using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;

namespace PhotoBookmart.Common.Helpers
{
    public static class StringHelper
    {
        /// <summary>
        /// String Extension, strip out html tag from string
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string StripHTMLTags(this string source)
        {
            return Regex.Replace(source, "<.*?>", string.Empty);
        }

        /// <summary>
        /// String Extension, to return first sentence of string
        /// </summary>
        public static string FirstSentence(this string text, bool strip_html_tag = false)
        {
            if (strip_html_tag)
            {
                text = text.StripHTMLTags();
            }
            int p1 = text.IndexOf(". ");
            int p2 = text.IndexOf("\r\n");
            if (p1 < 0)
            {
                p1 = text.Length;
            }
            if (p2 < 0)
            {
                p2 = text.Length;
            }
            if (p2 < p1)
            {
                p1 = p2;
            }
            if (p1 < 0)
            {
                p1 = text.Length;
            }
            return text.Substring(0, p1);
        }

        /// <summary>
        /// Return SEO URL base on input 
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
    }

    public class HtmlRemoval
    {
        public string StripHtml(string html, bool allowHarmlessTags = false)
        {
            return string.IsNullOrEmpty(html) ? string.Empty : System.Text.RegularExpressions.Regex.Replace(html, allowHarmlessTags ? "" : "<[^>]*>", string.Empty);
        }

        /// <summary>
        /// Remove HTML from string with Regex.
        /// </summary>
        public string StripTagsRegex(string source)
        {
            return Regex.Replace(source, "<.*?>", string.Empty);
        }

        /// <summary>
        /// Compiled regular expression for performance.
        /// </summary>
        static Regex _htmlRegex = new Regex("<.*?>", RegexOptions.Compiled);

        /// <summary>
        /// Remove HTML from string with compiled Regex.
        /// </summary>
        public string StripTagsRegexCompiled(string source)
        {
            return _htmlRegex.Replace(source, string.Empty);
        }

        /// <summary>
        /// Remove HTML tags from string using char array.
        /// </summary>
        public string StripTagsCharArray(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }
    }

}