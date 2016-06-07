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
using System.Configuration;
using System.Globalization;
using ServiceStack.OrmLite;
using ServiceStack.Common;

namespace PhotoBookmart.Controllers
{
    /// <summary>
    /// Paypal Payment Controller
    /// Source code copy from NopCommerce
    /// </summary>
    public class PaypalController : BaseController
    {
        PayPalStandardPaymentSettings _paypalStandardPaymentSettings = null;
        PaymentPaypalLib paypal = new PaymentPaypalLib();

        protected override void Initialize(RequestContext requestContext)
        {
            if (_paypalStandardPaymentSettings == null)
            {
                _paypalStandardPaymentSettings = PayPalStandardPaymentSettings.getSetting();
            }
            base.Initialize(requestContext);
        }




        #region Action Handler
        [ValidateInput(false)]
        public ActionResult PDTHandler(FormCollection form)
        {
            string tx = HttpContext.Request.QueryString.Get("tx");
            Dictionary<string, string> values;
            string response;

            if (paypal.GetPDTDetails(tx, out values, out response))
            {
                string orderNumber = string.Empty;
                values.TryGetValue("custom", out orderNumber);


                Order order = Db.Select<Order>(x => x.Where(m => m.Order_Number == orderNumber).Limit(1)).FirstOrDefault();
                if (order != null)
                {
                    decimal total = decimal.Zero;
                    try
                    {
                        total = decimal.Parse(values["mc_gross"], new CultureInfo("en-US"));
                    }
                    catch (Exception exc)
                    {
                        throw new Exception("PayPal PDT. Error getting mc_gross");
                    }

                    string payer_status = string.Empty;
                    values.TryGetValue("payer_status", out payer_status);
                    string payment_status = string.Empty;
                    values.TryGetValue("payment_status", out payment_status);
                    string pending_reason = string.Empty;
                    values.TryGetValue("pending_reason", out pending_reason);
                    string mc_currency = string.Empty;
                    values.TryGetValue("mc_currency", out mc_currency);
                    string txn_id = string.Empty;
                    values.TryGetValue("txn_id", out txn_id);
                    string payment_type = string.Empty;
                    values.TryGetValue("payment_type", out payment_type);
                    string payer_id = string.Empty;
                    values.TryGetValue("payer_id", out payer_id);
                    string receiver_id = string.Empty;
                    values.TryGetValue("receiver_id", out receiver_id);
                    string invoice = string.Empty;
                    values.TryGetValue("invoice", out invoice);
                    string payment_fee = string.Empty;
                    values.TryGetValue("payment_fee", out payment_fee);

                    var sb = new StringBuilder();
                    sb.AppendLine("Paypal PDT:");
                    sb.AppendLine("total: " + total);
                    sb.AppendLine("Payer status: " + payer_status);
                    sb.AppendLine("Payment status: " + payment_status);
                    sb.AppendLine("Pending reason: " + pending_reason);
                    sb.AppendLine("mc_currency: " + mc_currency);
                    sb.AppendLine("txn_id: " + txn_id);
                    sb.AppendLine("payment_type: " + payment_type);
                    sb.AppendLine("payer_id: " + payer_id);
                    sb.AppendLine("receiver_id: " + receiver_id);
                    sb.AppendLine("invoice: " + invoice);
                    sb.AppendLine("payment_fee: " + payment_fee);

                    order.AddHistory(sb.ToString(), "Paypal", 0);

                    ////validate order total
                    //if (payPalStandardPaymentSettings.PdtValidateOrderTotal && !Math.Round(total, 2).Equals(Math.Round(order.OrderTotal, 2)))
                    //{
                    //    string errorStr = string.Format("PayPal PDT. Returned order total {0} doesn't equal order total {1}", total, order.OrderTotal);
                    //    _logger.Error(errorStr);

                    //    return RedirectToAction("Index", "Home", new { area = "" });
                    //}

                    //mark order as paid
                    //if (_orderProcessingService.CanMarkOrderAsPaid(order))
                    //{
                    //    order.AuthorizationTransactionId = txn_id;
                    //    _orderService.UpdateOrder(order);

                    //    _orderProcessingService.MarkOrderAsPaid(order);
                    //}
                    //order.Set_PaymentStatus(Enum_PaymentStatus.Authorized);
                    order.AddHistory("Palpal is processing the payment. Update payment method to Paypal", "System", 0, true);
                    Db.UpdateOnly<Order>(new Order() { Payment_AuthorizationTransactionId = txn_id, PaymentMethod = Enum_PaymentMethod.Paypal }, ev => ev.Update(p => new { p.PaymentMethod, p.Payment_AuthorizationTransactionId }).Where(m => m.Id == order.Id));
                }
                order.Dispose();
                return RedirectToAction("OrderInvoiceDetail", "Product", new { id = order.Order_Number });
            }
            else
            {
                string orderNumber = string.Empty;
                // in case wrongly, normally it return the parameter cm
                values.TryGetValue("cm", out orderNumber);

                Order order = Db.Select<Order>(x => x.Where(m => m.Order_Number == orderNumber).Limit(1)).FirstOrDefault();
                if (order != null)
                {
                    //order note
                    order.AddHistory("Paypal PDT failed. Responsed from Paypal: " + response, "Paypal", 0);
                    order.Dispose();
                    return RedirectToAction("OrderInvoiceDetail", "Product", new { id = order.Order_Number });
                }
                return RedirectToAction("Index", "Home");
            }

        }

        [ValidateInput(false)]
        public ActionResult IPNHandler()
        {
            byte[] param = HttpContext.Request.BinaryRead(Request.ContentLength);
            string strRequest = Encoding.ASCII.GetString(param);
            Dictionary<string, string> values;

            if (paypal.VerifyIPN(strRequest, out values))
            {
                #region values
                decimal total = decimal.Zero;
                try
                {
                    total = decimal.Parse(values["mc_gross"], new CultureInfo("en-US"));
                }
                catch { }

                string payer_status = string.Empty;
                values.TryGetValue("payer_status", out payer_status);
                string payment_status = string.Empty;
                values.TryGetValue("payment_status", out payment_status);
                string pending_reason = string.Empty;
                values.TryGetValue("pending_reason", out pending_reason);
                string mc_currency = string.Empty;
                values.TryGetValue("mc_currency", out mc_currency);
                string txn_id = string.Empty;
                values.TryGetValue("txn_id", out txn_id);
                string txn_type = string.Empty;
                values.TryGetValue("txn_type", out txn_type);
                string rp_invoice_id = string.Empty;
                values.TryGetValue("rp_invoice_id", out rp_invoice_id);
                string payment_type = string.Empty;
                values.TryGetValue("payment_type", out payment_type);
                string payer_id = string.Empty;
                values.TryGetValue("payer_id", out payer_id);
                string receiver_id = string.Empty;
                values.TryGetValue("receiver_id", out receiver_id);
                string invoice = string.Empty;
                values.TryGetValue("invoice", out invoice);
                string payment_fee = string.Empty;
                values.TryGetValue("payment_fee", out payment_fee);

                #endregion

                var sb = new StringBuilder();
                sb.AppendLine("Paypal IPN:");
                foreach (KeyValuePair<string, string> kvp in values)
                {
                    sb.AppendLine(kvp.Key + ": " + kvp.Value);
                }

                var newPaymentStatus = paypal.GetPaymentStatus(payment_status, pending_reason);
                sb.AppendLine("New payment status: " + newPaymentStatus);

                switch (txn_type)
                {
                    case "recurring_payment_profile_created":
                        //do nothing here
                        break;
                    case "recurring_payment":
                        //#region Recurring payment
                        //{
                        //    Guid orderNumberGuid = Guid.Empty;
                        //    try
                        //    {
                        //        orderNumberGuid = new Guid(rp_invoice_id);
                        //    }
                        //    catch
                        //    {
                        //    }

                        //    var initialOrder = _orderService.GetOrderByGuid(orderNumberGuid);
                        //    if (initialOrder != null)
                        //    {
                        //        var recurringPayments = _orderService.SearchRecurringPayments(0, 0, initialOrder.Id, null, 0, int.MaxValue);
                        //        foreach (var rp in recurringPayments)
                        //        {
                        //            switch (newPaymentStatus)
                        //            {
                        //                case PaymentStatus.Authorized:
                        //                case PaymentStatus.Paid:
                        //                    {
                        //                        var recurringPaymentHistory = rp.RecurringPaymentHistory;
                        //                        if (recurringPaymentHistory.Count == 0)
                        //                        {
                        //                            //first payment
                        //                            var rph = new RecurringPaymentHistory()
                        //                            {
                        //                                RecurringPaymentId = rp.Id,
                        //                                OrderId = initialOrder.Id,
                        //                                CreatedOnUtc = DateTime.UtcNow
                        //                            };
                        //                            rp.RecurringPaymentHistory.Add(rph);
                        //                            _orderService.UpdateRecurringPayment(rp);
                        //                        }
                        //                        else
                        //                        {
                        //                            //next payments
                        //                            _orderProcessingService.ProcessNextRecurringPayment(rp);
                        //                        }
                        //                    }
                        //                    break;
                        //            }
                        //        }

                        //        //this.OrderService.InsertOrderNote(newOrder.OrderId, sb.ToString(), DateTime.UtcNow);
                        //        _logger.Information("PayPal IPN. Recurring info", new NopException(sb.ToString()));
                        //    }
                        //    else
                        //    {
                        //        _logger.Error("PayPal IPN. Order is not found", new NopException(sb.ToString()));
                        //    }
                        //}
                        //#endregion
                        break;
                    default:
                        #region Standard payment
                        {
                            string orderNumber = string.Empty;
                            values.TryGetValue("custom", out orderNumber);

                            Order order = Db.Select<Order>(x => x.Where(m => m.Order_Number == orderNumber).Limit(1)).FirstOrDefault();
                            if (order != null)
                            {

                                order.AddHistory(sb.ToString(), "Paypal", 0);

                                switch (newPaymentStatus)
                                {
                                    case Enum_PaymentStatus.Pending:
                                        {
                                        }
                                        break;
                                    case Enum_PaymentStatus.Authorized:
                                        {
                                            order.Set_PaymentStatus(Enum_PaymentStatus.Authorized);
                                        }
                                        break;
                                    case Enum_PaymentStatus.Paid:
                                        {
                                            order.Payment_AuthorizationTransactionId = txn_id;
                                            order.Set_PaymentStatus(Enum_PaymentStatus.Paid);
                                            order.PaymentMethod = Enum_PaymentMethod.Paypal;
                                        }
                                        break;
                                    case Enum_PaymentStatus.Refunded:
                                        {
                                            order.Set_PaymentStatus(Enum_PaymentStatus.Refunded);
                                        }
                                        break;
                                    case Enum_PaymentStatus.Voided:
                                        {
                                            //order.PaymentStatus = (int)Enum_PaymentStatus.Voided;
                                            order.Set_PaymentStatus(Enum_PaymentStatus.Voided);
                                        }
                                        break;
                                    default:
                                        break;
                                }

                                Db.Update<Order>(order);
                            }
                            else
                            {
                                throw new Exception("PayPal IPN. Order is not found", new Exception(sb.ToString()));
                            }
                        }
                        #endregion
                        break;
                }
            }
            else
            {
                throw new Exception("PayPal IPN failed.", new Exception(strRequest));
            }

            //nothing should be rendered to visitor
            return Content("");
        }

        public ActionResult CancelOrder(FormCollection form)
        {
            //if (_paypalStandardPaymentSettings.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage)
            //{
            //    var order = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
            //        customerId: _workContext.CurrentCustomer.Id, pageSize: 1)
            //        .FirstOrDefault();
            //    if (order != null)
            //    {
            //        return RedirectToRoute("OrderDetails", new { orderId = order.Id });
            //    }
            //}

            return RedirectToAction("Index", "Home");
        }
        #endregion




    }
}