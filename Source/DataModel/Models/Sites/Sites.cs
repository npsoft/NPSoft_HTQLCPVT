using System;
using System.Linq;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using PhotoBookmart.DataLayer.Models.System;


namespace PhotoBookmart.DataLayer.Models.Sites
{
    /// <summary>
    /// Copyright Trung Dang 
    /// </summary>
    [Alias("Website")]
    [Schema("CMS")]
    public partial class Website : ModelBase
    {
        public string Name { get; set; }

        /// <summary>
        /// =0: Waiting approve ; =1: Normal website ; =2: On hold website
        /// </summary>
        public int Status { get; set; }

        public string Theme { get; set; }

        /// <summary>
        /// Domain for this site
        /// </summary>
        public List<string> Domain { get; set; }

        public bool UseSSL { get; set; }

        public string SiteTitle { get; set; }

        public string SiteDefaultKeyword { get; set; }

        public string SiteDefaultMeta { get; set; }

        public string Email_Admin { get; set; }

        public string Email_Support { get; set; }

        public string Email_Contact { get; set; }

        public string PhoneNumber_HotLine { get; set; }

        public string ShareThis_PublisherKey { get; set; }

        public Website()
        {
            UseSSL = false;
            Domain = new List<string>();
            CreatedOn = DateTime.Now;
            SiteTitle = "";
            SiteDefaultKeyword = "";
            SiteDefaultMeta = "";
            Theme = "";
            Email_Admin = "";
            Email_Contact = "";
            Email_Support = "";
        }

        /// <summary>
        /// Return true if input domain is in list of this site domain
        /// </summary>
        /// <param name="domain_name"></param>
        /// <returns></returns>
        public bool IsValidDomain(string domain_name)
        {
            return this.Domain.Exists(m => m.ToLower() == domain_name);
        }

        public List<SocialAccount> Social_Account()
        {
            if (this.Id > 0 && Db != null)
            {
                return Db.Select<SocialAccount>();
            }
            else
            {
                return new List<SocialAccount>();
            }
        }

        public Site_ContactusConfig Contactus_Info(string language_code)
        {
            if (this.Id > 0 && Db != null)
            {
                var siteContactUsConfig = Db.Where<Site_ContactusConfig>(m => m.LanguageCode == language_code).FirstOrDefault();

                Db.Close();

                return siteContactUsConfig;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get list of languages available for this site
        /// </summary>
        /// <returns></returns>
        public List<Language> Languages()
        {
            //JoinSqlBuilder<Language, Language> jn = new JoinSqlBuilder<Language, Language>();
            //jn = jn.Join<Language, Site_Lang_Dis>(m => m.Id, k => k.LanguageId);
            //jn.Where<Site_Lang_Dis>(m => m.SiteId == this.Id);
            //var sql = jn.ToSql();
            //return Db.Select<Language>(sql);
            return Db.Where<Language>(m=>m.Status);
        }
    }
}
