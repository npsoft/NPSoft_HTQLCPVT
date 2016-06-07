using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PhotoBookmart.DataLayer.Models.Products;

namespace PhotoBookmart.Areas.Administration.Models
{
    public partial class OrderAutomatedShippingModel
    {
        public string Order_Number { get; set; }

        public Enum_ShippingType Shipping_Method { get; set; }

        public Enum_ShippingStatus Shipping_Status { get; set; }

        public string Shipping_TrackingNumber { get; set; }


        public OrderAutomatedShippingModel()
        {
            Order_Number = "";

            Shipping_Method = Enum_ShippingType.Other;

            Shipping_Status = Enum_ShippingStatus.PickedUp;
        }
    }
}