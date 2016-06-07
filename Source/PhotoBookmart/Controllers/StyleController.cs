using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.DataLayer.Models;
using System.Data;
using ServiceStack.Mvc;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.Products;
using ServiceStack.Common.Web;
using PhotoBookmart.Models;

namespace PhotoBookmart.Controllers
{
    public class StyleController : BaseController
    {
        public ActionResult Index()
        {
            var model = Db.Where<Product_Category>(x => (x.Status && (!x.IsRequireLogin || (x.IsRequireLogin && User.Identity.IsAuthenticated))));

            return View(model);
        }

        [ChildActionOnly]
        public ActionResult Material(long styleId)
        {
            var model = Db.Where<ProductCategoryMaterial>(x => (x.IsActive && x.ProductCategoryId == styleId)).OrderBy(x => (x.Order)).ToList();

            return View("_Material_List", model);
        }

        [ChildActionOnly]
        public ActionResult MaterialDetail(long materialId)
        {
            var model = Db.Where<ProductCategoryMaterialDetail>(x => (x.IsActive && x.ProductCategoryMaterialId == materialId)).OrderBy(x => (x.Order)).ToList();

            return View("_MaterialDetail_List", model);
        }

        public ActionResult Pricing()
        {
            var model = Db.Where<Product_Category>(x => (x.Status && (!x.IsRequireLogin || (x.IsRequireLogin && User.Identity.IsAuthenticated)))).OrderBy(x => (x.OrderIndex)).ToList();

            return View(model);
        }

        [ChildActionOnly]
        public ActionResult Pricing_ProductDetail(long catId)
        {
            var model = Db.Where<Product>(x => (x.Status && x.CatId == catId)).OrderBy(x => (x.Order)).ToList();

            //var country = Setting_GetCurrentCountry();

            //ViewData["Country"] = country;
            return View("Pricing_ProductDetail", model);
        }

        public ActionResult PaymentShipping()
        {
            var model = Db.Select<Product_Category>(x => x.Where(y => (y.Status && (!y.IsRequireLogin || (y.IsRequireLogin && User.Identity.IsAuthenticated)))).OrderBy(z => (z.OrderIndex)));

            return View(model);
        }

        [ChildActionOnly]
        public ActionResult PaymentShipping_ProductDetail(long catId)
        {
            var model = Db.Select<Product>(x => x.Where(y => (y.Status && y.CatId == catId)).OrderBy(z => (z.Order)));

            //var country = Setting_GetCurrentCountry();

            //ViewData["Country"] = country;

            return View("PaymentShipping_ProductDetail", model);
        }

        public ActionResult Detail(string id)
        {
            var model = Db.Select<Product_Category>(x => x.Where(y => (y.Status && y.SeoName == id)).Limit(1)).FirstOrDefault();

            if (model != null)
            {
                ViewData["Prev"] = Db.Select<Product_Category>(x => x.Where(y => (y.Status && y.OrderIndex <= model.OrderIndex && y.Id != model.Id)).OrderByDescending(z => (z.OrderIndex)).Limit(1)).FirstOrDefault();

                ViewData["Next"] = Db.Select<Product_Category>(x => x.Where(y => (y.Status && y.OrderIndex >= model.OrderIndex && y.Id != model.Id)).OrderBy(z => (z.OrderIndex)).Limit(1)).FirstOrDefault();

                ViewData["Images"] = Db.Select<ProductCategoryImage>(x => x.Where(y => (y.IsActive && y.ProductCategoryId == model.Id)));

                return View("Detail", model);
            }
            else
            {
                throw new HttpException(404, "Photobook style \"" + id + "\" not found!", 0);
            }
        }
    }
}