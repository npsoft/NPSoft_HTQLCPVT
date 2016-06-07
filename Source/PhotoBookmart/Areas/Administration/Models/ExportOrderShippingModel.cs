using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using ServiceStack.Text;
using OfficeOpenXml;

using PhotoBookmart.DataLayer.Models.Products;

namespace PhotoBookmart.Areas.Administration.Models
{
    public partial class ExportOrderShippingModel
    {
        public Enum_ShippingType Shipping_Method { get; set; }

        /// <summary>
        /// Accept shipping and production_sheet
        /// </summary>
        public string ExportResult { get; set; }

        public List<long> Orders { get; set; }

        public ExportOrderShippingModel()
        {
            Shipping_Method = Enum_ShippingType.Other;

            Orders = new List<long>();
        }
    }

    public partial class ExportOrderShippingXSLAramexModel
    {
        public string Buyer_Fullname { get; set; }

        public string Buyer_Phone_Number { get; set; }

        public string Buyer_Email { get; set; }

        public string Buyer_Address_1 { get; set; }

        public string Buyer_Address_2 { get; set; }

        public string Buyer_Address_1Buyer_Address_2 { get; set; }

        public string Buyer_City { get; set; }

        public string Buyer_State { get; set; }

        public string Buyer_Zip { get; set; }

        public string Buyer_Country { get; set; }

        public string Item_Title { get; set; }

        public string Sale_Date { get; set; }

        public string Transaction_ID { get; set; }

        public ExportOrderShippingXSLAramexModel() { }

        public static byte[] ExportToArrByte(List<Order> orders)
        {
            using (var package = new ExcelPackage())
            {
                package.Workbook.Worksheets.Add(string.Format("{0:yyyy-MM-dd}", DateTime.Now));
                ExcelWorksheet ws = package.Workbook.Worksheets[1];
                ws.Name = "Orders Shipping Information";
                ws.Cells.Style.Font.Size = 12;
                ws.Cells.Style.Font.Name = "Calibri";

                List<string> ws_header = new List<string>();
                ws_header.Add("");
                ws_header.Add("Buyer Fullname");
                ws_header.Add("Buyer Phone Number");
                ws_header.Add("Buyer Email");
                ws_header.Add("Buyer Address 1");
                ws_header.Add("Buyer Address 2");
                ws_header.Add("Buyer Address 1Buyer Address 2");
                ws_header.Add("Buyer City");
                ws_header.Add("Buyer State");
                ws_header.Add("Buyer Zip");
                ws_header.Add("Buyer Country");
                ws_header.Add("Item Title");
                ws_header.Add("Value");
                ws_header.Add("Transaction ID");

                int row_index = 1;
                for (int i = 1; i < ws_header.Count; i++)
                {
                    ws.Cells[row_index, i].Value = ws_header[i];
                    ws.Cells[row_index, i].Style.Font.Bold = true;
                    ws.Cells[row_index, i].Style.Font.Size = 14;
                }

                foreach (var item in orders ?? Enumerable.Empty<Order>())
                {
                    item.LoadAddress(0);
                    item.LoadAddress(1);

                    row_index++;
                    var col_index = 1;
                    // Buyer Fullname
                    ws.Cells[row_index, col_index].Value = string.Format("{0} {1}", item.ShippingAddressModel.FirstName, item.ShippingAddressModel.LastName);

                    col_index++;
                    // Buyer Phone Number
                    string phoneNum = item.ShippingAddressModel.PhoneNumber;
                    if (!string.IsNullOrEmpty(phoneNum) && phoneNum[0] == '+')
                    {
                        phoneNum = phoneNum.Substring(1, phoneNum.Length - 1);
                        phoneNum = string.Format("{0}{1}", "00", phoneNum);
                    }
                    ws.Cells[row_index, col_index].Value = phoneNum;

                    col_index++;
                    // Buyer Email
                    ws.Cells[row_index, col_index].Value = item.ShippingAddressModel.Email;

                    col_index++;
                    // Buyer Address 1
                    ws.Cells[row_index, col_index].Value = item.ShippingAddressModel.Address;

                    col_index++;
                    // Buyer Address 2
                    ws.Cells[row_index, col_index].Value = null;

                    col_index++;
                    // Buyer Address 1Buyer Address 2
                    ws.Cells[row_index, col_index].Value = string.Format("{0} {1}", item.ShippingAddressModel.Address, null);

                    col_index++;
                    // Buyer City
                    ws.Cells[row_index, col_index].Value = item.ShippingAddressModel.City;

                    col_index++;
                    // Buyer State
                    if (!string.IsNullOrEmpty(item.ShippingAddressModel.State))
                    {
                        ws.Cells[row_index, col_index].Value = item.ShippingAddressModel.State;
                    }
                    else
                    {
                        ws.Cells[row_index, col_index].Value = "";
                    }

                    col_index++;
                    // Buyer Zip
                    ws.Cells[row_index, col_index].Value = item.ShippingAddressModel.ZipPostalCode;

                    col_index++;
                    // Buyer Country
                    ws.Cells[row_index, col_index].Value = item.ShippingAddressModel.GetCountryName();

                    col_index++;
                    // Item Title
                    ws.Cells[row_index, col_index].Value = string.Format("{0}-{1}", item.Id.ToString("00000"), item.Product_Name);

                    col_index++;
                    // Sale Date
                    //ws.Cells[row_index, col_index].Value = string.Format("{0:MMM-dd-yy}", item.CreatedOn);
                    // replace sale date by value, always put value 20
                    ws.Cells[row_index, col_index].Value = 20;

                    col_index++;
                    // Transaction ID
                    //ws.Cells[row_index, col_index].Value = item.Payment_AuthorizationTransactionId;
                    // wait for the instruction of the transaction id
                    ws.Cells[row_index, col_index].Value = "";
                }

                for (int k = 1; k < ws_header.Count + 2; k++)
                {
                    ws.Column(k).AutoFit();
                }

                return package.GetAsByteArray();
            }
        }
    }

    public partial class ExportOrderShippingCsvTNTModel
    {
        //    public string SHORT_REF { get; set; }
        public string COMPANY_NAME { get; set; }

        public string ADDRESS_1 { get; set; }

        public string ADDRESS_2 { get; set; }

        public string ADDRESS_3 { get; set; }

        public string CITY { get; set; }

        public string POSTCODE { get; set; }

        public string PROVINCE { get; set; }

        public string COUNTYCODE { get; set; }

        public string TELEPHONE { get; set; }

        public string CONTACT { get; set; }

        public string QUANTITY { get; set; }

        public string WEIGHT { get; set; }

        public string SERVICE_CODE { get; set; }

        public string SPECIAL_INSTRUCTION { get; set; }

        public string CUSTOMER_REFERENCE { get; set; }

        public string EMAIL { get; set; }

        public ExportOrderShippingCsvTNTModel() { }

        /// <summary>
        /// process the string to maximum length
        /// </summary>
        /// <param name="st"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        static string String_Limit(string st, int length)
        {
            if (string.IsNullOrEmpty(st))
            {
                st = "";
            }

            if (st.Length <= length)
            {
                return st;
            }
            else
            {
                return st.Substring(0, length);
            }
        }

        public static byte[] ExportToArrByte(List<Order> orders)
        {
            List<ExportOrderShippingCsvTNTModel> l = new List<ExportOrderShippingCsvTNTModel>();

            foreach (var o in orders ?? Enumerable.Empty<Order>())
            {
                o.LoadAddress(0);
                o.LoadAddress(1);

                var s = new ExportOrderShippingCsvTNTModel();
                // Company Name (50 characters)
                s.COMPANY_NAME = String_Limit((string.IsNullOrEmpty(o.ShippingAddressModel.Company) ? o.ShippingAddressModel.FirstName + " " + o.ShippingAddressModel.LastName : o.ShippingAddressModel.Company), 50);

                //Address line one (30 characters)
                s.ADDRESS_1 = String_Limit(o.ShippingAddressModel.Address, 30);

                // address 2 (30 characters)
                s.ADDRESS_2 = "";

                // address 3 (30 characters)
                s.ADDRESS_3 = "";

                // city (30 characters)
                s.CITY = String_Limit(o.ShippingAddressModel.City, 30);

                // Postcode (9 characters)
                s.POSTCODE = String_Limit(o.ShippingAddressModel.ZipPostalCode, 9);

                // Province(30 characters) = State
                s.PROVINCE = String_Limit(o.ShippingAddressModel.State, 30);

                // Country Code(3 characters)
                s.COUNTYCODE = String_Limit(o.ShippingAddressModel.Country, 3);

                // Telephone (16 characters)
                string phoneNum = o.ShippingAddressModel.PhoneNumber;
                if (!string.IsNullOrEmpty(phoneNum) && phoneNum[0] == '+')
                {
                    phoneNum = phoneNum.Substring(1, phoneNum.Length - 1);
                    phoneNum = string.Format("{0}{1}", "00", phoneNum);
                }
                s.TELEPHONE = String_Limit(phoneNum, 16);

                // Contact( 22 characters)
                s.CONTACT = String_Limit(o.ShippingAddressModel.FirstName + " " + o.ShippingAddressModel.LastName, 22);

                // Quantity (99 pieces max)
                s.QUANTITY = "1";

                // weight
                s.WEIGHT = o.TotalWeight.ToString("0.##");

                // Service Code 
                s.SERVICE_CODE = "";

                // Special Instruction
                s.SPECIAL_INSTRUCTION = "";

                // Customer Referrence
                s.CUSTOMER_REFERENCE = o.Order_Number;

                // email
                s.EMAIL = o.Customer_Email;

                l.Add(s);
            }

            string str = CsvSerializer.SerializeToString(l);

            return Encoding.UTF8.GetBytes(str);
        }
    }
}