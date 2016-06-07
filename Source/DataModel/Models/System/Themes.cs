using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.System
{
    [Alias("Themes")]
    [Schema("CMS")]
    public partial class Theme
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        public string ThemeName { get; set; }

        public string FolderName { get; set; }

        public bool isResponsive { get; set; }

        public bool Status { get; set; }

        public int CreatedBy { get; set; }
        /// <summary>
        /// For local use when showing on form
        /// </summary>
        [Ignore]
        public string CreatedByUsername { get; set; }

        public DateTime CreatedOn { get; set; }

        public Theme()
        {
            CreatedOn = DateTime.Now;
            ThemeName = "";
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
