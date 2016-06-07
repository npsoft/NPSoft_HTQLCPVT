using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PhotoBookmart.Controllers
{
    public class ErrorsController : Controller
    {

        public ActionResult Index()
        {
            //return View();
            return Content("Err");
        }

        public ActionResult General(string mess)
        {
            //return View("Exception", exception);
            return Content(mess);
        }

        public ActionResult Http404(string mess)
        {
            if (string.IsNullOrEmpty(mess))
            {
                mess = "Page not found";
            }
            return View("Http404",(object)mess);
        }

        public ActionResult Http403()
        {
            return Content("Error 403");
        }
    }
}
