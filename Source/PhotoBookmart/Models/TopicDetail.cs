using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PhotoBookmart.DataLayer.Models.Sites;

namespace PhotoBookmart.Models
{
    public class TopicDetail:SiteTopic
    {
        public SiteTopicLanguage ContextLanguageDetail { get; private set; }

        public void SetLanguageContext(SiteTopicLanguage context)
        {
            this.ContextLanguageDetail = context;
        }
    }
}