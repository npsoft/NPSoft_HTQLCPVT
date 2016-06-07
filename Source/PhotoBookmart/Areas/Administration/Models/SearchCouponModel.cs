using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhotoBookmart.Areas.Administration.Models
{
    public partial class SearchCouponModel
    {
        public int? Type { get; set; }

        public string Key { get; set; }

        public DateTime? IssuedOnBefore { get; set; }

        public DateTime? IssuedOnAfter { get; set; }

        public SearchCouponModel()
        {
            Type = null;

            Key = null;

            IssuedOnBefore = null;

            IssuedOnAfter = null;
        }
    }
}