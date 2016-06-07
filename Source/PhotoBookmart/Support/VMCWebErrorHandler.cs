using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using System.Data;
using PhotoBookmart.ServiceInterface;
using PhotoBookmart.DataLayer.Models.System;

namespace PhotoBookmart.Support
{
    [ValidateInput(false)]
    public class TTGErrorHandler : HandleErrorAttribute
    {
        //private LocalAPI _TTGServiceAPI = null;
        //public virtual LocalAPI TTGService
        //{
        //    get
        //    {
        //        if (_TTGServiceAPI == null)
        //        {
        //            _TTGServiceAPI = AppHost.ResolveService<LocalAPI>(System.Web.HttpContext.Current);
        //        }
        //        return _TTGServiceAPI;
        //    }
        //}


        public TTGErrorHandler()
        {
        }

        public override void OnException(ExceptionContext filterContext)
        {
            //|| !filterContext.HttpContext.IsCustomErrorEnabled
            if (filterContext.ExceptionHandled)
            {
                return;
            }

            if (filterContext.Exception.Message.StartsWith("Server cannot set content type"))
            {
                return;
            }

            var exception_code = new HttpException(null, filterContext.Exception).GetHttpCode();
            if (!(exception_code == 500 || exception_code == 403 || exception_code == 401))
            {
                return;
            }

            if (!ExceptionType.IsInstanceOfType(filterContext.Exception))
            {
                return;
            }

            // log exception into db
            var httpcontext = System.Web.HttpContext.Current;
            var x = new Exceptions();
            x.ExceptionOn = DateTime.Now;
            x.ServerHost = httpcontext.Request.Url.Host;

            try
            {
                x.UserIp = HttpContext.Current.Request.UserHostAddress;
            }
            catch
            {
            }
            //x.UserId = TTGService.CurrentUserId;
            //if (TTGService.CurrentUser != null)
            //{
            //    x.UserName = TTGService.CurrentUser.UserName;
            //}
            //else
            //    x.UserName = "";

            x.ContextHttpCode = exception_code;
            //x.ContextSessionId = TTGService.SessionId;
            x.ContextHttpMethod = httpcontext.Request.HttpMethod;
            x.ContextBrowserAgent = httpcontext.Request.UserAgent;
            x.ContextUrl = httpcontext.Request.RawUrl;
            x.ContextHeader = httpcontext.Request.Headers.ToString();
            x.ContextForm = httpcontext.Request.Form.ToString();

            x.ExMessage = filterContext.Exception.Message;
            x.ExSource = filterContext.Exception.Source;
            x.ExStackTrace = filterContext.Exception.StackTrace;

            var Db = AppHost.Resolve<IDbConnectionFactory>().Open();
            Db.Insert<Exceptions>(x);
            Db.Close();
            //TTGService.Dispose();

            // insert into email queue
            PhotoBookmart.Common.Helpers.SendEmail.SendMail("", "Photobookmart exception on " + DateTime.Now.ToString() + ": " + x.EmailTitle, x.EmailBody);


            var controllerName = (string)filterContext.RouteData.Values["controller"];
            var actionName = (string)filterContext.RouteData.Values["action"];
            var model = new HandleErrorInfo(filterContext.Exception, controllerName, actionName);

            filterContext.Result = new ViewResult
            {
                ViewName = View,
                MasterName = Master,
                ViewData = new ViewDataDictionary<HandleErrorInfo>(model),
                TempData = filterContext.Controller.TempData
            };


            filterContext.ExceptionHandled = true;
            filterContext.HttpContext.Response.Clear();
            filterContext.HttpContext.Response.StatusCode = exception_code;

            filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
        }
    }
}