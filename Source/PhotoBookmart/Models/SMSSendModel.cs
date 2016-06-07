using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace PhotoBookmart.Models
{
    /// <summary>
    /// SMS Item in processing queue
    /// </summary>
    public class SMS_Item
    {
        public string Body { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsFlashSMS { get; set; }
        public bool IsProcessing { get; set; }
    }

    #region Request to Send SMS to SMS Server
    /// <summary>
    /// Model to send request send sms to server
    /// </summary>
    [Route("/SendSMS", "POST")]
    public class SMSServer_SMSSendModel : IReturn<SMSServer_SendSMSResponseModel>
    {
        public List<string> Receivers { get; set; }
        public string Content { get; set; }
        public bool IsFlashSMS { get; set; }
    }

    /// <summary>
    /// Model to recieve response from SMS Server after you send request.
    /// </summary>
    public class SMSServer_SendSMSResponseModel
    {
        /// <summary>
        /// Keep this ID for later query the status
        /// </summary>
        public long RequestId { get; set; }
        public ResponseStatus Status { get; set; }
        public SMSServer_SendSMSResponseModel()
        {
        }
    }
    #endregion

    #region Login & Logout on SMS Server
    [Route("/Login", "POST")]
    public class SMSServer_LoginModel : IReturn<SMSServer_LoginResponse>
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class SMSServer_LoginResponse
    {
        public ResponseStatus Status { get; set; }
        public string SecureToken { get; set; }

        public SMSServer_LoginResponse()
        {
        }
    }
    #endregion

    #region Queries SMSItem on SMS Server
    [Route("/ThirdParty_Query_SMSRequest", "POST")]
    [Api("Third Party - Query SMS Request ")]
    public class TPQuerySMSItemRequest : IReturn<TPQuerySMSItemResponse>
    {
        public int Page { get; set; }
    }

    public class TPQuerySMSItem
    {
        public int Id { get; set; }

        public DateTime CreateDate { get; set; }
        ///// <summary>
        ///// List of recever phone number
        ///// </summary>
        public List<string> Receivers { get; set; }
        /// <summary>
        /// Content of SMS
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// Return True of bulk
        /// </summary>
        public bool IsBulkSMS { get; set; }
        public bool IsFlashSMS { get; set; }
        public bool IsUnicodeSMS { get; set; }

        #region Processing Information
        /// <summary>
        /// How many fragment of this sms
        /// </summary>
        public int FragmentCount { get; set; }
        /// <summary>
        /// How many success fragment
        /// </summary>
        public int FragmentSuccess { get; set; }
        /// <summary>
        /// How many error fragment
        /// </summary>
        public int FragmentError { get; set; }

        /// <summary>
        /// How many items is sending
        /// </summary>
        public int FragmentSending { get; set; }
        #endregion

        #region Internal Data
        /// <summary>
        /// the time we start processing it
        /// </summary>
        public DateTime ProcessingOn { get; set; }
        /// <summary>
        /// The time we finish the processing
        /// </summary>
        public DateTime SentOn { get; set; }
        #endregion

    }

    public class TPQuerySMSItemResponse
    {
        public ResponseStatus Status { get; set; }

        /// <summary>
        /// How many page
        /// </summary>
        public int Pages { get; set; }
        // Current page
        public int Page { get; set; }
        /// <summary>
        /// How many items per page
        /// </summary>
        public int PageSize { get; set; }
        /// <summary>
        /// Total items
        /// </summary>
        public int Total { get; set; }

        public List<TPQuerySMSItem> Items { get; set; }

        public TPQuerySMSItemResponse()
        {
        }
    }
    #endregion

}