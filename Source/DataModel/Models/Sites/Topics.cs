using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    [Alias("SiteTopic")]
    [Schema("CMS")]
    public partial class SiteTopic
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        //[Default(0)]
        //[ForeignKey(typeof(Website), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        //public int SiteId { get; set; }

        public string SystemName { get; set; }

        public string Name { get; set; }

        public int CreatedBy { get; set; }
        [Ignore]
        public string CreatedByUsername { get; set; }

        public DateTime CreatedOn { get; set; }

        public bool CanMapToMenu { get; set; }

        public SiteTopic()
        {
        }
    }
}
