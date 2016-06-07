using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ServiceStack.CacheAccess;
using System.Data;
using ServiceStack.OrmLite;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.DataLayer.Models.Sites;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack;
using ServiceStack.ServiceInterface;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.ServiceInterface;
using PhotoBookmart.Support;
using System.Web.Routing;
using PhotoBookmart.Models;
using System.IO;
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.Support
{

    public abstract class RBVMCViewsBase<TModel> : WebViewPage<TModel>, IDisposable
    {
        private LocalAPI _TTGServiceAPI = null;
        public virtual LocalAPI InternalService
        {
            get
            {
                if (_TTGServiceAPI == null)
                {
                    _TTGServiceAPI = AppHost.ResolveService<LocalAPI>(System.Web.HttpContext.Current);
                }
                return _TTGServiceAPI;
            }
        }

        public string SessionId
        {
            get
            {
                return InternalService.GetSessionId();
            }
        }

        public virtual ICacheClient Cache
        {
            get
            {
                return InternalService.Cache;
            }
        }

        public void Dispose()
        {
            InternalService.Dispose();
            if (_connection != null)
            {
                _connection.Close();
            }

        }

        IDbConnection _connection = null;
        public virtual IDbConnection Db
        {
            get
            {
                if (_connection == null)
                {
                    _connection = AppHost.Resolve<IDbConnection>();
                }

                if (_connection != null && _connection.State != ConnectionState.Open)
                {
                    _connection.Open();
                }

                if (_connection != null && _connection.State != ConnectionState.Open)
                {
                    // force open new connection
                    _connection = AppHost.Resolve<IDbConnectionFactory>().Open();
                }

                return _connection;
            }
        }

        string _defaultCurrency = "";
        /// <summary>
        /// Return Default Currency
        /// </summary>
        public string DefaultCurrency
        {
            get
            {
                if (User.Identity.IsAuthenticated && !string.IsNullOrEmpty(CurrentUser.Country))
                {
                    var country = CurrentUser.Country;
                    var c = Db.Select<Country>(x => x.Where(m => m.Code == country).Limit(1)).FirstOrDefault();
                    if (c != null)
                    {
                        return c.CurrencyCode;
                    }
                }

                // still dont have currency
                if (string.IsNullOrEmpty(_defaultCurrency))
                {
                    _defaultCurrency = (string)Settings.Get(Enum_Settings_Key.WEBSITE_CURRENCY, "", Enum_Settings_DataType.String);
                }
                return _defaultCurrency;

            }
        }

        /// <summary>
        /// Get current country by context
        /// </summary>
        public Country CurrentCountry
        {
            get
            {
                var countries = Db.Where<Country>(x => x.Status);
                var country = new Country();

                if (User.Identity.IsAuthenticated)
                {
                    var c_code = CurrentUser.Country;
                    country = countries.Where(x => x.Code == c_code).FirstOrDefault();
                    if (country == null)
                    {
                        country = new Country();
                    }
                }
                else
                {
                    // try to get the country from the domain
                    var host = Request.Url.Authority;

                    var tcountry = countries.Where(x => x.Domains.Contains(host)).FirstOrDefault();

                    if (tcountry != null)
                    {
                        return tcountry;
                    }
                    else
                    {
                        //we find it by the default currency
                        var currency = DefaultCurrency;
                        var c = countries.Where(m => m.CurrencyCode == currency).FirstOrDefault();
                        if (c != null)
                        {
                            return c;
                        }
                    }
                }
                return country;
            }
        }

        /// <summary>
        /// Return Current Front End Website
        /// </summary>
        public virtual Website CurrentWebsite
        {
            get
            {
                return InternalService.FrontEnd_CurrentSite();
            }
        }

        /// <summary>
        /// Get current theme of current site
        /// </summary>
        public virtual string CurrentTheme
        {
            get
            {
                return InternalService.FrontEnd_GetTheme();
                if (Session["FrontEndCurrentTheme"] == null)
                    Session["FrontEndCurrentTheme"] = InternalService.FrontEnd_GetTheme();
                return Session["FrontEndCurrentTheme"].ToString();
            }
        }

        /// <summary>
        /// Return current Front End language
        /// </summary>
        public virtual Language CurrentLanguage
        {
            get
            {
                return InternalService.CurrentLanguage;
            }
        }

        /// <summary>
        /// Return Current Login User
        /// </summary>
        public virtual ApplicationPrincipal User
        {
            get
            {
                if (base.User == null)
                    return new ApplicationPrincipal();
                else
                    return base.User as ApplicationPrincipal;
            }
        }


        public ABUserAuth CurrentUser
        {
            get
            {
                if (InternalService != null)
                {
                    return InternalService.CurrentUser;
                }
                else
                {
                    return null;
                }
            }
        }

        public int CurrentUserId
        {
            get
            {
                if (InternalService != null)
                {
                    return InternalService.CurrentUserId;
                }
                else
                {
                    return 0;
                }
            }
        }


        /// <summary>
        /// Return translation for inputted key
        /// By default, this function will auto detect current language and site to get the most benefit translation
        /// You can define lang_id by your own
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual string T(string key, long lang_id = -1)
        {
            if (lang_id == -1 && CurrentLanguage != null)
            {
                lang_id = CurrentLanguage.Id;
            }

            return InternalService.Language_GetTranslation(key, lang_id);
        }

        public virtual string T(string key, string lang_code)
        {
            return InternalService.Language_GetTranslation(key, lang_code);
        }

        /// <summary>
        /// Trim string for pretty in Title, Keywords, and Description
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string TrimStringForTitle(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            else
                return text.Replace("\r", "").Replace("\n", "").Trim().Replace("  ", " ");
        }

        public IHtmlString Pagination(int pages, int page, string controller, string action, int total_items, int per_page = 20, RouteValueDictionary route = null, string viewName = "Pagination", string baseController = "WebAdmin")
        {
            //if (pages < 2)
            //{
            //    return Html.Raw("");
            //}

            if (route == null)
            {
                route = new RouteValueDictionary();
            }
            PaginationModel model = new PaginationModel() { action = action, controller = controller, page = page, pages = pages, route = route, per_page = per_page, total_items = total_items };
            ViewDataDictionary<PaginationModel> viewData = new ViewDataDictionary<PaginationModel>();
            viewData.Model = model;

            // generate fake controller context
            var context = new HttpContextWrapper(HttpContext.Current);
            var routeData = new RouteData();
            routeData.Values.Add("controller", baseController);
            var controllerContext = new ControllerContext(new RequestContext(context, routeData), new PhotoBookmart.Areas.Administration.Controllers.WebAdminController());

            using (StringWriter sw = new StringWriter())
            {
                ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(controllerContext, viewName);

                ViewContext viewContext = new ViewContext(controllerContext, viewResult.View, viewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);

                return Html.Raw(sw.GetStringBuilder().ToString());
            }
        }
    }


}
