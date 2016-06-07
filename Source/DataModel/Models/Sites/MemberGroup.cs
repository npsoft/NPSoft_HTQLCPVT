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
    [Alias("Site_Member_Group")]
    [Schema("CMS")]
    public partial class Site_MemberGroup
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }
        
        //[ForeignKey(typeof(Website), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        //public int SiteId { get; set; }
        //[Ignore]
        //public string Site_Name { get; set; }

        public string Name { get; set; }

        public bool Status { get; set; }

        public int CreatedBy { get; set; }
        [Ignore]
        public string CreatedByUserName { get; set; }

        public DateTime CreatedOn { get; set; }

        public Site_MemberGroup()
        {
            CreatedOn = DateTime.Now;
        }
    }
}
