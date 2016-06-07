using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using IO = System.IO;
using System.ComponentModel.DataAnnotations;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.DataLayer.Models.Products
{
    public enum Enum_PaymentMethod
    {
        Other,
        Paypal,
        iPay88,
        Cheque,
        Bank_Check
    }

    public enum Enum_FlagOrderMessage
    {
        No_NewMessage = 0,
        NewMessageFromCustomer = 1,
        NewMessageFromPhotobookmart = 2
    }

    public enum Enum_PaymentCardType
    {
        Other,
        Unionpay,
        MasterCard,
        Visa,
        Electron,
        JCB,
        Amex,
        Discover,
        Dinners
    }

    public enum Enum_ShippingType
    {
        Other,
        DHL,
        Aramex,
        TNT
    }

    public enum Enum_ShippingStatus
    {
        [Display(Name = "Not Yet Shipped")]
        Waiting,
        [Display(Name = "Picked Up")]
        PickedUp,
        [Display(Name = "Shipped")]
        Shipped,

    }

    /// <summary>
    /// Represents a payment status enumeration
    /// </summary>
    public enum Enum_PaymentStatus : int
    {
        /// <summary>
        /// Pending
        /// </summary>
        Pending = 0,
        /// <summary>
        /// Authorized
        /// </summary>
        Authorized = 20,
        /// <summary>
        /// Paid
        /// </summary>
        Paid = 30,
        /// <summary>
        /// Partially Refunded
        /// </summary>
        PartiallyRefunded = 35,
        /// <summary>
        /// Refunded
        /// </summary>
        Refunded = 40,
        /// <summary>
        /// Voided
        /// </summary>
        Voided = 50,
    }

    public enum Enum_OrderStatus
    {
        [Display(Name = "Canceled")]
        Canceled = -1,

        [Display(Name = "Refund")]
        Refund = -2,

        /// <summary>
        /// Step 1, just receive order
        /// </summary>
        [Display(Name = "Step 1 - Order just received")]
        Received = 1,

        /// <summary>
        /// Step 2
        /// </summary>
        [Display(Name = "Step 2 - File verify")]
        Verify = 2,

        /// <summary>
        /// Step 3
        /// </summary>
        [Display(Name = "Step 3 - File processing")]
        Processing = 3,

        /// <summary>
        /// Step 4
        /// </summary>
        [Display(Name = "Step 4 - QC after file processing")]
        QC_AfterFileProcessing = 4,

        /// <summary>
        /// Step 5
        /// </summary>
        [Display(Name = "Step 5 - Printing")]
        Printing = 5,

        /// <summary>
        /// Step 6
        /// </summary>
        [Display(Name = "Step 6 - QC, Printing and prepare Job Sheets")]
        QC_AfterFilePriting = 6,

        /// <summary>
        /// Step 7
        /// </summary>
        [Display(Name = "Step 7 - Production")]
        Production = 7,

        /// <summary>
        /// Step 8
        /// </summary>
        [Display(Name = "Step 8 - QC for the books and packing. Shipping")]
        Shipping = 8,

        /// <summary>
        /// Step 9
        /// </summary>
        [Display(Name = "Step 9 - Finish order")]
        Finished = 9,

        ///// <summary>
        ///// Step 10
        ///// </summary>
        //[Display(Name = "Step 10 - Order Closed")]
        //Closed = 10
    }

    [Schema("Products")]
    public partial class Order : BasicModelBase
    {
        /// <summary>
        /// 5 digits
        /// </summary>
        [Display(Name = "Order number (6 digits)")]
        public string Order_Number { get; set; }

        [Display(Name = "Customer Id (integer)")]
        public long Customer_Id { get; set; }
        /// <summary>
        /// Just in case, hehe
        /// </summary>
        [Display(Name = "Customer username")]
        public string Customer_Username { get; set; }

        [Display(Name = "Customer name")]
        public string Customer_Name { get; set; }

        [Display(Name = "Customer email address")]
        public string Customer_Email { get; set; }

        [Display(Name = "Date when the order created")]
        public DateTime CreatedOn { get; set; }

        [Display(Name = "Paid date - the date this order has been marked as paid")]
        public DateTime PaidDate { get; set; }

        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [Display(Name = "Order sub total amount - included shipping fee")]
        public double Bill_SubTotal { get; set; }
        /// <summary>
        /// Grand total without gst, without rounding
        /// </summary>
        [Display(Name = "Order grand total amount - Sub total * Quantity")]
        public double Bill_GrandTotal { get; set; }

        [Display(Name = "Order GST, 6% of the grand total")]
        public double Bill_GST { get; set; }

        /// <summary>
        /// Final total, with rounding
        /// </summary>
        [Display(Name = "Order total")]
        public double Bill_Total { get; set; }

        [Display(Name = "Order status (integer)")]
        public int Status { get; set; }

        [Ignore]
        [IncludeWhenGenerateList]
        public Enum_OrderStatus StatusEnum
        {
            get
            {
                return (Enum_OrderStatus)Status;
            }
            set
            {
                Status = (int)value;
            }
        }

        public DateTime LastUpdate { get; set; }

        /// <summary>
        /// To be used when show the order list in Backend. To determine what is the status of the file corrupt upload token
        /// </summary>
        [Ignore]
        public string UploadFilesTicketStatus { get; set; }

        #region Product
        /// <summary>
        /// String code generated by the app My Photo Creation, we use this field to file the files in FTP
        /// </summary>
        [Display(Name = "AppCode - The code from My Photo Creation App")]
        public string AppCode { set; get; }

        [Display(Name = "Product Id")]
        public long Product_Id { get; set; }

        /// <summary>
        /// Copy from table product, for easy searching
        /// </summary>
        [Display(Name = "Product name")]
        public string Product_Name { get; set; }

        [Display(Name = "Product price, in RM")]
        public double Product_Price { get; set; }

        /// <summary>
        /// Point to Material detail
        /// </summary>
        [IgnoreWhenGenerateList]
        public int CoverMaterial { get; set; }
        #endregion

        #region Dimensions
        /// <summary>
        /// Total product weight + option weight
        /// </summary>
        public double TotalWeight { get; set; }
        #endregion

        #region Coupon
        /// <summary>
        /// If use coupon, then set to true
        /// </summary>
        [Display(Name = "Is Use Coupon?")]
        public bool isUseCoupon { get; set; }
        /// <summary>
        /// Coupon code
        /// </summary>
        [Display(Name = "Coupon Code")]
        public string Coupon_Code { get; set; }

        /// <summary>
        /// Coupon type, same as Enum_CouponType
        /// </summary>
        [IgnoreWhenGenerateList]
        public int CouponType { get; set; }

        /// <summary>
        /// Security code
        /// </summary>
        [Display(Name = "Cooupon SecurityCode")]
        public string Coupon_SecrectCode { get; set; }

        /// <summary>
        /// Total discount when we apply the coupon
        /// </summary>
        [Display(Name = "Coupon discount amount, in default currency")]
        public double Coupon_TotalDiscount { get; set; }

        /// <summary>
        /// The discount amount we will show to customer, with currency well formated
        /// </summary>
        public string Coupon_DiscountDisplay { get; set; }
        #endregion

        #region Payment
        [Display(Name = "Is this order paid?")]
        public bool Payment_isPaid { get; set; }

        [IgnoreWhenGenerateList]
        public int PaymentStatus { get; set; }

        [Ignore]
        [IncludeWhenGenerateList]
        [Display(Name = "Payment status")]
        public Enum_PaymentStatus PaymentStatusEnum
        {
            get
            {
                return (Enum_PaymentStatus)PaymentStatus;
            }
            set
            {
                PaymentStatus = (int)value;
            }
        }

        [Display(Name = "Payment method")]
        public Enum_PaymentMethod PaymentMethod { get; set; }

        [Display(Name = "Payment cheque or check number")]
        public string Payment_ChequeCheckNumber { get; set; }

        [Display(Name = "Payment - card type")]
        public Enum_PaymentCardType Payment_CardType { get; set; }

        [IgnoreWhenGenerateListAttribute]
        public string Payment_CardName { get; set; }

        [IgnoreWhenGenerateListAttribute]
        public string Payment_CardNumber { get; set; }

        [IgnoreWhenGenerateListAttribute]
        public string Payment_MaskedCreditCardNumber { get; set; }

        [IgnoreWhenGenerateListAttribute]
        public string Payment_CardCvv2 { get; set; }

        [IgnoreWhenGenerateListAttribute]
        public int Payment_CardExpirationMonth { get; set; }

        [IgnoreWhenGenerateListAttribute]
        public int Payment_CardExpirationYear { get; set; }

        [IgnoreWhenGenerateListAttribute]
        public string Payment_AuthorizationTransactionId { get; set; }

        [IgnoreWhenGenerateListAttribute]
        public string Payment_AuthorizationTransactionCode { get; set; }

        [IgnoreWhenGenerateListAttribute]
        public string Payment_AuthorizationTransactionResult { get; set; }

        [IgnoreWhenGenerateListAttribute]
        public string Payment_CaptureTransactionId { get; set; }

        [IgnoreWhenGenerateListAttribute]
        public string Payment_CaptureTransactionResult { get; set; }

        [IgnoreWhenGenerateListAttribute]
        public string Payment_SubscriptionTransactionId { get; set; }

        /// <summary>
        /// Link to table Address
        /// </summary>
        [IgnoreWhenGenerateList]
        public long Payment_BillingAddress { get; set; }

        [Display(Name = "Shipping address is same with billing address")]
        public bool Payment_BillingAddress_SameWith_Shipping { get; set; }
        #endregion

        #region Shipping
        /// <summary>
        /// Real shipping price in RM
        /// </summary>
        [Display(Name = "Shipping price, in RM")]
        public double Shipping_RealPrice { get; set; }
        /// <summary>
        /// Display price in billing / shipping country currency
        /// </summary>
        [Display(Name = "Shipping display price, maybe in other currency")]
        public double Shipping_DisplayPrice { get; set; }
        /// <summary>
        /// Shipping price currency sign
        /// </summary>
        [Display(Name = "Shipping display price currency sign")]
        public string Shipping_DisplayPriceSign { get; set; }

        [Display(Name = "Shipping Method")]
        public Enum_ShippingType Shipping_Method { get; set; }

        [Display(Name = "Date this order has been picked by the shipping company")]
        public DateTime Shipping_ShipOn { get; set; }

        [Display(Name = "Shipping tracking number")]
        public string Shipping_TrackingNumber { get; set; }

        [Display(Name = "Shipping Status")]
        public Enum_ShippingStatus Shipping_Status { get; set; }

        /// <summary>
        /// Link to table Address
        /// </summary>
        [IgnoreWhenGenerateList]
        public long ShippingAddress { get; set; }

        [Display(Name = "Shipping Note")]
        public string ShippingNote { get; set; }
        #endregion

        #region Staff Process Controlling
        /// <summary>
        /// Id of the staff user who is working on this order
        /// If =0: No one working on this order
        /// </summary>
        [Default(0)]
        [IgnoreWhenGenerateList]
        public int WorkingStaff_Id { get; set; }
        /// <summary>
        /// Date and time when the staff user click into this order to work on it
        /// </summary>
        [IgnoreWhenGenerateList]
        public DateTime WorkingStaff_On { get; set; }

        [Ignore]
        public string WorkingStaff_Name { get; set; }

        /// <summary>
        /// =0: No new message
        /// =1: Have new message from customer
        /// =2: Have new message from Photobookmart (staff reply OR system)
        /// </summary>
        [Default(0)]
        public int FlagHistoryMessage { get; set; }

        /// <summary>
        /// True if order photobook has been deleted. False if not.
        /// </summary>
        public bool Order_Photobook_Deleted { get; set; }
        #endregion

        public Order()
        {
            PaymentStatus = (int)Enum_PaymentStatus.Pending;
            Shipping_Method = Enum_ShippingType.Other;
            PaymentMethod = Enum_PaymentMethod.Other;
            Payment_CardType = Enum_PaymentCardType.Other;
        }

        public void Status_NextValue()
        {
            if (Status < 9)
            {
                Status++;
            }
        }

        public void Status_PreviousValue()
        {
            if (Status > 1)
            {
                Status--;
            }
        }

        #region For Address Helper
        /* For Internal code using */
        /// <summary>
        /// Billing Address Model, to help easy show the invoice, ignore
        /// </summary>
        [Ignore]
        public AddressModel BillingAddressModel { get; private set; }

        [Ignore]
        public AddressModel ShippingAddressModel { get; private set; }

        /// <summary>
        /// Load the address model into this model
        /// </summary>
        /// <param name="type">=0: Billing, =1: Shipping</param>
        /// <returns></returns>
        public void LoadAddress(int type)
        {
            //if (type == 1 && Payment_BillingAddress_SameWith_Shipping && BillingAddressModel.Id > 0)
            //{
            //    ShippingAddressModel = BillingAddressModel;
            //    return;
            //}
            AddressModel model = new AddressModel();
            long id = Payment_BillingAddress;
            if (type == 1)
            {
                id = ShippingAddress;
            }
            model = Db.Select<AddressModel>(x => x.Where(m => m.Id == id).Limit(1)).FirstOrDefault();
            if (model == null)
            {
                model = new AddressModel();
            }
            if (type == 0)
            {
                BillingAddressModel = model;
            }
            else
            {
                ShippingAddressModel = model;
            }
            Db.Close();
        }
        #endregion

        #region for Product Helper
        /// <summary>
        /// Get Product Model of the Product Id, for internal use, ignore
        /// </summary>
        [Ignore]
        public Product ProductModel { get; private set; }

        /// <summary>
        /// Get the list of all options in product
        /// </summary>
        [Ignore]
        public List<Order_ProductOptionUsing> Product_OptionsModel { get; private set; }

        public void LoadProductInfo()
        {
            ProductModel = Db.Select<Product>(x => x.Where(m => m.Id == Product_Id).Limit(1)).FirstOrDefault();
            if (ProductModel == null)
            {
                ProductModel = new Product();
                Product_OptionsModel = new List<Order_ProductOptionUsing>();
            }
            Product_OptionsModel = Db.Where<Order_ProductOptionUsing>(x => x.Order_Id == Id);
            Db.Close();
        }

        #endregion

        #region Order History
        /// <summary>
        /// Insert Order History
        /// </summary>
        /// <param name="content"></param>
        /// <param name="username"></param>
        public void AddHistory(string content, string name, int user_id, bool isPrivate = false)
        {
            Order_History h = new Order_History();
            h.Content = content;
            h.OnDate = DateTime.Now;
            h.Order_Id = Id;
            h.UserName = name;
            h.UserId = user_id;
            h.isPrivate = isPrivate;
            Db.Insert<Order_History>(h);
            Db.Close();
        }


        /// <summary>
        /// Assign a staff to work on this order
        /// </summary>
        /// <param name="user_id"></param>
        public void AssignStaffWorking(int user_id, string name)
        {
            if (WorkingStaff_Id == 0)
            {
                AddHistory(string.Format("Staff {0} has been assigned to this order. Current step is: {1}", name, StatusEnum), "System", 0, true);
                Db.UpdateOnly<Order>(new Order()
                {
                    WorkingStaff_Id = user_id,
                    WorkingStaff_On = DateTime.Now
                }, ev => ev.Update(p => new
                {
                    p.WorkingStaff_Id,
                    p.WorkingStaff_On
                }).Where(m => m.Id == Id).Limit(1));
            }
            else
            {
                AddHistory(string.Format("Can not assign staff {0} to work on this order. Current step is: {1}", name, StatusEnum), "System", 0, true);
            }
            Db.Close();
        }

        /// <summary>
        /// Update last update of this order
        /// </summary>
        public void Update_LastUpdate()
        {
            Db.UpdateOnly<Order>(new Order() { LastUpdate = DateTime.Now }, ev => ev.Update(p => p.LastUpdate).Where(m => m.Id == Id).Limit(1));
            Db.Close();
        }
        #endregion

        #region Payment

        public void Set_PaymentStatus(Enum_PaymentStatus status, string username = "")
        {
            this.PaymentStatus = (int)status;
            if (string.IsNullOrEmpty(username))
            {
                username = "System";
            }

            AddHistory("Order payment status is changed to " + status.ToString(), "System", 0);

            if (status == Enum_PaymentStatus.Paid)
            {
                Payment_isPaid = true;
                PaidDate = DateTime.Now;
            }
            else
            {
                Payment_isPaid = false;
            }

            // When update payment status, update LastUpdate field
            Db.UpdateOnly<Order>(new Order() { PaymentStatus = PaymentStatus, LastUpdate = DateTime.Now, PaidDate = PaidDate, Payment_isPaid = Payment_isPaid },
                ev => ev.Update(p => new
                {
                    p.PaymentStatus,
                    p.LastUpdate,
                    p.PaidDate,
                    p.Payment_isPaid
                }).Where(m => m.Id == Id));

            // try to move photobook directory 
            if (PaymentStatusEnum == Enum_PaymentStatus.Paid && _PhotoBook_ProcessPhotobookFolder())
            {
                AddHistory("Moved " + this.Order_Number + " to Data Directory", "System", 0, true);
            }
            else if (PaymentStatusEnum != Enum_PaymentStatus.Paid && _PhotoBook_ProcessPhotobookFolder_Undo())
            {
                AddHistory("Undo Moved " + this.Order_Number + " to Data Directory", "System", 0, true);
            }

            //if (status == Enum_PaymentStatus.Paid && isUseCoupon)
            //{
            //    // we dont update the coupon again because in ProductController, SubmitOrder, we already increased the coupon Used
            //}

            Db.Close();
        }
        #endregion

        #region Country
        /// <summary>
        /// Get country of this order
        /// </summary>
        /// <returns></returns>
        public Country GetCountry()
        {
            var c = Db.Select<Country>(x => x.Where(m => m.CurrencyCode == Shipping_DisplayPriceSign).Limit(1)).FirstOrDefault();
            Db.Close();
            return c;
        }
        #endregion

        #region Customer User
        /// <summary>
        /// Get user account of this order
        /// </summary>
        /// <returns></returns>
        public ABUserAuth Get_CustomerUserAccount()
        {
            var u = Db.Select<ABUserAuth>(x => x.Where(m => m.Id == Customer_Id).Limit(1)).FirstOrDefault();
            Db.Close();
            return u;
        }

        /// <summary>
        /// Copy photobook folder from WEBSITE_ORDERS_FOLDER_NOTYETPAID_PATH to WEBSITE_UPLOAD_PATH_DEFAULT when the order is PAID
        /// </summary>
        /// <param name="photobook_code"></param>
        /// <param name="rename"></param>
        /// <param name="new_name"></param>
        /// <returns></returns>
        bool _PhotoBook_ProcessPhotobookFolder()
        {
            var path = Settings.Get(Enum_Settings_Key.WEBSITE_ORDERS_FOLDER_NOTYETPAID_PATH, IO.Path.GetTempPath(), Enum_Settings_DataType.String).ToString();
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }
            try
            {
                if (IO.Directory.Exists(path + this.Order_Number))
                {
                    // in old system we rename, but in new sytem we move it to WEBSITE_ORDERS_FOLDER_NOTYETPAID_PATH
                    var path_copy_to = Settings.Get(Enum_Settings_Key.WEBSITE_UPLOAD_PATH_DEFAULT, IO.Path.GetTempPath(), Enum_Settings_DataType.String).ToString();
                    if (!path_copy_to.EndsWith("\\"))
                    {
                        path_copy_to += "\\";
                    }
                    path_copy_to += this.Order_Number;

                    // create new folder first
                    if (!IO.Directory.Exists(path_copy_to))
                    {
                        IO.Directory.CreateDirectory(path_copy_to);
                    }

                    var dir_info = new IO.DirectoryInfo(path + this.Order_Number);
                    foreach (var x in dir_info.GetFiles())
                    {
                        x.CopyTo(IO.Path.Combine(path_copy_to, x.Name));
                    }

                    // everything ok? then we delete this folder
                    IO.Directory.Delete(path + this.Order_Number, true);
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Copy photobook folder from WEBSITE_UPLOAD_PATH_DEFAULT to WEBSITE_ORDERS_FOLDER_NOTYETPAID_PATH when the order is PAID > !PAID
        /// </summary>
        /// <param name="photobook_code"></param>
        /// <param name="rename"></param>
        /// <param name="new_name"></param>
        /// <returns></returns>
        bool _PhotoBook_ProcessPhotobookFolder_Undo()
        {
            var path = Settings.Get(Enum_Settings_Key.WEBSITE_UPLOAD_PATH_DEFAULT, IO.Path.GetTempPath(), Enum_Settings_DataType.String).ToString();
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }
            try
            {
                if (IO.Directory.Exists(path + this.Order_Number))
                {
                    // in old system we rename, but in new sytem we move it to WEBSITE_UPLOAD_PATH_DEFAULT
                    var path_copy_to = Settings.Get(Enum_Settings_Key.WEBSITE_ORDERS_FOLDER_NOTYETPAID_PATH, IO.Path.GetTempPath(), Enum_Settings_DataType.String).ToString();
                    if (!path_copy_to.EndsWith("\\"))
                    {
                        path_copy_to += "\\";
                    }
                    path_copy_to += this.Order_Number;

                    // create new folder first
                    if (!IO.Directory.Exists(path_copy_to))
                    {
                        IO.Directory.CreateDirectory(path_copy_to);
                    }

                    var dir_info = new IO.DirectoryInfo(path + this.Order_Number);
                    foreach (var x in dir_info.GetFiles())
                    {
                        x.CopyTo(IO.Path.Combine(path_copy_to, x.Name));
                    }

                    // everything ok? then we delete this folder
                    IO.Directory.Delete(path + this.Order_Number, true);
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
        #endregion
    }
}