using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Net;
using System.IO;
using System.Web.Routing;
using System.Text;
using System.Web;
using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.Support.Payment;
using ServiceStack.OrmLite;
using PhotoBookmart.DataLayer.Models.System;
using ServiceStack.Text;

namespace PhotoBookmart.Controllers
{
    /// <summary>
    /// Paypal Payment Controller
    /// Source code copy from NopCommerce
    /// </summary>
    public class iPay88Controller : BaseController
    {
        public ActionResult Response(iPay_ResponseModel model)
        {
            // try to log into exception for later debug
            //Exceptions ex = new Exceptions();
            //ex.ExMessage = "iPay88 Calback";
            //ex.ExStackTrace = model.ToJson();
            //Db.Insert<Exceptions>(ex);

            // check the RefNo == OderNumber
            var order = Db.Select<Order>(x => x.Where(m => m.Order_Number == model.RefNo).Limit(1)).FirstOrDefault();

            // generate the signature
            PayPalStandardPaymentSettings settings = PayPalStandardPaymentSettings.getSetting();
            if (settings.UseSandbox)
            {
                order.Bill_Total = 1;
            }
            var signagure = new iPay88Helper().generate_SHA1keyResponse(order, model.PaymentId);

            if (order != null)
            {
                if (model.Status == "1" && signagure == model.Signature && order.PaymentStatusEnum != Enum_PaymentStatus.Paid)
                {

                    order.AddHistory("Receive callback from iPay88: " + model.ToJson(), "iPay88", 0, true);
                    order.Set_PaymentStatus(Enum_PaymentStatus.Paid);
                    order.AddHistory("Update payment method to iPay88", "System", 0, true);
                    Db.UpdateOnly<Order>(new Order() { Payment_AuthorizationTransactionId = model.TransId, PaymentMethod = Enum_PaymentMethod.iPay88 }, ev => ev.Update(p => new { p.Payment_AuthorizationTransactionId, p.PaymentMethod }).Where(m => m.Id == order.Id));
                    // return RedirectToAction("OrderInvoiceDetail", "Product", new { id = order.Order_Number });
                }
                return RedirectToAction("OrderInvoiceDetail", "Product", new { id = order.Order_Number });
            }
            else
            {
                return Redirect("/");
            }
        }
    }
}