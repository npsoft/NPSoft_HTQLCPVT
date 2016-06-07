using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    /// <summary>
    /// Copyright Trung Dang 
    /// </summary>
    [Alias("Testimonial")]
    [Schema("CMS")]
    public partial class Testimonial : ModelBase
    {
        
        //[Default(0)]
        //[ForeignKey(typeof(Website), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        ///// <summary>
        ///// If ==0 then can be use for Public
        ///// </summary>
        //public int SiteId { get; set; }

        //[Ignore]
        //public string SiteName { get; set; }

        public string SeoName { get; set; }

        public string Name { get; set; }

        public string Comment { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public double Rate { get; set; }

        public DateTime SubmitOn { get; set; }

        public bool isNew { get; set; }

        public string ThumbnailFilename { get; set; }

        public string LanguageCode { get; set; }

        public Testimonial()
        {
        }

       
    }
}
