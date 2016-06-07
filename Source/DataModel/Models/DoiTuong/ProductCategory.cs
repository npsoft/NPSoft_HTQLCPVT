using System;
using System.Linq;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Products
{
    [Alias("Product_Category")]
    [Schema("Products")]
    public class Product_Category : ModelBase
    {
        public Product_Category()
        {
            CreatedOn = DateTime.Now;

            Status = true;

            IsRequireLogin = false;

            IsDisplayHomePage = true;
        }

        public string Name { get; set; }

        [Default(0)]
        public int OrderIndex { get; set; }

        [Default(0)]
        public long ParentId { get; set; }

        public bool Status { get; set; }

        public string SeoName { get; set; }

        public string Filename { get; set; }

        public string Filename_grayscale { get; set; }

        public string Description { get; set; }

        public bool IsRequireLogin { get; set; }

        public bool IsDisplayHomePage { get; set; }

        public string DescShort { get; set; }

        public string Cover { get; set; }

        public string PaperType { get; set; }

        public string NoOfPages { get; set; }

        public string BindingType { get; set; }

        public string Printing { get; set; }

        public string EndSheets { get; set; }

        /// <summary>
        /// Short name
        /// </summary>
        public string ShortCode { get; set; }

        /// <summary>
        /// Generate Seoname for this object
        /// </summary>
        public void GenerateSeoName()
        {
            // get parent seoname first
            List<string> parent_name = new List<string>();
            var parent_id = this.ParentId;
            while (parent_id > 0)
            {
                var x = Db.IdOrDefault<Product_Category>(parent_id);
                if (x == null)
                {
                    break;
                }
                parent_name.Insert(0, x.Name);
                parent_id = x.ParentId;
            }
            var pname = parent_name.Join(";");

            // generate seo name
            string random = "";
            do
            {
                if (string.IsNullOrEmpty(this.SeoName))
                {
                    this.SeoName = pname + ";" + this.Name + random;
                    this.SeoName = this.SeoName.ToSeoUrl();
                }
                else
                {
                    this.SeoName = this.SeoName.ToSeoUrl();
                }

                // check exist
                if (Db.Count<Product_Category>(m => m.SeoName == this.SeoName && m.Id != this.Id) == 0)
                {
                    break;
                }

                random = "_" + random.GenerateRandomText(3);
                this.SeoName = "";
            } while (0 < 1);
        }

        /// <summary>
        /// Return all Categories belongs to this current category
        /// </summary>
        /// <returns></returns>
        public List<Product_Category> GetSubCategories()
        {
            if (this.Id == 0)
            {
                return new List<Product_Category>(); // nothing to search
            }

            // get list of categories.
            var items = Db.Where<Product_Category>(true);
            //
            List<Product_Category> data = new List<Product_Category>();
            List<long> temp = new List<long>() { this.Id };
            // loop into inside of this cat, while the temp list still has items
            while (temp.Count > 0)
            {
                var f = temp.First();
                temp.RemoveAt(0);
                //var f_item = items.Where(m => m.Id == f).FirstOrDefault();
                //if (f_item == null)
                //{
                //    continue;
                //}

                // insert into data this item first
                //data.Add(f_item);
                // then search for sub
                var x = items.Where(m => m.ParentId == f);
                //insert back to temp for next loop
                foreach (var k in x)
                {
                    temp.Add(k.Id);
                    data.Add(k);
                }

                if (temp.Count == 0)
                {
                    break;
                }
            }
            return data;
        }
    }
}