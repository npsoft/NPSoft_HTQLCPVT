using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.DataLayer.Models.Products
{
    [Alias("DanhMuc_MucDoKhuyetTat")]
    [Schema("DanhMuc")]
    public partial class DanhMuc_MucDoKhuyetTat : BasicModelBase
    {
        public Guid IDMucDoKT { get; set; }
        public string TenMucDoKT { get; set; }

        public DanhMuc_MucDoKhuyetTat() { }
    }

    #region [Products].[ProductCategoryMaterialDetail]

    [Alias("ProductCategoryMaterialDetail")]
    [Schema("Products")]
    public partial class ProductCategoryMaterialDetail : ModelBase
    {
        public string Name { get; set; }

        public bool IsActive { get; set; }
        
        public long? LastUpdatedBy { get; set; }

        public DateTime? LastUpdatedOn { get; set; }

        public int Order { get; set; }

        [Default(0)]
        [ForeignKey(typeof(ProductCategoryMaterial), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public long ProductCategoryMaterialId { get; set; }

        public string Thumbnail { get; set; }

        public bool IsPresentive { get; set; }

        [Ignore]
        public string CreatedByName { get; set; }

        [Ignore]
        public string LastUpdatedByName { get; set; }

        [Ignore]
        public string ProductCategoryName { get; set; }

        [Ignore]
        public string ProductCategoryMaterialName { get; set; }

        public ProductCategoryMaterialDetail()
        {
            Id = 0;

            CreatedBy = 0;

            CreatedOn = DateTime.Now;

            LastUpdatedBy = null;

            LastUpdatedOn = null;

            Order = 1;
        }

        public int GetOrderNewLast()
        {
            var last = Db.Select<ProductCategoryMaterialDetail>(x => x.Where(y => (y.ProductCategoryMaterialId == this.ProductCategoryMaterialId))).OrderBy(x => (x.Order)).LastOrDefault();

            if (last != null)
            {
                return last.Order + 1;
            }
            else
            {
                return 1;
            }
        }

        public string GetCreatedByName()
        {
            var user = Db.Select<ABUserAuth>(x => (x.Id == this.CreatedBy));

            return user.Count != 0 ? user.First().DisplayName : null;
        }

        public string GetLastUpdatedByName()
        {
            if (LastUpdatedBy.HasValue)
            {
                var user = Db.Select<ABUserAuth>(x => (x.Id == this.LastUpdatedBy.Value));

                return user.Count != 0 ? user.First().DisplayName : null;
            }

            return null;
        }

        public string GetProductCategoryName()
        {
            var material = Db.Select<ProductCategoryMaterial>(x => (x.Id == this.ProductCategoryMaterialId)).FirstOrDefault();

            return material != null ? material.GetProductCategoryName() : null;
        }

        public string GetProductCategoryMaterialName()
        {
            var material = Db.Select<ProductCategoryMaterial>(x => (x.Id == this.ProductCategoryMaterialId));

            return material.Count != 0 ? material.First().Name : null;
        }
    }

    #endregion
}
