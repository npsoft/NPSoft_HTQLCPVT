using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.DataLayer.Models.Products
{
    public enum Enum_UploadFilesTicketStatus
    {
        [Display(Name = "Waiting Upload")]
        Default = 0,

        [Display(Name = "Waiting to Decrypt")]
        Uploaded = 1,

        [Display(Name = "Decrypted Successfull")]
        DecryptedSuccess = 2,

        [Display(Name = "Decrypted Not Success")]
        DecryptedFailed = 3,

        [Display(Name = "Approved, waiting to move to Data Folder")]
        MoveToDataFolder = 4,
    }

    [Alias("Order_UploadFilesTicket")]
    [Schema("Products")]
    public class Order_UploadFilesTicket : BasicModelBase
    {
        //[PrimaryKey]
        //[AutoIncrement]
        //public long Id { get; set; }

        [Default(0)]
        [ForeignKey(typeof(Order), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public long OrderId { get; set; }

        [Default(0)]
        [ForeignKey(typeof(ABUserAuth), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public int UserId { get; set; }

        public int Status { get; set; }

        /// <summary>
        /// Original file name of the customer
        /// </summary>
        public string FileName { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime LastUpdate { get; set; }
        
        public Order_UploadFilesTicket() { }
    }
}