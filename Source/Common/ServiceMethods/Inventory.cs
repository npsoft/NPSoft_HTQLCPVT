using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

/// Methods to communicate with Products / Products Categories
namespace ProsperOnline.ServiceMethods.Inventory
{
    #region Product Categories
    [Route("/Inventory_GetCategories", "POST")]
    [Api("Get all product categories by kiosk context")]
    public class Inventory_GetCategories : IReturn<Inventory_GetCategoriesResponse>
    {

    }

    /// <summary>
    /// Prepresent Active Product Categories
    /// </summary>
    public class Inventory_Categorie
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ParentId { get; set; }
        public string Filename { get; set; }
        public string Thumb_BannerImage { get; set; }
        public string Description { get; set; }
    }

    public class Inventory_GetCategoriesResponse
    {
        public ResponseStatus Status { get; set; }
        public List<Inventory_Categorie> Categories { get; set; }

        public Inventory_GetCategoriesResponse()
        {
            Status = new ResponseStatus() { ErrorCode = "" };
        }
    }

    #endregion


    #region Products
    [Route("/Inventory_GetProducts", "POST")]
    [Api("Get all products by kiosk context")]
    public class Inventory_GetProducts : IReturn<Inventory_GetProductsResponse>
    {

    }

    public class Inventory_ProductImage
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Filename { get; set; }
    }

    /// <summary>
    /// Prepresent  Products
    /// </summary>
    public class Inventory_Product
    {
        public int Id { get; set; }
        public bool Status { get; set; }
        public string Name { get; set; }
        public List<string> Tag { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public float Price { get; set; }
        public bool PriceDontShow { get; set; }
        public bool PriceCallForPrice { get; set; }
        public string Manufacturer_Name { get; set; }
        public string Manufacturer_Website { get; set; }
        public bool ShowOnHomepage { get; set; }
        /// <summary>
        /// List of categories this product belong to
        /// </summary>
        public List<int> Categories { get; set; }
        public List<Inventory_ProductImage> Images { get; set; }
    }

    public class Inventory_GetProductsResponse
    {
        public ResponseStatus Status { get; set; }
        public List<Inventory_Product> Products { get; set; }

        public Inventory_GetProductsResponse()
        {
            Status = new ResponseStatus() { ErrorCode = "" };
        }
    }
    #endregion
}
