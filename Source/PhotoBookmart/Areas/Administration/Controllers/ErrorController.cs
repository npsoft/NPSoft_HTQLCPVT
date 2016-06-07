using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.Controllers;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    public class ErrorController : BaseController
    {
        public ActionResult Unauthorized()
        {
            return View();
        }
    }
}
