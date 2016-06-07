using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    /// <summary>
    /// Copyright Trung Dang 
    /// </summary>
    [Alias("Site_ContactUs")]
    [Schema("CMS")]
    public partial class Site_ContactUs
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        //[Default(0)]
        //[ForeignKey(typeof(Website), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        ///// <summary>
        ///// If ==0 then can be use for Public
        ///// </summary>
        //public int SiteId { get; set; }

        //[Ignore]
        //public string SiteName { get; set; }

        public string Name { get; set; }

        public string Comment { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Website { get; set; }

        /// <summary>
        /// id of logged in user who contact
        /// </summary>
        public long Contact_UserId { get; set; }

        public string Contact_IP { get; set; }

        public DateTime Contact_On { get; set; }

        public long ParentId { get; set; }

        public bool IsNew { get; set; }

        public long Reply_UserId { get; set; }
        [Ignore]
        public string Reply_Username { get; set; }
        public DateTime Reply_On { get; set; }



        public Site_ContactUs()
        {
            IsNew = true;
            Reply_On = DateTime.Now;
            Contact_On = DateTime.Now;
        }

       
    }
}
