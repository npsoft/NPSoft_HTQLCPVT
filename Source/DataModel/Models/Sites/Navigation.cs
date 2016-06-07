using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    [Alias("NavigationMenu")]
    [Schema("CMS")]
    public partial class Navigation
    {
        public Navigation()
        {
            LanguageName = "";
            CreatedOn = DateTime.Now;
        }
    
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        [Default(typeof(string), "")]
        public string Name { get; set; }

        //[Default(0)]
        //[ForeignKey(typeof(Website), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        //public int WebsiteId { get; set; }
        //[Ignore]
        //public string Website_Name { get; set; }

        [Default(0)]
        public int OrderMenu { get; set; }

        [Default(0)]
        public long ParentId { get; set; }

        /// <summary>
        /// =0: External Link
        /// =1: Link to Topic
        /// </summary>
        [Default(0)]
        public int Menutype { get; set; }

        [Default(typeof(string), "")]
        public string Link { get; set; }

        [Default(0)]
        public long TopicId { get; set; }

        public bool Status { get; set; }

        public bool RequireLogin { get; set; }

        public bool NeedAddCulturePrefix { get; set; }

        [Default(typeof(string), "")]
        public string LanguageName { get; set; }

        public int CreatedBy { get; set; }
        [Ignore]
        public string CreatedByUsername { get; set; }

        public DateTime CreatedOn { get; set; }

    }
}
