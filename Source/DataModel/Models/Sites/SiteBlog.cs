using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    [Alias("Site_Blog")]
    [Schema("CMS")]
    public partial class Site_Blog : ModelBase
    {
        //public int SiteId { get; set; }

        public string LanguageCode { get; set; }

        public bool IsActive { get; set; }

        [ForeignKey(typeof(Site_Blog_Category), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public long CategoryId { get; set; }

        public string Name { get; set; }

        public string SeoName { get; set; }

        public string ShortIntro { get; set; }

        public string Desc { get; set; }

        public int Order { get; set; }

        public string ThumbnailFile { get; set; }

        public List<string> Tag { get; set; }

        public bool IsNew { get; set; }

        public bool PublishSchedule { get; set; }

        public DateTime PublishOn { get; set; }

        public DateTime UnPublishOn { get; set; }

        public int StatisticViews { get; set; }

        public Site_Blog()
        {
            Id = 0;
            CreatedBy = 0;
            CreatedOn = DateTime.Now;
            IsActive = true;
            CategoryId = 0;
            IsNew = true;
            PublishSchedule = false;
            PublishOn = DateTime.Now;
            UnPublishOn = DateTime.Now.AddMonths(12);
            StatisticViews = 0;
        }

        [Ignore]
        public string CategoryName { get; set; }

        public ABUserAuth Get_User_Created()
        {
            return Db.GetById<ABUserAuth>(CreatedBy);
        }
    }
}