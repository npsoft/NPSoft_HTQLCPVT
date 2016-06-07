using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.DataLayer.Models.Products
{
    [Alias("DanhMuc_LoaiDT")]
    [Schema("DanhMuc")]
    public partial class DanhMuc_LoaiDT : BasicModelBase
    {
        public string MaLDT { get; set; }
        public string TenLDT { get; set; }
        public int Order { get; set; }
        public double? HeSo { get; set; }
        public DateTime? NgayAP { get; set; }

        public DanhMuc_LoaiDT() { }
    }

    [Alias("ProductCategoryImage")]
    [Schema("Products")]
    public partial class ProductCategoryImage : ModelBase
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        public string SEO { get; set; }

        public string Name { get; set; }

        public string Desc { get; set; }

        public string Lang { get; set; }

        public bool IsActive { get; set; }

        public long CreatedBy { get; set; }
        
        public DateTime CreatedOn { get; set; }

        public long? LastUpdatedBy { get; set; }

        public DateTime? LastUpdatedOn { get; set; }

        public int Order { get; set; }

        [Default(0)]
        [ForeignKey(typeof(Product_Category), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public long ProductCategoryId { get; set; }

        [Ignore]
        public string ProductCategoryName { get; set; }

        public string Thumbnail { get; set; }

        public bool IsPresentive { get; set; }

        public ProductCategoryImage()
        {
            Id = 0;

            IsActive = true;

            CreatedBy = 0;

            CreatedOn = DateTime.Now;

            LastUpdatedBy = null;

            LastUpdatedOn = null;

            Order = 0;

            IsPresentive = false;
        }

        public string Get_CreatedBy_Name()
        {
            var user = Db.SelectParam<ABUserAuth>(o => (o.Id == this.CreatedBy));
            Db.Close();
            return user.Count != 0 ? user.First().DisplayName : "Deleted user";
        }

        public int Get_Order_New()
        {
            var last = Db.Select<ProductCategoryImage>(x => x.Where(y => (y.ProductCategoryId == ProductCategoryId))).OrderBy(x => (x.Order)).LastOrDefault();

            if (last != null)
            {
                return last.Order + 1;
            }
            else
            {
                return 1;
            }
            Db.Close();
        }
    }
}
