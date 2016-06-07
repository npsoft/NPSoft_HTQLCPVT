using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.System
{
    [Alias("Languages")]
    [Schema("CMS")]
    public partial class Language : ModelBase
    {
        public string LanguageName { get; set; }

        public string LanguageCode { get; set; }

        public bool Status { get; set; }

        /// <summary>
        /// =0: Left to Right
        /// =1: Right To Left
        /// </summary>
        [Default(0)]
        public int TextDirection { get; set; }

        public Language()
        {
            CreatedOn = DateTime.Now;
            LanguageName = ""; LanguageCode = "";
        }

        public void Enable()
        {
            this.Status = true;
        }

        public void Disable()
        {
            this.Status = false;
        }
    }
}
