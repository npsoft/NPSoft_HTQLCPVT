using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.Common.Helpers;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;
using PhotoBookmart.Areas.Administration.Controllers;
using PhotoBookmart.DataLayer.Models.Products;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    [RequiredRole("Administrator")]
    public class PaypalConfigController : WebAdminController
    {

        //[Authorize(Roles = "Administrator,Manager")]
        public ActionResult Edit()
        {
            PayPalStandardPaymentSettings model = PayPalStandardPaymentSettings.getSetting();
            return View(model);
        }

        [Authorize(Roles = "Administrator,Manager")]
        [HttpPost]
        public ActionResult Update(PayPalStandardPaymentSettings model, IEnumerable<HttpPostedFileBase> FileUp)
        {

            Db.Update<PayPalStandardPaymentSettings>(model);

            return RedirectToAction("Edit");
        }

    }
}
