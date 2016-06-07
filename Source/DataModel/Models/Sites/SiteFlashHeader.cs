using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    /// <summary>
    /// Copyright Trung Dang 
    /// </summary>
    [Alias("Site_FlashHeader")]
    [Schema("CMS")]
    public partial class Site_FlashHeader
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        public string Content { get; set; }

        public string LinkTo { get; set; }

        public bool Status { get; set; }

        public string LanguageCode { get; set; }

        public int BannerIndex { get; set; }

        public int CreatedBy { get; set; }
        [Ignore]
        public string CreatedByUsername { get; set; }

        public DateTime CreatedOn { get; set; }

        public Site_FlashHeader()
        {

        }
    }
}
