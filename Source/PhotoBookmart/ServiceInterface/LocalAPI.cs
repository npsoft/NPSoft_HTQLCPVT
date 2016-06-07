using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using PhotoBookmart.ServiceInterface.RequestModel;
using ServiceStack.ServiceInterface.Auth;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.Sites;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.Common.Helpers;


namespace PhotoBookmart.ServiceInterface
{
    public class LocalAPI : Service, IDisposable
    {
        public IUserAuthRepository UserAuthRepo { get; set; }
        public AuthService AuthService { get; set; }

        public override void Dispose()
        {
            try
            {
                Db.Close();
                base.Db.Close();
            }
            catch
            {
            }
        }

        public ABUserAuth CurrentUser
        {
            get
            {
                var session = this.GetSession();
                if (session.IsAuthenticated)
                {
                    int user_id = int.Parse(session.UserAuthId);
                    var x = Db.Where<ABUserAuth>(m => m.Id == user_id).ToList();
                    return x.FirstOrDefault();
                }
                else
                    return null;
            }
        }

        public int CurrentUserId
        {
            get
            {
                var session = this.GetSession();
                if (session.IsAuthenticated)
                {
                    var existingUser = UserAuthRepo.GetUserAuth(session, null);
                    if (existingUser != null)
                        return existingUser.Id;
                    else
                        return 0;
                }
                else
                {
                    return 0;
                }
            }
        }

        public string CurrentUserIP
        {
            get
            {

                //var ipaddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                //if (ipaddress == "" || ipaddress == null)
                //    ipaddress = Request.ServerVariables["REMOTE_ADDR"];
                try
                {
                    if (Request.UserHostAddress == "127.0.0.1")
                        return "localhost";
                    else return Request.UserHostAddress;
                }
                catch
                {
                    return "127.0.0.1";
                }


            }
        }

        public string SessionId
        {
            get
            {
                string ret = "";
                try
                {
                    ret = this.GetSessionId();
                }
                catch
                {
                    ret = "";
                }
                return ret;
            }
        }

        /// <summary>
        /// Return Default Prefix of Session Name
        /// </summary>
        public string SessionNamePrefix
        {
            get
            {
                return PhotoBookmart.Common.Helpers.CommonConst.DefaultSessionPrefix + SessionId;
            }
        }

        /// <summary>
        /// Front End - Return current language
        /// </summary>
        public Language CurrentLanguage
        {
            get
            {
                var site = FrontEnd_CurrentSite();
                // get all langs belongs to this site by inner join
                //JoinSqlBuilder<Language, Language> jn = new JoinSqlBuilder<Language, Language>();
                //jn = jn.Join<Language, Site_Lang_Dis>(m => m.Id, k => k.LanguageId);
                //jn.Where<Site_Lang_Dis>(m => m.SiteId == site.Id && m.Status);
                //jn.Where<Language>(m => m.Status);
                //var sql = jn.ToSql();
                //var langs = Db.Select<Language>(sql);

                var langs = Db.Where<Language>(m => m.Status);

                return langs.FirstOrDefault();
            }

        }

        /// <summary>
        /// Front End: Get all language belongs to current website
        /// </summary>
        public List<Language> Language_GetAll
        {
            get
            {
                var site = FrontEnd_CurrentSite();
                // get all langs belongs to this site by inner join
                //JoinSqlBuilder<Language, Language> jn = new JoinSqlBuilder<Language, Language>();
                //jn = jn.Join<Language, Site_Lang_Dis>(m => m.Id, k => k.LanguageId);
                //jn.Where<Site_Lang_Dis>(m => m.SiteId == site.Id && m.Status);
                //jn.Where<Language>(m => m.Status);
                //var sql = jn.ToSql();
                //var ret = Db.Select<Language>(sql);
                var ret = Db.Where<Language>(m => m.Status);
                return ret;
            }
        }

        public string CurrentWebsiteDomainURL
        {
            get
            {
                var request = (System.Web.HttpRequest)Request.OriginalRequest;
                var ret = request.Url.AbsoluteUri.Substring(0, request.Url.AbsoluteUri.Length - request.Url.PathAndQuery.Length + 1);
                return ret;
            }
        }

        /// <summary>
        /// Return template name for current URL request (for Front End)
        /// </summary>
        /// <returns></returns>
        public string FrontEnd_GetTheme()
        {
            var request = (System.Web.HttpRequest)Request.OriginalRequest;
            var host = request.Url.Scheme;
            host += "://";
            host += request.Url.Authority;

            var websites = Db.Where<Website>(m => m.Status == 1);

            var ret = websites.Where(m => m.IsValidDomain(host) || m.IsValidDomain(request.Url.Authority)).FirstOrDefault();
            if (ret == null)
                return "";
            else
                return ret.Theme;
        }

        /// <summary>
        /// Return website according to current request
        /// </summary>
        /// <returns></returns>
        public Website FrontEnd_CurrentSite()
        {
            var request = (System.Web.HttpRequest)Request.OriginalRequest;
            var host = request.Url.Scheme;
            host += "://";
            host += request.Url.Authority;

            var websites = Db.Where<Website>(m => m.Status == 1);

            var ret = websites.Where(m => m.IsValidDomain(host) || m.IsValidDomain(request.Url.Authority)).FirstOrDefault();
            if (ret == null)
                return null;
            else
                return ret;
        }

        #region Ultils
        /// <summary>
        /// Return language translation by enter the language key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="lang_id"></param>
        /// <returns></returns>
        public string Language_GetTranslation(string key, long lang_id)
        {
            key = key.ToLower();
            // use Cache
            List<Language_Translation> x = new List<Language_Translation>();
            x = Cache.Get<List<Language_Translation>>("TRANSLATION_FOR_" + key);
            if (x == null)
            {
                x = Db.Where<Language_Translation>(m => m.Key == key);
                Cache.Set<List<Language_Translation>>("TRANSLATION_FOR_" + key, x, TimeSpan.FromMinutes(1));
            }

            // the best result is key = key, lang = lang, site=site
            IEnumerable<Language_Translation> where_ret;
            if (lang_id != -1 )
            {
                where_ret = x.Where(m => m.Key == key && m.LangId == lang_id );
                if (where_ret.Count() > 0)
                {
                    return where_ret.First().Value;
                }
            }

            // case key = key ; lang = 0; site = site
            where_ret = x.Where(m => m.Key == key && m.LangId == 0 );
            if (where_ret.Count() > 0)
            {
                return where_ret.First().Value;
            }

            // case key = key ; lang = lang ; site = 0
            where_ret = x.Where(m => m.Key == key && m.LangId == lang_id );
            if (where_ret.Count() > 0)
            {
                return where_ret.First().Value;
            }

            // case key = key ; lang = 0 ; site = 0
            where_ret = x.Where(m => m.Key == key && m.LangId == 0);
            if (where_ret.Count() > 0)
            {
                return where_ret.First().Value;
            }

            return key;
        }

        /// <summary>
        /// Return language translation by enter the language key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="lang_code"></param>
        /// <returns></returns>
        public string Language_GetTranslation(string key, string lang_code)
        {
            long langid = -1;
            var lang = Language_GetAll.Where(m => m.LanguageCode == lang_code).FirstOrDefault();

            if (lang != null)
                langid = lang.Id;
            return Language_GetTranslation(key, langid);
        }
        #endregion

        #region Service API
        public object Post(WebSite_SetCurrent request)
        {
            try
            {

                var session_name = SessionNamePrefix + "_CURRENT_WEBSITE";
                Cache.Set<Website>(session_name, request);
            }
            catch
            {
            }
            return true;
        }

        #endregion
    }
}
