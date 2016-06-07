using System;
using System.Collections.Generic;
using System.Linq;

namespace PhotoBookmart.Areas.Administration.Models
{
    public class BaoCao_DSChiTraTroCapModel
    {
        public int Thang { get; set; }
        public int Nam { get; set; }
        public List<string> LoaiDTs { get; set; }
        public List<string> Villages { get; set; }
        public string Action { get; set; }

        public BaoCao_DSChiTraTroCapModel() { }
    }

    public class OrderReportFilterModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? MaxRows { get; set; }
    }
}
