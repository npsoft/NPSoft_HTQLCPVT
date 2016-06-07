using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack.OrmLite;

namespace PhotoBookmart.Common.ServiceStackHelper.ORM
{
    public class ABNamingStrategy : INamingStrategy
    {
        public string GetTableName(string name)
        {
            return name.Replace(" ", "_");
        }

        public string GetColumnName(string name)
        {
            return name.Replace(" ", "_");
        }
    }
}