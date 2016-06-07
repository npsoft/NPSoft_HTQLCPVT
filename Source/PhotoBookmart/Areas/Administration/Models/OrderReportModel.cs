using System;

namespace PhotoBookmart.Areas.Administration.Models
{
    public class OrderReportModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string PriceSign { get; set; }
        public int NumOrders { get; set; }
        public double TotalMoney { get; set; }
    }
}