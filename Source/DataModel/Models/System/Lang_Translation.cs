using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using PhotoBookmart.DataLayer.Models.Sites;

namespace PhotoBookmart.DataLayer.Models.System
{
    /// <summary>
    /// Copyright Trung Dang 
    /// July 9, 2014: update: there is no SiteId anymore
    /// LangId = 0 && SiteId == 0 : Common Default Translation, for everyone can use. Use this translation incase you have no other choice
    /// LangId > 0 && SiteId == 0 : Common Translation for specific language, for everyone can use
    /// LangId = 0 && SiteId > 0: Common Translation for specific site, use this translation in case you have no other choice.
    /// LangId > 0 && SiteId > 0: Translation for Specific site and language
    /// </summary>
    [Alias("Language_Translation")]
    [Schema("CMS")]
    public partial class Language_Translation
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        [Default(0)]
        //[ForeignKey(typeof(Language), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public long LangId { get; set; }
        [Ignore]
        public string Language_Name { get; set; }

        //[Default(0)]
        //[ForeignKey(typeof(Website), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        //public int SiteId { get; set; }
        //[Ignore]
        //public string Site_Name { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public Language_Translation()
        {
            LangId = 0;
            Key = "";
            Value = "";
        }
    }
}
