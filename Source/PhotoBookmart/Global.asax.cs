using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Globalization;
using PhotoBookmart.Support;
using ServiceStack.OrmLite;
using PhotoBookmart.ServiceInterface;
using System.Web.Mvc;
using PhotoBookmart.DataLayer.Models.Sites;
using PhotoBookmart.Common.Helpers;
using System.Configuration;
using PhotoBookmart.Tasks;

namespace PhotoBookmart
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_PreSendRequestHeaders(object sender, EventArgs e)
        {
            Response.AddHeader("X-Frame-Options", "SAMEORIGIN");
        }

        /// <summary>
        /// Keep Alive timer to keep the site alive
        /// </summary>
        static KeepAliveTask keep_alive_task;

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new TTGErrorHandler());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            try
            {
                routes.IgnoreRoute("Content/Administration/Content/{*pathInfo}");
                routes.IgnoreRoute("Content/{*pathInfo}");
                routes.IgnoreRoute("Script/{*pathInfo}");
                routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
                routes.IgnoreRoute("api/{*pathInfo}");

                // fix bug detail payment shipping
               
                routes.MapRoute(
                    null,
                    "Logon/{id}",
                    new { controller = "Home", action = "SignIn", id = UrlParameter.Optional }
                );

                routes.MapRoute(
                    "JavascriptCommont", // Route name
                    "common.js", // URL with parameters
                    new { controller = "Home", action = "JavascriptCommon" }// Parameter defaults
                );

                ///////// For Default
                routes.MapRoute(
                   "Error404", // Route name
                   "http_404/{mess}", // URL with parameters
                   new { controller = "Errors", action = "Http404", mess = UrlParameter.Optional },
                     new string[] { "PhotoBookmart.Controllers" }
               );

                // topic detail
                routes.MapRoute(
                  "TopicDetail", // Route name
                  "Detail/{id}", // URL with parameters
                  new { controller = "Topic", action = "TopicDetail", id = UrlParameter.Optional },
                    new string[] { "PhotoBookmart.Controllers" }
              );

                // my photo creation
                routes.MapRoute(
                  "myphotocreation", // Route name
                  "cart/getinfo.asp", // URL with parameters
                  new { controller = "Product", action = "MyPhotoCreation" },
                    new string[] { "PhotoBookmart.Controllers" }
              );

                // topic detail
                routes.MapRoute(
                  "Photobookstyle", // Route name
                  "photobooks/{id}", // URL with parameters
                  new { controller = "Style", action = "Detail", id = UrlParameter.Optional },
                    new string[] { "PhotoBookmart.Controllers" }
              );


                // barcode for order
                routes.MapRoute(
              "barcode", // Route name
              "barcode/{id}", // URL with parameters
              new { controller = "Order", action = "Barcode", id = UrlParameter.Optional },
                new string[] { "PhotoBookmart.Controllers" }
          );

                // invoice detail
                routes.MapRoute(
              "invoice", // Route name
              "invoice", // URL with parameters
              new { controller = "Product", action = "OrderInvoiceDetail", id = UrlParameter.Optional },
                new string[] { "PhotoBookmart.Controllers" }
          );

                // payment and shipping
                routes.MapRoute(
              "Help", // Route name
              "help", // URL with parameters
              new { controller = "Home", action = "Help", id = UrlParameter.Optional },
                new string[] { "PhotoBookmart.Controllers" }
          );

                // pricing
                routes.MapRoute(
              "Pricing", // Route name
              "pricing", // URL with parameters
              new { controller = "Style", action = "Pricing", id = UrlParameter.Optional },
                new string[] { "PhotoBookmart.Controllers" }
          );

                // payment and shipping
                routes.MapRoute(
              "PaymentShipping", // Route name
              "payment-shipping", // URL with parameters
              new { controller = "Style", action = "PaymentShipping", id = UrlParameter.Optional },
                new string[] { "PhotoBookmart.Controllers" }
          );

                // new order
                routes.MapRoute(
              "newOrder", // Route name
              "new_order", // URL with parameters
              new { controller = "Product", action = "NewOrder", id = UrlParameter.Optional },
                new string[] { "PhotoBookmart.Controllers" }
          );

                routes.MapRoute(
                   "Default", // Route name
                   "{controller}/{action}/{id}", // URL with parameters
                   new { controller = "Home", action = "Index", id = UrlParameter.Optional }, // Parameter defaults
                   new string[] { "PhotoBookmart.Controllers" }
               );


                ///////////////////////

                //routes.MapRoute("CatchAll", "{*url}",
                //    new { controller = "Home", action = "Index" }
                //);
            }
            catch
            {
            }
            //ABLocalization.RouteHandleWithLocalization(ref routes);
        }

        public static void RegisterViewEngine(ViewEngineCollection viewEngines)
        {
            // We do not need the default view engine
            viewEngines.Clear();

            var themeableRazorViewEngine = new ThemeableRazorViewEngine
            {
                CurrentTheme = httpContext =>
                {
                    if (httpContext.Session == null)
                    {
                        return "";
                    }

                    string theme = "";

                    if (httpContext.Session != null && httpContext.Session["theme"] != null && (string)httpContext.Session["theme"] != "")
                    {
                        theme = httpContext.Session["theme"].ToString();
                    }
                    else
                    {
                        var Db = AppHost.Resolve<IDbConnection>();
                        if (Db != null && Db.State != ConnectionState.Open)
                        {
                            Db = AppHost.Resolve<IDbConnectionFactory>().Open();
                        }

                        var request = HttpContext.Current.Request;
                        var host = request.Url.Scheme;
                        host += "://";
                        host += request.Url.Authority;

                        var websites = Db.Where<Website>(m => m.Status == 1);

                        var ret = websites.Where(m => m.IsValidDomain(host) || m.IsValidDomain(request.Url.Authority)).FirstOrDefault();
                        theme = "";
                        if (ret != null)
                            theme = ret.Theme;
                        httpContext.Session.Add("theme", theme);

                        // 
                        Db.Close();
                    }

                    return theme;
                }
            };

            viewEngines.Add(themeableRazorViewEngine);
        }

        protected void Application_Start()
        {
            // start our local service
            AppHost.Start();
            // Register Custom Memory Cache for Child Action Method Caching
            OutputCacheAttribute.ChildActionCache = new CustomMemoryCache("My Cache");

            AreaRegistration.RegisterAllAreas();
            PhotoBookmart.Common.Helpers.SendEmail.StartThreadSendMail();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
            RegisterViewEngine(ViewEngines.Engines);

            // start the keep alive
            var site_url = ConfigurationManager.AppSettings.Get("PaypalWebsiteURL") + "keepalive/index";
            keep_alive_task = new KeepAliveTask(site_url);
            keep_alive_task.Start();

            // start the sms queue
            SMSTransferAgentTask.Start();

            AutoCancelUnpaidOrder.Start();

            //Set MVC to use the same Funq IOC as ServiceStack
            //ControllerBuilder.Current.SetControllerFactory(new FunqControllerFactory(AppHost.Instance.Container));
        }

        public override void Dispose()
        {
            try
            {
                //keep_alive_task.Dispose();
                //sms_transfer_agent_task.Dispose();
            }
            catch
            {
            }
            base.Dispose();
        }

        /// <summary>
        /// Handle Error Request
        /// </summary>
        protected void Application_Errora()
        {
            var exception = Server.GetLastError();
            var httpException = exception as HttpException;

            Response.Clear();
            Server.ClearError();
            var routeData = new RouteData();
            routeData.Values["controller"] = "Errors";
            routeData.Values["action"] = "General";
            Response.StatusCode = 500;
            if (httpException != null)
            {
                Response.StatusCode = httpException.GetHttpCode();
                switch (Response.StatusCode)
                {
                    case 403:
                        routeData.Values["action"] = "Http403";
                        break;
                    case 404:
                        routeData.Values["action"] = "Http404";
                        routeData.Values["mess"] = exception.Message;
                        break;
                    default:
                        routeData.Values["mess"] = exception.Message;
                        break;
                }
            }
            // Avoid IIS7 getting in the middle
            Response.TrySkipIisCustomErrors = true;
            IController errorsController = new PhotoBookmart.Controllers.ErrorsController();
            HttpContextWrapper wrapper = new HttpContextWrapper(Context);
            var rc = new RequestContext(wrapper, routeData);
            errorsController.Execute(rc);

        }
    }
}
