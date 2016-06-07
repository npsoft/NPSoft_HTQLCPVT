using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Products
{
    /// <summary>
    /// To be linked  with Order table 
    /// </summary>
    [Schema("Products")]
    public partial class Order_ProductOptionUsing : BasicModelBase
    {
        [Default(0)]
        [ForeignKey(typeof(Order), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public long Order_Id { get; set; }

        public long Option_Id { get; set; }

        public int Option_Quantity { get; set; }

        public string Option_Name { get; set; }

        /// <summary>
        /// Like pages, piece,...
        /// </summary>
        public string Unit_Name { get; set; }

        /// <summary>
        /// Price in RM
        /// </summary>
        public double Price { get; set; }
        /// <summary>
        /// Display price
        /// </summary>
        public double PriceDisplay { get; set; }
        /// <summary>
        /// The currency display sign, by knowing this sign, we can query back the country and get the 3 letters of currency for Paypal
        /// </summary>
        public string PriceDisplaySign { get; set; }
        
        public Order_ProductOptionUsing()
        {

        }

    }
}
