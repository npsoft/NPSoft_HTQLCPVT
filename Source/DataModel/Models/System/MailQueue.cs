using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using PhotoBookmart.DataLayer.Models.Sites;

namespace PhotoBookmart.DataLayer.Models.System
{
    [Alias("MailQueue")]
    [Schema("CMS")]
    public partial class MailQueue
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        public string Sender { get; set; }

        public string Receiver { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        /// <summary>
        /// ==0: Waiting
        /// ==1: Processing
        /// == 2: Sent
        /// </summary>
        public int Status { get; set; }

        public int QueueOn { get; set; }

        public int SentOn { get; set; }

        public MailQueue()
        {

        }
    }
}
