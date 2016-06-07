using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    /// <summary>
    /// Copyright Trung Dang 
    /// </summary>
    [Alias("Site_Member_Group_Detail")]
    [Schema("CMS")]
    public partial class Site_MemberGroupDetail
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }
        
        //public int SiteId { get; set; }
        //[Ignore]
        //public string Site_Name { get; set; }

        public long UserId { get; set; }

        // All fields below we will copy from Users in Management page controller
        [Ignore]
        public string UserFullName { get; set; }
        [Ignore]
        public string Username { get; set; }
        [Ignore]
        public string UserEmail { get; set; }

        [ForeignKey(typeof(Site_MemberGroup), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public int GroupId { get; set; }

        public int CreatedBy { get; set; }
        [Ignore]
        public string CreatedByUserName { get; set; }

        public DateTime CreatedOn { get; set; }

        public Site_MemberGroupDetail()
        {
            CreatedOn = DateTime.Now;
        }
    }
}
