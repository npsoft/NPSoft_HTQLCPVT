using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    public enum EnumSiteSocial
    {
        Manual = 0,
        Facebook = 1,
        Youtube = 2,
        GooglePus = 3,
        Instagram = 4,
        Twitter = 5,
        Pinterest = 6,

    }

    /// <summary>
    /// Copyright Trung Dang 
    /// </summary>
    [Alias("SocialAccount")]
    [Schema("CMS")]
    public partial class SocialAccount : ModelBase
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        //[Default(0)]
        //[ForeignKey(typeof(Website), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        ///// <summary>
        ///// If ==0 then can be use for Public
        ///// </summary>
        //public int SiteId { get; set; }

        //[Ignore]
        //public string SiteName { get; set; }

        public bool Status { get; set; }

        public int ServiceType { get; set; }

        [Ignore]
        public EnumSiteSocial ServiceTypeEnum
        {
            get
            {
                return (EnumSiteSocial)ServiceType;
            }
            set
            {
                ServiceType = (int)value;
            }
        }

        public string URL { get; set; }

        public string AccountId { get; set; }

        public string AccountPassword { get; set; }

        public SocialAccount()
        {
            CreatedOn = DateTime.Now;
            Status = true;
        }

        public List<EnumSiteSocial> ListServiceType()
        {
            return new List<EnumSiteSocial>() { EnumSiteSocial.Manual, EnumSiteSocial.Facebook, EnumSiteSocial.GooglePus, EnumSiteSocial.Instagram, EnumSiteSocial.Pinterest, EnumSiteSocial.Twitter, EnumSiteSocial.Youtube };
        }
    }
}
