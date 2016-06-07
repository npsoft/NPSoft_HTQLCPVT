using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    /// <summary>
    /// Copyright Trung Dang 
    /// </summary>
    [Alias("Site_Banners")]
    [Schema("CMS")]
    public partial class Site_Banner
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        //[Default(0)]
        //[ForeignKey(typeof(Website), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        ///// <summary>
        ///// If ==0 then can be use for Public
        ///// </summary>
        //public int SiteId { get; set; }

        //[Ignore]
        //public string SiteName { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        /// <summary>
        /// =0: Image banner ; =1: Flash banner ; =2: youtube banner
        /// </summary>
        public int BannerType { get; set; }

        public int BannerIndex { get; set; }

        public string FileName { get; set; }

        public string LanguageCode { get; set; }

        public bool Status { get; set; }

        public int CreatedBy { get; set; }
        [Ignore]
        public string CreatedByUsername { get; set; }

        public DateTime CreatedOn { get; set; }

        public Site_Banner()
        {
            Width = 600;
            Height = 300;
        }


    }
}
