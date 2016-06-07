using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    /// <summary>
    /// Copyright Trung Dang 
    /// </summary>
    [Alias("Site_ContactusConfig")]
    [Schema("CMS")]
    public partial class Site_ContactusConfig
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        //[Default(0)]
        //[ForeignKey(typeof(Website), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        //public int SiteId { get; set; }

        //[Ignore]
        //public string Site_Name { get; set; }

        public float Coor_Lat { get; set; }

        public float Coor_Lng { get; set; }

        public string Address { get; set; }

        public string Info { get; set; }

        public string InfoAtFooter { get; set; }

        public bool AllowRouting { get; set; }

        public bool IsHideMap { get; set; }

        public float Center_Lat { get; set; }

        public float Center_Lng { get; set; }

        public string LanguageCode { get; set; }

        public Site_ContactusConfig()
        {
            InfoAtFooter = "";
            AllowRouting = false;
            Center_Lat = 0;
            Center_Lng = 0;
            LanguageCode = "";
            Address = "";
            Info = "";
        }

       
    }
}
