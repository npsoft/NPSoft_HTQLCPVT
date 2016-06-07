using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Products
{
    [Alias("DanhMuc_KhaNangPhucVu")]
    [Schema("DanhMuc")]
    public partial class DanhMuc_KhaNangPhucVu : BasicModelBase
    {
        public Guid IDKhaNangPV { get; set; }
        public string TenKhaNangPV { get; set; }
    }

    [Schema("Products")]
    public partial class Product_Option : ModelBase
    {
        public string Name { get; set; }

        public string Thumbnail { get; set; }

        public string InternalName { get; set; }

        /// <summary>
        /// Text area
        /// </summary>
        public string Desc { get; set; }

        public bool Status { get; set; }

        //Option code
        public string Code { get; set; }

        public string UnitName { get; set; }

        /// <summary>
        /// Weight of the option, in grams
        /// </summary>
        [Default(0)]
        public double Weight { get; set; }

        public Product_Option()
        {

        }

        public Price getPrice(Enum_Price_MasterType type = Enum_Price_MasterType.ProductOption, string countrycode = "MY")
        {
            var price = base.getPrice(Id, type, countrycode);
            Price ret = null;
            if (price.Count > 0)
            {
                if (string.IsNullOrEmpty(countrycode))
                {
                    // find RM, Malaysia
                    ret = price.Where(x => x.CountryCode == "MY").FirstOrDefault();
                    if (ret == null)
                    {
                        ret = price.FirstOrDefault();
                    }
                }
                else
                {
                    ret = price.FirstOrDefault();
                }
            }
            else
            {
                ret = new Price();
            }
            Db.Close();
            return ret;
        }
    }
}
