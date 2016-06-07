using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using ServiceStack.Common;
using System;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.DataLayer.Models.System;

public static class AdzDateHelper
{
    public static string DayOfWeekName(int dayofweek, bool show_short_format = true)
    {
        var date_long = new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        var date_short = new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

        if (dayofweek > date_short.Count())
            return "";

        if (show_short_format)
            return date_short[dayofweek];
        else
            return date_long[dayofweek];
    }

    public static string MinuteTimeToString(int minutetime)
    {
        var x = PhotoBookmart.Models.MinuteTimeModel.MinuteTimeToTime(minutetime);
        return string.Format("{0:00}:{1:00}", x.Hour, x.Minute);
    }
}


namespace PhotoBookmart.Support
{
    public static class CurrencyHelper
    {
        public static string _CurrencySign = "";
        public static string _CurrencySignFormat = "";
        /// <summary>
        /// Format the currency
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="currency_sign">if this sign is not proviced, then it will get the default currency sign</param>
        /// <returns></returns>
        public static string ToMoneyFormated(this double amount, string currency_sign = "")
        {
            if (string.IsNullOrEmpty(_CurrencySign))
            {
                _CurrencySign = (string)Settings.Get(Enum_Settings_Key.WEBSITE_CURRENCY, "", Enum_Settings_DataType.String);
            }

            if (string.IsNullOrEmpty(_CurrencySignFormat))
            {
                _CurrencySignFormat = (string)Settings.Get(Enum_Settings_Key.WEBSITE_CURRENCY_FORMAT, "{0} {1:0.##}", Enum_Settings_DataType.String);
            }

            if (string.IsNullOrEmpty(currency_sign))
            {
                currency_sign = _CurrencySign;
            }

            return string.Format(_CurrencySignFormat, currency_sign, amount);
        }
    }

    /// <summary>
    /// Dimention helper
    /// </summary>
    public static class DimentionHelper
    {
        /// <summary>
        /// Format in grams, kilograms, ...
        /// </summary>
        /// <param name="amount">Base unit in grams</param>
        /// <returns></returns>
        public static string ToWeightDimentionFormated(this double amount)
        {
            var unit = "g";
            if (amount > 1000)
            {
                amount = amount / 1000d;
                unit = "kg";
            }
            return string.Format("{0:0.##} {1}", amount, unit);
        }
    }
}