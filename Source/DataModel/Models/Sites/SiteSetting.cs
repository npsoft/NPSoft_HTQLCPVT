using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    [Alias("SiteSetting")]
    [Schema("CMS")]
    public partial class SiteSetting
    {
        public SiteSetting()
        {
        }
    
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        [Default(typeof(string), "")]
        public string SettingName { get; set; }

        [Default(typeof(string), "")]
        public string Body { get; set; }

        [Default(typeof(string), "")]
        public string Title { get; set; }
    }
}
