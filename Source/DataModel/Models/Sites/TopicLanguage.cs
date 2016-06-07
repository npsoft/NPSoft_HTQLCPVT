using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    [Alias("TopicLanguage")]
    [Schema("CMS")]
    public partial class SiteTopicLanguage : ModelBase
    {
        public SiteTopicLanguage()
        {
            IsDeleted = false;
        }
    
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        //[Default(0)]
        //public int SiteId { get; set; }

        [Default(0)]
        [ForeignKey(typeof(SiteTopic), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public long TopicId { get; set; }

        [Default(0)]
        public long LanguageId { get; set; }

        [Default(typeof(string), "")]
        public string LanguageCode { get; set; }

        public bool IsDefault { get; set; }

        [Default(typeof(string), "")]
        public string Title { get; set; }

        [Default(typeof(string), "")]
        public string Body { get; set; }

        [Default(typeof(string), "")]
        public string MetaKeywords { get; set; }

        [Default(typeof(string), "")]
        public string MetaDescription { get; set; }

        [Default(typeof(string), "")]
        public string MetaTitle { get; set; }

        [Default(0)]
        public int LayoutType { get; set; }

        [Default(typeof(string), "")]
        public string MoreLink { get; set; }

        /// <summary>
        /// Use this field only in Edit form to let the controller know you want to delete this translation
        /// </summary>
        [Ignore]
        public bool IsDeleted { get; set; }
    }
}
