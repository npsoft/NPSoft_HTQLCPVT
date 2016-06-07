using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    /// <summary>
    /// Copyright Trung Dang 
    /// </summary>
    [Alias("Site_News")]
    [Schema("CMS")]
    public partial class Site_News : ModelBase
    {
        //[Default(0)]
        //public int SiteId { get; set; }

        //[Ignore]
        //public string Site_Name { get; set; }

        public string Name { get; set; }

        public bool IsActive { get; set; }

        [Default(0)]
        [ForeignKey(typeof(Site_News_Category), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public long CategoryId { get; set; }

        [Ignore]
        public string Category_Name { get; set; }

        [Ignore]
        public string Category_SeoName { get; set; }

        public bool isNew { get; set; }

        public string ShortIntro { get; set; }

        public List<string> Tag { get; set; }

        public string ThumbnailFile { get; set; }

        public bool PublishSchedule { get; set; }

        public DateTime PublishOn { get; set; }

        public DateTime UnPublishOn { get; set; }

        public string LanguageCode { get; set; }

        public string Description { get; set; }

        public string SeoName { get; set; }

        [Default(0)]
        public int Statistic_Views { get; set; }

        public Site_News()
        {
            PublishOn = DateTime.Now;
            UnPublishOn = DateTime.Now.AddMonths(12);
        }

        /// <summary>
        /// Return list of categories this news belong to 
        /// </summary>
        /// <returns></returns>
        public Site_News_Category Categories()
        {
            return Db.IdOrDefault<Site_News_Category>(this.CategoryId);
        }

       
    }
}
