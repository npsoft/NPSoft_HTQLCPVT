using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Reports
{
    [Alias("DanhMuc_DanToc")]
    [Schema("DanhMuc")]
    public partial class DanhMuc_DanToc
    {
        [PrimaryKey]
        [AutoIncrement]
        [IgnoreWhenGenerateList]
        [Alias("MaDanToc")]
        public long Id { get; set; }
        public string TenDanToc { get; set; }

        public DanhMuc_DanToc() { }
    }

    /// <summary>
    /// User Activities with order processing, group by day
    /// </summary>
    [Schema("Reports")]
    public partial class StaffActivity : BasicModelBase
    {
        /// <summary>
        /// Id of user
        /// </summary>
        public int User_Id { get; set; }

        /// <summary>
        /// the Date only to group
        /// </summary>
        public DateTime Day { get; set; }

        /// <summary>
        /// Count of all actions of user when do the order processing
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        /// Sum of times this staff spend to work with each order, in minutes
        /// </summary>
        public long Sum { get; set; }
    }
}
