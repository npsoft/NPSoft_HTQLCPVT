using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    [Alias("Site_Newsletter")]
    [Schema("CMS")]
    public partial class SiteNewsletter : ModelBase
    {
        //[Default(0)]
        //public int SiteId { get; set; }
        public string LanguageCode { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Desc { get; set; }

        public bool IsActive { get; set; }

        public SiteNewsletter()
        {
            IsActive = false;

            CreatedBy = 0;

            CreatedOn = DateTime.Now;
        }
    }
}