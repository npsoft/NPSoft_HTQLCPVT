using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Products
{
 /// <summary>
 /// To keep the communication beween customer and the staff
 /// </summary>
    [Schema("Products")]
    public partial class Order_History : BasicModelBase
    {
        [Default(0)]
        [ForeignKey(typeof(Order), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public long Order_Id { get; set; }

        public DateTime OnDate { get; set; }

        /// <summary>
        /// Name of the poster
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// If system, then put 0
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Direction left or right when showing on the timeline
        /// </summary>
        [Ignore]
        public string Direction { get; set; }

        [Ignore]
        public string UserAvatar { get; set; }

        [Ignore]
        public string OnDateFormat { get; set; }

        public string Content { get; set; }

        public bool isPrivate { get; set; }

        /// <summary>
        /// True if this history is commented by customer. False if not
        /// </summary>
        public bool IsCustomer { get; set; }

        public Order_History()
        {

        }
    }
}
