using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.SMS
{
    /// <summary>
    /// Copyright Trung Dang 
    /// </summary>
    [Alias("SMS_Template")]
    [Schema("SMS")]
    public partial class SMSTemplateModel : ModelBase
    {
        public string Name { get; set; }

        public string SystemName { get; set; }

        public string Content { get; set; }

        public bool IsFlash { get; set; }

        public string CountryCode { get; set; }
    }
}