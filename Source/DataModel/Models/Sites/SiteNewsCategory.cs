using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using PhotoBookmart.DataLayer.Models.System;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    /// <summary>
    /// Copyright Trung Dang 
    /// </summary>
    [Alias("Site_News_Category")]
    [Schema("CMS")]
    public partial class Site_News_Category : ModelBase
    {
        //[ForeignKey(typeof(Website), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        //public int SiteId { get; set; }
        //[Ignore]
        //public string Site_Name { get; set; }

        public string Name { get; set; }

        public string SeoName { get; set; }

        public bool Status { get; set; }

        public string LanguageCode { get; set; }

        [Ignore]
        public int CountNewsInside { get; set; }

        public Site_News_Category()
        {
            CreatedOn = DateTime.Now;
        }

        public int New_CountInside()
        {
            try
            {
                return (int)Db.Count<Site_News>(m => m.CategoryId == this.Id);
            }
            catch
            {
                return 0;
            }
        }
    }
}