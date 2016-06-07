using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Products
{
 /// <summary>
 /// To keep control each step foreach order
 /// </summary>
    [Schema("Products")]
    public partial class Order_ProductionJobSheet : BasicModelBase
    {
        [Default(0)]
        [ForeignKey(typeof(Order), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public long Order_Id { get; set; }

        /// <summary>
        /// The date time when staff open the ticket
        /// </summary>
        public DateTime OnDate { get; set; }

        /// <summary>
        /// Who finish this step
        /// </summary>
        public int User_Id { get; set; }

        /// <summary>
        /// Step number, compare with the Enum_OrderStatus
        /// </summary>
        public int Step { get; set; }

        public Order_ProductionJobSheet()
        {

        }
    }
}
