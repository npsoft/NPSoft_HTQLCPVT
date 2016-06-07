using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PhotoBookmart.Controllers
{
    public class KeepAliveController : Controller
    {
        public ActionResult Index()
        {
            //return View();
            return Content("Still Alive ... ");
        }
    }
}
