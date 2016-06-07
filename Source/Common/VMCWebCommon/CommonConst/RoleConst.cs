using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhotoBookmart.Common
{
    public class RoleConst
    {
        public static string Administrator = "Administrator";

        public static string Manager="Manager";
        
        public static string Customer
        {
            get
            {
                return "Customer";
            }
        }

        public static string Staff
        {
            get
            {
                return "Staff";
            }
        }
    }
}
