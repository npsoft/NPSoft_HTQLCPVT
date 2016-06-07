using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.DataLayer.Models.Products
{
    [Alias("DanhMuc_TinhTrangDT")]
    [Schema("DanhMuc")]
    public partial class DanhMuc_TinhTrangDT : BasicModelBase
    {
        public string MaTT { get; set; }
        public string TenTT { get; set; }

        public DanhMuc_TinhTrangDT() { }
    }

    #region [Products].[ProductCategoryMaterial]

    [Alias("ProductCategoryMaterial")]
    [Schema("Products")]
    public partial class ProductCategoryMaterial : ModelBase
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

        public bool IsPresentive { get; set; }

        [Default(0)]
        [ForeignKey(typeof(Product_Category), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public long ProductCategoryId { get; set; }

        [Ignore]
        public string CreatedByName { get; set; }

        [Ignore]
        public string LastUpdatedByName { get; set; }

        [Ignore]
        public string ProductCategoryName { get; set; }

        public ProductCategoryMaterial()
        {
            Id = 0;

            IsActive = true;

            CreatedBy = 0;

            CreatedOn = DateTime.Now;

            LastUpdatedBy = null;

            LastUpdatedOn = null;

            Order = 1;

            IsPresentive = false;
        }

        public string GetSEO()
        {
            string seo = this.SEO, random = "";

            do
            {
                if (string.IsNullOrEmpty(seo)) seo = this.Name + random;

                seo = seo.ToSeoUrl();
                
                if (Db.Count<ProductCategoryMaterial>(x => (x.SEO == seo && x.Id != this.Id)) == 0) break;

                seo = "";

                random = "_" + random.GenerateRandomText(3);

            } while (true);

            return seo;
        }

        public int GetOrderNewLast()
        {
            var last = Db.Select<ProductCategoryMaterial>(x => x.Where(y => (y.ProductCategoryId == ProductCategoryId))).OrderBy(x => (x.Order)).LastOrDefault();

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
            var cat = Db.Select<Product_Category>(x => (x.Id == this.ProductCategoryId));

            return cat.Count != 0 ? cat.First().Name : null;
        }
    }

    #endregion
}
