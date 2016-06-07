using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    public enum Enum_MaillingList_Categories
    {
        [Display(Name = "Common")]
        C,
        [Display(Name = "Order Processing - Common")]
        Order_Common,
        [Display(Name = "Order Processing - Payment")]
        Order_Payment
    }

    /// <summary>
    /// Copyright Trung Dang 
    /// </summary>
    [Alias("Site_MaillingListTemplate")]
    [Schema("CMS")]
    public partial class Site_MaillingListTemplate
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        public string Name { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        public string Systemname { get; set; }

        public bool Status { get; set; }

        public int CreatedBy { get; set; }
        [Ignore]
        public string CreatedByUsername { get; set; }

        public DateTime CreatedOn { get; set; }

        public bool IsPublic { get; set; }

        /// <summary>
        /// Is for Order Processing
        /// </summary>
        public bool IsForOrder { get; set; }

        /// <summary>
        /// Order Processing category
        /// </summary>
        public Enum_MaillingList_Categories CategoryName { get; set; }

        public Site_MaillingListTemplate()
        {
            Id = 0;
            CreatedBy = 0;
            CreatedOn = DateTime.Now;
        }
    }
}