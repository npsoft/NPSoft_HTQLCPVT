using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.MMO
{
    /// <summary>
    /// Store images of clients to process
    /// </summary>
    [Alias("MMO_Imgs")]
    [Schema("MMO")]
    public partial class MMO_Imgs : BasicModelBase
    {
        public string Type { get; set; }
        public string From { get; set; }
        public long TimeOut { get; set; }
        public string NameOrg { get; set; }
        public string PathFTP { get; set; }
        public string Status { get; set; }
        public string Content { get; set; }

        public DateTime? CreatedOn { get; set; }
        public long? CreatedBy { get; set; }
        public DateTime? LastModifiedOn { get; set; }
        public long? LastModifiedBy { get; set; }

        public MMO_Imgs() { }
    }
}
