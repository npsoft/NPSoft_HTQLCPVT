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
    [Alias("Site_Lang_Dis")]
    [Schema("CMS")]
    public partial class Site_Lang_Dis
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }
        
        [Default(0)]
        [ForeignKey(typeof(Language), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public int LanguageId { get; set; }
        [Ignore]
        public string Language_Name { get; set; }

        /// <summary>
        /// Alias from Language table
        /// </summary>
        [Ignore]
        public int TextDirection { get; set; }

        public string LanguageCode { get; set; }

        [Default(0)]
        [ForeignKey(typeof(Website), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public int SiteId { get; set; }

        public bool Status { get; set; }

        public int CreatedBy { get; set; }
        [Ignore]
        public string CreatedByUserName { get; set; }

        public DateTime CreatedOn { get; set; }

        public Site_Lang_Dis()
        {
            LanguageCode = "";
            CreatedOn = DateTime.Now;
        }
    }
}
