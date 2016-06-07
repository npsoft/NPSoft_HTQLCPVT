using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Products
{
    /// <summary>
    /// For Billing And Shipping Address
    /// </summary>
    [Schema("Products")]
    public partial class AddressModel : BasicModelBase
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Company { get; set; }

        public string Country { get; set; }

        public string City { get; set; }

        public string Address { get; set; }

        public string ZipPostalCode { get; set; }

        public string PhoneNumber { get; set; }

        public string FaxNumber { get; set; }

        public string State { get; set; }

        public DateTime CreatedOn { get; set; }

        public AddressModel()
        {

        }

        /// <summary>
        /// Return country name
        /// </summary>
        /// <returns></returns>
        public string GetCountryName()
        {
            var c = Db.Select<Models.System.Country>(x => x.Where(m => m.Code == Country).Limit(1)).FirstOrDefault();
            Db.Close();
            if (c == null)
            {
                return Country;
            }
            else
            {
                return c.Name;
            }
        }
    }
}
