using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.DataLayer.Models;
using ServiceStack.Mvc;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using ServiceStack.Text;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.Models;
using ServiceStack.ServiceInterface;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.ExtraShipping;

namespace PhotoBookmart.Controllers
{
    public class OrderController : BaseController
    {
        [Authenticate]
        public ActionResult Index()
        {
            var current_user = CurrentUser;

            // check user role
            var user = CurrentUser;
            List<Order> c = Db.Select<Order>(x => x.Where(m => m.Customer_Id == user.Id).OrderByDescending(d => d.CreatedOn));

            return View(c);
        }

        [Authenticate]
        public ActionResult Detail(int id)
        {
            var u = CurrentUser;
            var order = Db.Select<Order>(x => x.Where(m => m.Id == id && m.Customer_Id == u.Id)).FirstOrDefault();
            if (order == null)
            {
                return RedirectToAction("Index");
            }

            if (order.Status == (int)Enum_OrderStatus.Received || order.StatusEnum == Enum_OrderStatus.Verify)
            {
                var ticket = Db.Select<Order_UploadFilesTicket>(x => x.Where(y => (y.OrderId == order.Id)).Limit(1)).FirstOrDefault();

                if (ticket != null)
                {
                    ViewData["TicketId"] = ticket.Id;

                    order.UploadFilesTicketStatus = ((Enum_UploadFilesTicketStatus)ticket.Status).ToString();
                }
            }

            return View(order);
        }

        /// <summary>
        /// Return barcode of the order
        /// </summary>
        /// <param name="id">Order Id</param>
        /// <returns></returns>
        public ActionResult Barcode(int id)
        {
            var order = Db.Select<Order>(m => m.Where(x => x.Id == id).Limit(1)).FirstOrDefault();
            if (order == null)
            {
                order = new Order();
                order.Order_Number = "000000";
            }

            var dir = Server.MapPath("~/Content/product_barcode/");
            var path = System.IO.Path.Combine(dir, id + ".jpg");

            if (!System.IO.File.Exists(path) || DateTime.Now.Subtract(System.IO.File.GetCreationTime(path)).TotalDays > 1)
            {

                var image = new PhotoBookmart.Lib.OrderLib(order).Barcode();
                image.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
                image.Dispose();
            }
            return base.File(path, "image/jpeg");
        }

        #region History
        /// <summary>
        /// Load next page for order history
        /// </summary>
        /// <param name="id"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        [Authenticate]
        [HttpPost]
        public ActionResult History_LoadNext(long id, int page)
        {
            var order = Db.Select<Order>(m => m.Where(x => x.Id == id).Limit(1)).FirstOrDefault();
            if (order == null)
            {
                return RedirectToAction("Index");
            }

            int page_size = 9;

            // update the new message flag when page 1 only, because we list the new message first
            if (page == 1 && order.FlagHistoryMessage == (int)Enum_FlagOrderMessage.NewMessageFromPhotobookmart)
            {
                order.AddHistory("Customer read messages from Photobookmart", "System", 0, true);
                // update order to let them know we have new message from customer
                Db.UpdateOnly<Order>(new Order() { FlagHistoryMessage = (int)Enum_FlagOrderMessage.No_NewMessage },
                   ev => ev.Update(p => new
                   {
                       p.FlagHistoryMessage
                   }).Where(m => m.Id == order.Id));
            }

            // count fist
            var count = (int)Db.Count<Order_History>(x => x.Order_Id == id && x.isPrivate == false);
            var pages = count / page_size;
            if (count % page_size > 0)
            {
                pages++;
            }

            var ret = Db.Select<Order_History>(x => x.Where(m => m.Order_Id == id && m.isPrivate == false).OrderByDescending(k => k.OnDate).Limit((page - 1) * page_size, page_size));
            var users = Db.Select<ABUserAuth>();

            // get user who submit order
            var order_user = users.Where(x => x.Id == order.Customer_Id).FirstOrDefault();
            if (order_user == null)
            {
                order_user = new ABUserAuth();
            }
            foreach (var item in ret)
            {
                // check usertype and avatar
                item.UserAvatar = "";
                if (item.UserId == order_user.Id)
                {
                    item.Direction = "left";
                    item.UserAvatar = order_user.Avatar;
                    item.UserName = "You";
                }
                else
                {
                    // system or staff
                    item.Direction = "right";
                    item.UserAvatar = "Content/default_system_orderhistory_logo.png";
                    // 
                    var u = users.Where(x => x.Id == item.UserId).FirstOrDefault();
                    if (u != null)
                    {
                        item.UserName = string.Format("{0} {1}", u.FirstName, u.LastName);
                        item.UserAvatar = u.Avatar;
                        // incase this user does not have avatar
                        if (string.IsNullOrEmpty(item.UserAvatar))
                        {
                            item.UserAvatar = "content/default_chat_avatar.png";
                        }
                    }
                }

                // date format
                var dif = (int)DateTime.Now.Subtract(item.OnDate).TotalMinutes;
                if (dif < 2)
                {
                    item.OnDateFormat = "Now";
                }
                else if (dif < 60)
                {
                    item.OnDateFormat = string.Format("{0} minutes ago", dif);
                }
                else if (dif <= 60 * 8) // 8 hours
                {
                    dif = dif / 60;
                    item.OnDateFormat = string.Format("About {0} hours ago", dif);
                }
                else
                {
                    item.OnDateFormat = string.Format("{0:dddd, MMMM dd, yyyy HH:mm:ss}", item.OnDate);
                }

            }

            Db.Close();
            if (ret.Count == 0)
            {
                return Content("");
            }
            else
            {
                return PartialView(ret);
            }
        }

        /// <summary>
        /// Insert a message into order history
        /// </summary>
        /// <param name="OrderId"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        [HttpPost]
        [Authenticate]
        public ActionResult History_InsertComment(OrderHistory_Add model)
        {
            var order = Db.Select<Order>(m => m.Where(x => x.Id == model.OrderId).Limit(1)).FirstOrDefault();
            if (order == null)
            {
                return JsonError("Order is not found");
            }

            Order_History k = new Order_History();
            k.Content = model.Message;
            k.isPrivate = false;
            k.OnDate = DateTime.Now;
            k.Order_Id = model.OrderId;
            k.UserName = CurrentUser.FullName;
            k.UserId = CurrentUser.Id;
            k.IsCustomer = true;
            Db.Insert<Order_History>(k);

            // update order to let them know we have new message from customer
            Db.UpdateOnly<Order>(new Order() { FlagHistoryMessage = (int)Enum_FlagOrderMessage.NewMessageFromCustomer },
               ev => ev.Update(p => new
               {
                   p.FlagHistoryMessage
               }).Where(m => m.Id == order.Id));

            return JsonSuccess("", "Your message has been sent to Photobookmart. Please wait while we are processing your order. ");
        }
        #endregion

        #region Shipping

        [HttpPost]
        [Authenticate]
        public ActionResult UpdateShippingAddr(AddressModel model, long OrderId)
        {
            var current_user = CurrentUser;
            var order = Db.Select<Order>(x => x.Where(m => m.Id == OrderId && m.Customer_Id == current_user.Id).Limit(1)).FirstOrDefault();

            if (order == null)
            {
                return JsonError("Can not find your order.");
            }
            else
            {
                if (!(order.Status > 0 && order.Status < (int)Enum_OrderStatus.Shipping))
                {
                    return JsonError("Sorry, you can not update shipping address of this order");
                }

                if (model.Id != order.ShippingAddress)
                {
                    return JsonError("You have no permission to update this shipping address");
                }

                order.LoadAddress(1);

                AddressModel compare = order.ShippingAddressModel;
                string address_name = "Shipping Address";

                string content = "";
                if (model.FirstName != compare.FirstName)
                {
                    content += string.Format("\r\n First name change from {0} to {1}", compare.FirstName, model.FirstName);
                }
                if (model.LastName != compare.LastName)
                {
                    content += string.Format("\r\n Last name change from {0} to {1}", compare.LastName, model.LastName);
                }
                if (model.Email != compare.Email)
                {
                    content += string.Format("\r\n Email change from {0} to {1}", compare.Email, model.Email);
                }
                if (model.Company != compare.Company)
                {
                    content += string.Format("\r\n Company change from {0} to {1}", compare.Company, model.Company);
                }

                // Dont allow them to change the city and country
                model.Country = compare.Country;
                model.City = compare.City;

                if (model.Address != compare.Address)
                {
                    content += string.Format("\r\n Address change from {0} to {1}", compare.Address, model.Address);
                }
                if (model.State != compare.State)
                {
                    content += string.Format("\r\n State change from {0} to {1}", compare.State, model.State);
                }

                if (model.ZipPostalCode != compare.ZipPostalCode)
                {
                    content += string.Format("\r\n Zip / PostalCode change from {0} to {1}", compare.ZipPostalCode, model.ZipPostalCode);
                }

                if (model.PhoneNumber != compare.PhoneNumber)
                {
                    content += string.Format("\r\n Phone number change from {0} to {1}", compare.PhoneNumber, model.PhoneNumber);
                }

                // check the state and phone number
                var country = Db.Select<Country>(x => x.Where(y => y.Code == order.ShippingAddressModel.Country).Limit(1)).FirstOrDefault();
                if (country != null)
                {
                    if (country.Code == "MY" &&
                        model.State != compare.State &&
                        Db.Select<Country_State_ExtraShipping>(x => x.Where(y => (
                            y.CountryId == country.Id &&
                            y.State == model.State &&
                            y.Amount == 0)).Limit(1)).FirstOrDefault() == null)
                    {
                        return JsonError("Sorry, You can not change the shipping address of this order to state " + model.State + ".");
                    }
                }
                else
                {
                    return JsonError("Sorry, we can not validate the new country of this order");
                }

                // validate the phone number
                /*if (!IsValidPhoneByCountry(model.PhoneNumber, order.ShippingAddressModel.Country, true))
                {
                    return JsonError("Sorry, We can not validate your phone number. Please enter valid mobile phone number.");
                }*/

                if (content != "")
                {
                    content = "Update " + address_name + content;
                    Db.UpdateOnly<AddressModel>(model, ev => ev.Update(p => new
                    {
                        p.FirstName,
                        p.LastName,
                        p.Email,
                        p.Company,
                        //p.Country,
                        //p.City,
                        p.Address,
                        //p.ZipPostalCode,
                        p.PhoneNumber,
                        //p.FaxNumber,
                        p.State
                    }).Where(m => m.Id == model.Id).Limit(1));
                    order.AddHistory(content, CurrentUser.FullName, CurrentUser.Id);
                }

                return JsonSuccess(null, "Address updated successfully.");
            }
        }

        #endregion

        [HttpPost]
        [ValidateInput(false)]
        [Authenticate]
        public ActionResult UploadFile(HttpPostedFileBase file, string name, int chunks, int? chunk, long OrderId, long TicketId)
        {
            chunk = chunk ?? 0;

            var order = new Order();

            try
            {
                if ((chunk.Value == 0) || (chunk.Value == chunks - 1))
                {
                    order = Db.Select<Order>(x => x.Where(y => (
                        y.Id == OrderId &&
                        (y.Status == (int)Enum_OrderStatus.Received) || y.Status == (int)Enum_OrderStatus.Verify)).Limit(1)).FirstOrDefault();

                    if (order == null)
                    {
                        return JsonError("Can't find your order or Order status must is received.");
                    }

                    var ticket = Db.Select<Order_UploadFilesTicket>(x => x.Where(y => (y.Id == TicketId && y.OrderId == OrderId && y.UserId == CurrentUser.Id && y.Status == (int)Enum_UploadFilesTicketStatus.Default)).Limit(1)).FirstOrDefault();

                    if (ticket == null)
                    {
                        return JsonError("Can't find upload file request or You don't have permission to execute.");
                    }

                    if (chunk.Value == 0)
                    {
                        var path = Settings.Get(Enum_Settings_Key.WEBSITE_CUSTOMER_UPLOAD_PATH_DEFAULT, System.IO.Path.GetTempPath(), Enum_Settings_DataType.String).ToString();

                        Session["PATH"] = path != null ? path : "/";
                    }
                }

                if ((file != null) && (file.ContentLength != 0))
                {
                    //string _path = System.Web.HttpContext.Current.Server.MapPath(Session["PATH"].ToString());
                    string _path = Session["PATH"].ToString();

                    string _name1 = string.Format("{0}_{1}{2}", OrderId, TicketId, Path.GetExtension(name));

                    // string _name2 = string.Format("{0}_{1}{2}[{3}]", OrderId, TicketId, Path.GetExtension(name), chunk);

                    // file.SaveAs(_path + "/" + _name2);

                    using (var fs = new FileStream(Path.Combine(_path, _name1), chunk == 0 ? FileMode.Create : FileMode.Append))
                    {
                        var buffer = new byte[file.InputStream.Length];

                        file.InputStream.Read(buffer, 0, buffer.Length);

                        fs.Write(buffer, 0, buffer.Length);
                        fs.Close();
                    }

                    if (chunk.Value == chunks - 1)
                    {
                        Session["PATH"] = null;

                        Db.UpdateOnly<Order_UploadFilesTicket>(new Order_UploadFilesTicket()
                        {
                            Status = (int)Enum_UploadFilesTicketStatus.Uploaded,
                            FileName = name,
                            LastUpdate = DateTime.Now
                        }, ev => ev.Update(p => new
                        {
                            p.Status,
                            p.FileName,
                            p.LastUpdate
                        }).Where(m => m.Id == TicketId).Limit(1));

                        Db.UpdateOnly<Order>(new Order()
                        {
                            LastUpdate = DateTime.Now
                        }, ev => ev.Update(p => new
                        {
                            p.LastUpdate
                        }).Where(m => m.Id == order.Id).Limit(1));

                        order.AddHistory(string.Format("Customer uploaded file {0}, request Photobookmart to approve new file update.", name), CurrentWebsite.Name, (int)CurrentWebsite.CreatedBy, false);

                        return JsonSuccess(Url.Action("Index", "Order", new { }), "Your file has been submited to Photobookmart.");
                    }

                    return JsonSuccess(null, "Upload chunk successfull.");
                }
                else
                {
                    return JsonError("There was an error when upload file. Please try again.");
                }
            }
            catch (Exception ex)
            {
                return JsonError("There was an error when upload file: " + ex.Message + ".");
            }
        }
    }
}