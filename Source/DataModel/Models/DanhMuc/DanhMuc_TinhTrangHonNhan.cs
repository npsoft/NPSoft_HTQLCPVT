using System;
using System.Linq;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Products
{
    [Alias("DanhMuc_TinhTrangHonNhan")]
    [Schema("DanhMuc")]
    public partial class DanhMuc_TinhTrangHonNhan : BasicModelBase
    {
        public Guid IDTinhTrangHN { get; set; }
        public string TenTinhTrangHN { get; set; }
    }

    [Alias("Product_Images")]
    [Schema("Products")]
    public partial class Product_Images : ModelBase
    {
        [Default(0)]
        [ForeignKey(typeof(Product), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public long ProductId { get; set; }
        [Ignore]
        public string Product_Name { get; set; }

        public bool Status { get; set; }

        public string Name { get; set; }

        public string Filename { get; set; }

        public Product_Images()
        {
            CreatedOn = DateTime.Now;
        }
    }
}
