using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack.Common;
using ServiceStack.OrmLite;
using System.IO;
//using iTextSharp.text;
//using iTextSharp.text.pdf;
//using iTextSharp.text.html.simpleparser;
using PhotoBookmart.DataLayer.Models.Products;
using System.Web.Mvc;
using System.Web.Routing;
using System.Net;
using PhotoBookmart.Lib.Barcode;
using System.Drawing;
using PhotoBookmart.DataLayer.Models.Users_Management;
using System.Data;
using iTextSharp.text.pdf;
using iTextSharp.text;
using PhotoBookmart.DataLayer.Models.System;

namespace PhotoBookmart.Lib
{
    /// <summary>
    /// Library to process all things relative to order
    /// </summary>
    public class OrderLib : BaseLib, IDisposable
    {
        public Order Order { get; private set; }



        public OrderLib(Order order)
        {
            this.Order = order;
        }

        public void Dispose()
        {
            base.Dispose();
        }

        #region Static Functions
        /// <summary>
        /// Get list of available roles that can be apply to filter orders
        /// </summary>
        /// <returns></returns>
        public static List<Enum_OrderStatus> _GetUserAvailableRoleToCheckOrder(List<string> Roles)
        {
            List<Enum_OrderStatus> ret = new List<Enum_OrderStatus>();
            foreach (var x in Roles)
            {
                // convert it to OrderStatus first
                bool can_convert = false;
                Enum_OrderStatus o = Enum_OrderStatus.Received;
                switch (x)
                {
                    case "Received":
                        can_convert = true;
                        o = Enum_OrderStatus.Received;
                        break;
                    case "Verify":
                        can_convert = true;
                        o = Enum_OrderStatus.Verify;
                        break;
                    case "Processing":
                        can_convert = true;
                        o = Enum_OrderStatus.Processing;
                        break;
                    case "QC_AfterFileProcessing":
                        can_convert = true;
                        o = Enum_OrderStatus.QC_AfterFileProcessing;
                        break;
                    case "Printing":
                        can_convert = true;
                        o = Enum_OrderStatus.Printing;
                        break;
                    case "QC_AfterFilePriting":
                        can_convert = true;
                        o = Enum_OrderStatus.QC_AfterFilePriting;
                        break;
                    case "Production":
                        can_convert = true;
                        o = Enum_OrderStatus.Production;
                        break;
                    case "Shipping":
                        can_convert = true;
                        o = Enum_OrderStatus.Shipping;
                        break;
                    case "Finished":
                        can_convert = true;
                        o = Enum_OrderStatus.Finished;
                        break;
                    default:
                        can_convert = false;
                        break;
                }

                if (can_convert && !ret.Contains(o))
                {
                    ret.Add(o);
                }
            }
            return ret;
        }

        #endregion

        #region Public functions

        /// <summary>
        /// Mark order has been finished
        /// Send email to say thanks
        /// </summary>
        public void CloseOrder()
        {

        }

        /// <summary>
        /// Generate barcode 
        /// </summary>
        /// <returns></returns>
        public System.Drawing.Image Barcode(string order_numer = "", double scale = 1.1d)
        {
            if (string.IsNullOrEmpty(order_numer))
            {
                order_numer = Order.Order_Number;
            }
            int barcode_width = (int)(Code128Rendering.EstimateWidth(order_numer, 2, false) * scale);
            System.Drawing.Image image = new System.Drawing.Bitmap(barcode_width, 70);

            Graphics g = Graphics.FromImage(image);
            g.Clear(Color.White);
            System.Drawing.Font font = new System.Drawing.Font("Tahoma", 14, FontStyle.Bold);
            StringFormat center_align = new StringFormat();
            center_align.Alignment = StringAlignment.Center;

            var y = Code128Rendering.MakeBarcodeImage(order_numer, 2, false, ref g, 1, barcode_width);

            y += 3;
            g.DrawString(Order.Order_Number, font, Brushes.Black, barcode_width / 2, y, center_align);

            font.Dispose();
            g.Dispose();
            return image;
        }

        ///// <summary>
        ///// Return the Invoice in PDF
        ///// </summary>
        ///// <returns></returns>
        //public FileContentResult ExportToPdf()
        //{
        //    string filename = string.Format("Invoice {0} - {1} - Photobookmart", Order.Order_Number, Order.CreatedOn.ToString("ddd, MMM d, yyyy"));
        //    var content = "<html><head></head><body>" + RenderView("InvoiceDetail", Order) + "</body></html>";
        //    var stream = CreatePdf(content);
        //    // mimetype from http://stackoverflow.com/questions/4212861/what-is-a-correct-mime-type-for-docx-pptx-etc

        //    var pdf = new FileContentResult(stream.ToArray(), "application/pdf");
        //    pdf.FileDownloadName = filename;
        //    return pdf;
        //}

        /// <summary>
        /// Generate the invoice and send it to email
        /// </summary>
        public void Invoice_SendEmail()
        {
            try
            {
                string body = RenderView("InvoiceDetail", Order);
                string title = string.Format("Invoice {0} - {1} - Photobookmart", Order.Order_Number, Order.CreatedOn.ToString("ddd, MMM d, yyyy"));

                Order.LoadAddress(0);
                PhotoBookmart.Common.Helpers.SendEmail.SendMail(Order.BillingAddressModel.Email, title, body, "", "");
            }
            catch (Exception ex)
            {
                throw new Exception("Send Invoice Email Error. " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Open ticket for this order by the user
        /// </summary>
        /// <param name="user_id"></param>
        public void ProductionJobSheet_Add(int user_id)
        {
            Order_ProductionJobSheet j = new Order_ProductionJobSheet() { OnDate = DateTime.Now, Order_Id = Order.Id, Step = Order.Status, User_Id = user_id };
            Db.Insert<Order_ProductionJobSheet>(j);
            Db.Dispose();
        }

        /// <summary>
        /// Return True if this order can be approved to get next step from Status Received
        /// </summary>
        /// <returns></returns>
        public bool CheckBeforeApproveForStatusReceived()
        {
            if (Order.Status == (int)Enum_OrderStatus.Received)
            {
                if (!(Order.Payment_isPaid && Order.PaymentStatusEnum == Enum_PaymentStatus.Paid))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Return True if this order can be approved to get next step from Status Shipping
        /// </summary>
        /// <returns></returns>
        public bool CheckBeforeApproveForStatusShipping()
        {
            if (Order.Status == (int)Enum_OrderStatus.Shipping)
            {
                if (!(!string.IsNullOrEmpty(Order.Shipping_TrackingNumber) && Order.Shipping_Status == Enum_ShippingStatus.Shipped))
                    return false;
            }
            return true;
        }


        #region Print Individual Sheet

        private List<string> TruncOptsByOrderId(long orderId)
        {
            List<string> result = new List<string>();
            var data_options = Db.Where<Order_ProductOptionUsing>(x => (x.Order_Id == orderId));
            var data = new List<string>();
            data_options.ForEach(x =>
            {
                data.Add(x.Option_Name);
            });
            if (data != null)
            {
                string[] keysTrunc = new string[] { "Fine silk", "Premium textured", "Superior", "Priority" };
                string[] keysHidden = new string[] { "Additional Page" };
                foreach (var d in data)
                {
                    bool isHidden = false;
                    foreach (var h in keysHidden)
                    {
                        if (d.ToLower().IndexOf(h.ToLower()) >= 0) { isHidden = true; break; }
                    }
                    if (isHidden) { continue; }

                    string valTrunc = d;
                    foreach (var t in keysTrunc)
                    {
                        if (d.ToLower().IndexOf(t.ToLower()) >= 0) { valTrunc = t; break; }
                    }
                    result.Add(valTrunc);
                }
            }
            return result;
        }

        void AddOutline(PdfWriter writer, string Title, float Position)
        {
            PdfDestination destination = new PdfDestination(PdfDestination.FITH, Position);
            PdfOutline outline = new PdfOutline(writer.DirectContent.RootOutline, destination, Title);
            writer.DirectContent.AddOutline(outline, "Name = " + Title);
        }

        public MemoryStream IndividualSheet_PDfGenerator(string extra, List<Order> orders)
        {
            MemoryStream PDFData = new MemoryStream();
            Document document = new Document(PageSize.A5.Rotate(), 50f, 50f, 50f, 50f);

            PdfWriter PDFWriter = PdfWriter.GetInstance(document, PDFData);
            PDFWriter.ViewerPreferences = PdfWriter.PageModeUseOutlines;
            document.Open();

            int index = 0;
            //var dir = Server.MapPath("~/Content/product_barcode/");
            foreach (var order in orders)
            {
                var product = Db.Select<Product>(x => x.Where(m => m.Id == order.Product_Id).Limit(1)).FirstOrDefault();
                if (product == null) { continue; }
                var product_cat = Db.Select<Product_Category>(m => m.Where(x => x.Id == product.CatId).Limit(1)).FirstOrDefault();
                var poptions = Db.Select<Order_ProductOptionUsing>(m => m.Where(x => x.Order_Id == order.Id && x.Option_Name.Contains(extra)).Limit(1)).FirstOrDefault();
                if (poptions != null) { product.Pages += poptions.Option_Quantity; }

                if (index != 0) { document.NewPage(); }
                //// New outline must be created after the page is added
                //AddOutline(PDFWriter, string.Format("{0}-{1}", index + 1, order.Order_Number), document.PageSize.Height);

                PdfPTable _table = new PdfPTable(2);
                iTextSharp.text.Font my_font = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 18, iTextSharp.text.Font.NORMAL);

                #region Order No

                PdfPCell _cell1 = new PdfPCell(new Phrase(string.Format("Order No: {0}", order.Order_Number), my_font));
                _cell1.VerticalAlignment = 0;
                _cell1.HorizontalAlignment = 0;
                _cell1.Border = 0;
                _table.AddCell(_cell1);

                #endregion

                #region Bar Code

                PdfPCell _cell2;

                Order.Order_Number = order.Order_Number;
                iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(Barcode(order.Order_Number, 1.1), System.Drawing.Imaging.ImageFormat.Jpeg);
                img.ScaleAbsolute(img.Width * 1.2f, img.Height * 1.2f);
                _cell2 = new PdfPCell(img);

                _cell2.VerticalAlignment = 1;
                _cell2.HorizontalAlignment = 2;
                _cell2.Border = 0;
                _table.AddCell(_cell2);

                #endregion

                #region Size

                PdfPCell _cell3 = new PdfPCell(new Phrase(string.Format("Size: {0}", product.Size), my_font));
                _cell3.Colspan = 2;
                _cell3.HorizontalAlignment = 0;
                _cell3.PaddingTop = 10;
                _cell3.Border = 0;
                _table.AddCell(_cell3);

                #endregion

                #region Oriental

                PdfPCell _cell4 = new PdfPCell(new Phrase(string.Format("Oriental: {0}", product.Orientation), my_font));
                _cell4.Colspan = 2;
                _cell4.HorizontalAlignment = 0;
                _cell4.PaddingTop = 10;
                _cell4.Border = 0;
                _table.AddCell(_cell4);

                #endregion

                #region Style

                PdfPCell _cell5 = new PdfPCell(new Phrase(string.Format("Style: {0}", product_cat.ShortCode), my_font));
                _cell5.Colspan = 2;
                _cell5.HorizontalAlignment = 0;
                _cell5.PaddingTop = 10;
                _cell5.Border = 0;
                _table.AddCell(_cell5);

                #endregion

                #region Pages

                PdfPCell _cell6 = new PdfPCell(new Phrase(string.Format("Pages: {0}", product.Pages), my_font));
                _cell6.Colspan = 2;
                _cell6.HorizontalAlignment = 0;
                _cell6.PaddingTop = 10;
                _cell6.Border = 0;
                _table.AddCell(_cell6);

                #endregion

                #region Cover Material

                string valCell7 = "";
                if (order.CoverMaterial == 0) { valCell7 = product_cat.ShortCode; }
                else
                {
                    var material = Db.Select<ProductCategoryMaterialDetail>(x => x.Where(m => m.Id == order.CoverMaterial).Limit(1)).FirstOrDefault();
                    if (material != null)
                    {
                        var mcat = Db.Select<ProductCategoryMaterial>(x => x.Where(m => m.Id == material.ProductCategoryMaterialId).Limit(1)).FirstOrDefault();
                        var name = "";
                        if (mcat != null) { name = mcat.Name + " - "; }
                        name += material.Name;
                        valCell7 = name.ToUpper();
                        material.Dispose();
                        mcat.Dispose();
                    }
                }
                PdfPCell _cell7 = new PdfPCell(new Phrase(string.Format("Cover Material: {0}", valCell7), my_font));
                _cell7.Colspan = 2;
                _cell7.HorizontalAlignment = 0;
                _cell7.PaddingTop = 10;
                _cell7.Border = 0;
                _table.AddCell(_cell7);

                #endregion

                #region Country

                Country country = order.GetCountry();
                PdfPCell _cell8 = new PdfPCell(new Phrase(string.Format("Country: {0}", country != null ? country.Name : ""), my_font));
                _cell8.Colspan = 2;
                _cell8.HorizontalAlignment = 0;
                _cell8.PaddingTop = 10;
                _cell8.Border = 0;
                _table.AddCell(_cell8);

                #endregion

                #region Options

                PdfPCell _cell9 = new PdfPCell(new Phrase("Options: " + string.Join(", ", TruncOptsByOrderId(order.Id)), my_font));
                _cell9.Colspan = 2;
                _cell9.HorizontalAlignment = 0;
                _cell9.PaddingTop = 10;
                _cell9.Border = 0;
                _table.AddCell(_cell9);

                #endregion

                if (product != null) product.Dispose();
                if (product_cat != null) product_cat.Dispose();
                if (poptions != null) poptions.Dispose();

                document.Add(_table);
                index++;
            }
            /* TODO:...
            document.Add(new Paragraph("\r\n"));*/
            document.Close();
            return PDFData;
        }
        #endregion

        #endregion
    }
}