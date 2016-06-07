using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace PhotoBookmart.Common.Helpers
{
    /// <summary>
    /// Define Token name for mailling list template render
    /// </summary>
    public enum EnumMaillingListTokens
    {
        not_match_any_key,
        current_date_time,
        link_01,

        website_domain,
        website_name,
        website_admin_email,
        website_info_email,
        website_contact_email,

        user_ipaddress,
        user_name,
        user_username,
        user_email,
    }

    public static class EnumHelper
    {
        #region Example: Get all data in enum
        /* TODO: Comments
        public enum ExEnum
        {
            A = 0,
            B = 1,
            C = 2,
            D = 3,
            E = 4
        }

        public static SelectList GetAllExEnum()
        {
            List<ListModel> result = new List<ListModel>();
            foreach (var e in (int[])Enum.GetValues(typeof(ExEnum)) ?? Enumerable.Empty<int>())
            {
                result.Add(new ListModel() { Id = e.ToString(), Name = ((ExEnum)e).ToString() });
            }
            return new SelectList(result, "Id", "Name");
        }*/
        #endregion

        /// <summary>
        /// Get the display attribute of the enum
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DisplayName(this Enum value)
        {
            Type enumType = value.GetType();
            var enumValue = Enum.GetName(enumType, value);
            MemberInfo member = enumType.GetMember(enumValue)[0];

            if (member.IsDefined(typeof(DisplayAttribute), false))
            {
                var attrs = member.GetCustomAttributes(typeof(DisplayAttribute), false);
                var outString = ((DisplayAttribute)attrs[0]).Name;

                if (((DisplayAttribute)attrs[0]).ResourceType != null)
                {
                    outString = ((DisplayAttribute)attrs[0]).GetName();
                }

                return outString;
            }
            else
            {
                return member.Name;
            }
        }      
    }
}
