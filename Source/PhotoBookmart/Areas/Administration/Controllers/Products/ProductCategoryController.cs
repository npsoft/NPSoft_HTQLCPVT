using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.Areas.Administration.Models;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.Controllers;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;
using PhotoBookmart.Areas.Administration.Controllers;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.DataLayer.Models.Sites;

namespace PhotoBookmart.Areas.Administration.Controllers
{
    [ABRequiresAnyRole(RoleEnum.Administrator)]
    public class ProductCategoryController : WebAdminController
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult List()
        {
            List<Product_Category> c = new List<Product_Category>();
            c = Db.Where<Product_Category>(true);
            var list_users = Cache_GetAllUsers();
            
            foreach (var x in c)
            {
                var z = list_users.Where(m => m.Id == x.CreatedBy);
                if (z.Count() > 0)
                {
                    var k = z.First();
                    if (string.IsNullOrEmpty(k.FullName))
                        x.CreatedByUsername = k.UserName;
                    else
                        x.CreatedByUsername = k.FullName;
                }
                else
                {
                    x.CreatedByUsername = "Deleted user";
                }
            }

            return PartialView("_List", c);
        }

        public ActionResult Add()
        {
            Product_Category model = new Product_Category();
            return View(model);
        }

        public ActionResult Edit(int id)
        {
            var model = Db.Where<Product_Category>(m => m.Id == id).FirstOrDefault();
            if (model == null)
                return Redirect("/");

            return View("Add", model);
        }

        [ValidateInput(false)]
        public ActionResult Update(Product_Category model, IEnumerable<HttpPostedFileBase> FileUp, IEnumerable<HttpPostedFileBase> FileUpGray, string Status, string IsRequireLogin, string IsDisplayHomePage, string ParentId)
        {
            model.Status = string.IsNullOrEmpty(Status) ? false : true;

            model.IsRequireLogin = string.IsNullOrEmpty(IsRequireLogin) ? false : true;

            model.IsDisplayHomePage = string.IsNullOrEmpty(IsDisplayHomePage) ? false : true;

            if (string.IsNullOrEmpty(model.Name))
            {
                ViewBag.Error = "Please enter field \"Name\"";

                return View("Add", model);
            }

            model.GenerateSeoName();

            Product_Category current_item = new Product_Category();
            if (model.Id > 0)
            {
                var z = Db.Where<Product_Category>(m => m.Id == model.Id);
                if (z.Count == 0)
                {
                    // the ID is not exist
                    return JsonError("Please dont try to hack us");
                }
                else
                {
                    current_item = z.First();

                    model.ParentId = string.IsNullOrEmpty(ParentId) ? current_item.ParentId : model.ParentId;
                }
            }

            if (model.Id == 0)
            {
                model.CreatedOn = DateTime.Now;
                model.CreatedBy = AuthenticatedUserID;

                // set Order Menu
                try
                {
                    int OrderMenu = Db.Where<Product_Category>(m => m.ParentId == model.ParentId).Max(m => m.OrderIndex);
                    model.OrderIndex = OrderMenu + 1;
                }
                catch
                {
                    model.OrderIndex = 0;
                }
            }
            else
            {
                model.CreatedOn = current_item.CreatedOn;
                model.CreatedBy = current_item.CreatedBy;
            }

            if (FileUp != null && FileUp.Count() > 0 && FileUp.First() != null)
                model.Filename = UploadFile(AuthenticatedUserID, User.Identity.Name, "ProductCategory", FileUp);
            else
                model.Filename = current_item.Filename;

            if (FileUpGray != null && FileUpGray.Count() > 0 && FileUpGray.First() != null)
                model.Filename_grayscale = UploadFile(AuthenticatedUserID, User.Identity.Name, "ProductCategory", FileUpGray);
            else
                model.Filename_grayscale = current_item.Filename_grayscale;

            if (model.Id == 0)
            {
                Db.Insert<Product_Category>(model);
                return RedirectToAction("Index");
            }
            else
            {
                Db.Update<Product_Category>(model);
                return RedirectToAction("Index");
            }
        }

        public ActionResult Move(int id, int direction)
        {
            try
            {
                var entity = Db.Where<Product_Category>(m => m.Id == id).FirstOrDefault();
                var a = new List<Product_Category>();
                var temp = new Product_Category();

                // get the nearest
                if (direction == 1) // down
                {
                    a = Db.Where<Product_Category>(m =>  m.OrderIndex < entity.OrderIndex).OrderBy(m => m.OrderIndex).ToList();
                    if (a.Count() > 0)
                        temp = a.LastOrDefault();
                }
                else
                {
                    a = Db.Where<Product_Category>(m => m.OrderIndex > entity.OrderIndex ).OrderBy(m => m.OrderIndex).ToList();
                    if (a.Count() > 0)
                        temp = a.FirstOrDefault();
                }

                if (temp.Id > 0)
                {
                    int t = temp.OrderIndex;
                    temp.OrderIndex = entity.OrderIndex;
                    entity.OrderIndex = t;
                    Db.Update<Product_Category>(temp);
                    Db.Update<Product_Category>(entity);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return JsonSuccess("", "Product category order changed");
        }

        public ActionResult Delete(int id)
        {
            try
            {
                Db.DeleteById<Product_Category>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Detail(long id)
        {
            var model = Db.Where<Product_Category>(m => m.Id == id).FirstOrDefault();
            if (model == null)
                return Redirect("/");


            // created by username
            var list_users = Cache_GetAllUsers();

            var zk = list_users.Where(m => m.Id == model.CreatedBy).FirstOrDefault();
            if (zk == null)
            {
                model.CreatedByUsername = "Deleted User";
            }
            else
            {
                if (string.IsNullOrEmpty(zk.FullName))
                    model.CreatedByUsername = zk.UserName;
                else
                    model.CreatedByUsername = zk.FullName;
            }

            return View(model);
        }

        #region  [CONTROLLER] ProductCategory.Image

        public ActionResult List_Image(long pcId)
        {
            List<ProductCategoryImage> x = new List<ProductCategoryImage>();

            x = Db.Where<ProductCategoryImage>(y => (y.ProductCategoryId == pcId)).OrderBy(y => (y.Order)).ToList();

            ViewData["pcId"] = pcId;
            
            return PartialView("_List_Image", x);
        }

        public ActionResult Add_Image(long? pcId)
        {
            ProductCategoryImage model = new ProductCategoryImage() { Id = 0, ProductCategoryId = 0 };

            if (pcId.HasValue)
            {
                var pc = Db.Where<Product_Category>(m => (m.Id == pcId)).FirstOrDefault();

                if (pc != null)
                {
                    model.ProductCategoryId = pc.Id;

                    model.ProductCategoryName = pc.Name;

                    model.Order = model.Get_Order_New();
                }
            }

            if (model.ProductCategoryId > 0)
            {
                return View(model);
            }
            else
            {
                return RedirectToAction("Index", "ProductCategory", new { });
            }
        }

        public ActionResult Edit_Image(int id)
        {
            var models = Db.Where<ProductCategoryImage>(m => m.Id == id);

            if (models.Count == 0)
            {
                return RedirectToAction("Index", "ProductCategory", new { });
            }
            else
            {
                var model = models.First();

                model.ProductCategoryName = Db.Where<Product_Category>(m => (m.Id == model.ProductCategoryId)).First().Name;

                return View("Add_Image", model);
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Update_Image(ProductCategoryImage model, IEnumerable<HttpPostedFileBase> Thumbnail, string IsActive)
        {
            model.IsActive = IsActive != null ? true : false;

            ProductCategoryImage current_item = new ProductCategoryImage();

            if (model.Id > 0)
            {
                var z = Db.Where<ProductCategoryImage>(m => m.Id == model.Id);

                if (z.Count == 0)
                {
                    ViewBag.Error = "Please don't try to hack us";

                    return View("Add_Image", model);
                }
                else
                {
                    current_item = z.First();
                }
            }

            if (string.IsNullOrEmpty(model.Name))
            {
                ViewBag.Error = "Please enter field » Name";

                return View("Add_Image", model);
            }

            if (Thumbnail != null && Thumbnail.Count() > 0 && Thumbnail.First() != null)
            {
                model.Thumbnail = UploadFile(model.Id, CurrentUser.UserName, "_product_category", Thumbnail);
            }
            else
            {
                model.Thumbnail = current_item.Thumbnail;
            }

            if (model.Id == 0)
            {
                model.CreatedOn = DateTime.Now;

                model.CreatedBy = AuthenticatedUserID;
            }
            else
            {
                model.CreatedOn = current_item.CreatedOn;

                model.CreatedBy = current_item.CreatedBy;
            }

            if (model.Id == 0)
            {
                Db.Insert<ProductCategoryImage>(model);
            }
            else
            {
                Db.Update<ProductCategoryImage>(model);
            }

            return RedirectToAction("Detail", "ProductCategory", new { id = model.ProductCategoryId });
        }

        [RequiredRole("Administrator")]
        public ActionResult Delete_Image(int id)
        {
            try
            {
                Db.DeleteById<ProductCategoryImage>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [RequiredRole("Administrator")]
        public ActionResult Status_Image(int id, bool status)
        {
            try
            {
                var x = Db.SelectParam<ProductCategoryImage>(m => (m.Id == id));

                if (x.Count != 0)
                {
                    var y = x.First();

                    y.IsActive = status;

                    Db.Update(y);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [RequiredRole("Administrator")]
        public ActionResult Move_Image(int id, int move)
        {
            try
            {
                var e = Db.SelectParam<ProductCategoryImage>(m => (m.Id == id)).FirstOrDefault();

                var a = new List<ProductCategoryImage>();

                var t = new ProductCategoryImage();

                if (move == 1)
                {
                    a = Db.Where<ProductCategoryImage>(m => (m.ProductCategoryId == e.ProductCategoryId && m.Order < e.Order)).OrderBy(m => (m.Order)).ToList();

                    if (a.Count != 0) t = a.LastOrDefault();
                }
                else
                {
                    a = Db.Where<ProductCategoryImage>(m => (m.ProductCategoryId == e.ProductCategoryId && m.Order > e.Order)).OrderBy(m => (m.Order)).ToList();

                    if (a.Count != 0) t = a.FirstOrDefault();
                }

                if (t.Id > 0)
                {
                    int i = t.Order;

                    t.Order = e.Order;

                    e.Order = i;

                    Db.Update<ProductCategoryImage>(t);

                    Db.Update<ProductCategoryImage>(e);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region  [CONTROLLER] ProductCategory.Material

        public ActionResult List_Material(long pcId)
        {
            var model = Db.Where<ProductCategoryMaterial>(x => (x.ProductCategoryId == pcId)).OrderBy(x => (x.Order)).ToList();

            ViewData["pcId"] = pcId;

            return PartialView("_List_Material", model);
        }

        public ActionResult Add_Material(long? pcId)
        {
            if (!pcId.HasValue) return RedirectToAction("Index", "ProductCategory", new { });

            var pc = Db.Select<Product_Category>(x => x.Where(y => (y.Id == pcId))).FirstOrDefault();

            if (pc == null) return RedirectToAction("Index", "ProductCategory", new { });

            ProductCategoryMaterial model = new ProductCategoryMaterial() {
                Id = 0,
                ProductCategoryId = pc.Id,
                ProductCategoryName = pc.Name
            };

            model.Order = model.GetOrderNewLast();

            if (model.Order == 1) model.IsPresentive = true;

            return View(model);
        }

        public ActionResult Edit_Material(long? id)
        {
            if (!id.HasValue) return RedirectToAction("Index", "ProductCategory", new { });

            var model = Db.Select<ProductCategoryMaterial>(x => x.Where(y => (y.Id == id))).FirstOrDefault();

            if (model == null) return RedirectToAction("Index", "ProductCategory", new { });

            model.ProductCategoryName = model.GetProductCategoryName();

            return View("Add_Material", model);
        }

        public ActionResult Detail_Material(long? id)
        {
            if (!id.HasValue) return RedirectToAction("Index", "ProductCategory", new { });

            var model = Db.Where<ProductCategoryMaterial>(x => (x.Id == id)).FirstOrDefault();

            if (model == null) return RedirectToAction("Index", "ProductCategory", new { });

            model.ProductCategoryName = model.GetProductCategoryName();

            return View(model);
        }

        [HttpPost]
        [ValidateInput(false)]
        [RequiredRole("Administrator")]
        public ActionResult Update_Material(ProductCategoryMaterial model, string IsPresentive, string IsActive)
        {
            model.IsPresentive = IsPresentive != null ? true : false;

            model.IsActive = IsActive != null ? true : false;

            ProductCategoryMaterial modelUpdated = new ProductCategoryMaterial();

            if (model.Id > 0)
            {
                modelUpdated = Db.Select<ProductCategoryMaterial>(x => x.Where(y => (y.Id == model.Id))).FirstOrDefault();

                if (modelUpdated == null)
                {
                    ViewBag.Error = "Please don't try to hack us";

                    return View("Add_Material", model);
                }
            }

            if (string.IsNullOrEmpty(model.Name))
            {
                ViewBag.Error = "Please enter field » Name";

                return View("Add_Material", model);
            }

            model.SEO = model.GetSEO();

            if (model.Id == 0)
            {
                model.CreatedBy = AuthenticatedUserID;

                model.CreatedOn = DateTime.Now;

                Db.Insert<ProductCategoryMaterial>(model);

                model.Id = Db.GetLastInsertId();

                SetOrderMaterial(model.Id, model.Order);

                SetPresentiveMaterial(model.Id, model.IsPresentive);
            }
            else
            {
                modelUpdated.Name = model.Name;

                modelUpdated.SEO = model.SEO;

                modelUpdated.Desc = model.Desc;

                modelUpdated.Order = model.Order;

                modelUpdated.IsPresentive = model.IsPresentive;

                modelUpdated.IsActive = model.IsActive;

                modelUpdated.LastUpdatedBy = AuthenticatedUserID;

                modelUpdated.LastUpdatedOn = DateTime.Now;

                Db.Update<ProductCategoryMaterial>(modelUpdated);

                SetOrderMaterial(modelUpdated.Id, modelUpdated.Order);

                SetPresentiveMaterial(modelUpdated.Id, modelUpdated.IsPresentive);
            }

            return RedirectToAction("Detail", "ProductCategory", new { id = model.ProductCategoryId });
        }

        [RequiredRole("Administrator")]
        public ActionResult Delete_Material(long id)
        {
            try
            {
                Db.DeleteById<ProductCategoryMaterial>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [RequiredRole("Administrator")]
        public ActionResult Active_Material(long id, bool active)
        {
            try
            {
                var material = Db.Select<ProductCategoryMaterial>(x => x.Where(y => (y.Id == id))).FirstOrDefault();

                if (material != null && material.IsActive != active)
                {
                    material.IsActive = active;

                    Db.Update(material);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [RequiredRole("Administrator")]
        public ActionResult Move_Material(long id, int move)
        {
            try
            {
                var e = Db.SelectParam<ProductCategoryMaterial>(m => (m.Id == id)).FirstOrDefault();

                var a = new List<ProductCategoryMaterial>();

                var t = new ProductCategoryMaterial();

                if (move == 1)
                {
                    a = Db.Where<ProductCategoryMaterial>(m => (m.ProductCategoryId == e.ProductCategoryId && m.Order < e.Order)).OrderBy(m => (m.Order)).ToList();

                    if (a.Count != 0) t = a.LastOrDefault();
                }
                else
                {
                    a = Db.Where<ProductCategoryMaterial>(m => (m.ProductCategoryId == e.ProductCategoryId && m.Order > e.Order)).OrderBy(m => (m.Order)).ToList();

                    if (a.Count != 0) t = a.FirstOrDefault();
                }

                if (t.Id > 0)
                {
                    int i = t.Order;

                    t.Order = e.Order;

                    e.Order = i;

                    Db.Update<ProductCategoryMaterial>(t);

                    Db.Update<ProductCategoryMaterial>(e);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [RequiredRole("Administrator")]
        public ActionResult Presentive_Material(long id, bool isPresentive)
        {
            Dictionary<string, object> dic = SetPresentiveMaterial(id, isPresentive);

            if (!(bool)dic["IsSuccess"]) return JsonError((string)dic["Message"]);

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        public void SetOrderMaterial(long id, int order)
        {
            try
            {
                var pcId = Db.Select<ProductCategoryMaterial>(x => x.Where(y => (y.Id == id))).First().ProductCategoryId;

                var isSame = Db.Select<ProductCategoryMaterial>(x => x.Where(y => (y.ProductCategoryId == pcId && y.Order == order && y.Id != id))).Count != 0;

                if (!isSame) return;

                var materials = Db.Select<ProductCategoryMaterial>(x => x.Where(y => (y.ProductCategoryId == pcId && y.Order >= order && y.Id != id)));

                for (int i = 0; i < materials.Count; i++)
                {
                    var item = materials[i];

                    item.Order += 1;

                    Db.Update<ProductCategoryMaterial>(item);
                }
            }
            catch (Exception) { }
        }

        public Dictionary<string, object> SetPresentiveMaterial(long id, bool isPresentive)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();

            dic.Add("IsSuccess", false); dic.Add("Message", null);

            try
            {
                var x = Db.Where<ProductCategoryMaterial>(m => m.Id == id).FirstOrDefault();

                if (x != null)
                {
                    if (isPresentive)
                    {
                        Db.UpdateOnly<ProductCategoryMaterial>(new ProductCategoryMaterial() { IsPresentive = false }, ev => ev.Update(p => p.IsPresentive).Where(m => (m.ProductCategoryId == x.ProductCategoryId && m.Id != x.Id && m.IsPresentive != false)));
                    }

                    Db.UpdateOnly<ProductCategoryMaterial>(new ProductCategoryMaterial() { IsPresentive = isPresentive }, ev => ev.Update(p => p.IsPresentive).Where(m => (m.Id == x.Id && m.IsPresentive != isPresentive)));

                    dic["IsSuccess"] = true;
                }
                else
                {
                    dic["Message"] = "Material not found!";
                }
            }
            catch (Exception ex)
            {
                dic["Message"] = ex.Message;
            }

            return dic;
        }

        #endregion

        #region  [CONTROLLER] ProductCategory.MaterialDetail

        public ActionResult List_MaterialDetail(long materialId)
        {
            var model = Db.Where<ProductCategoryMaterialDetail>(x => (x.ProductCategoryMaterialId == materialId)).OrderBy(x => (x.Order)).ToList();

            ViewData["materialId"] = materialId;

            return PartialView("_List_MaterialDetail", model);
        }

        public ActionResult Add_MaterialDetail(long? materialId)
        {
            if (!materialId.HasValue) return RedirectToAction("Index", "ProductCategory", new { });

            var material = Db.Select<ProductCategoryMaterial>(x => x.Where(y => (y.Id == materialId))).FirstOrDefault();

            if (material == null) return RedirectToAction("Index", "ProductCategory", new { });

            ProductCategoryMaterialDetail model = new ProductCategoryMaterialDetail()
            {
                Id = 0,
                ProductCategoryMaterialId = material.Id,
                ProductCategoryMaterialName = material.Name
            };

            model.Order = model.GetOrderNewLast();

            model.ProductCategoryName = model.GetProductCategoryName();

            if (model.Order == 1) model.IsPresentive = true;

            return View(model);
        }

        public ActionResult Edit_MaterialDetail(long? id)
        {
            if (!id.HasValue) return RedirectToAction("Index", "ProductCategory", new { });

            var model = Db.Select<ProductCategoryMaterialDetail>(x => x.Where(y => (y.Id == id))).FirstOrDefault();

            if (model == null) return RedirectToAction("Index", "ProductCategory", new { });

            model.ProductCategoryName = model.GetProductCategoryName();

            return View("Add_MaterialDetail", model);
        }

        public ActionResult Detail_MaterialDetail(long? id)
        {
            if (!id.HasValue) return RedirectToAction("Index", "ProductCategory", new { });

            var model = Db.Where<ProductCategoryMaterialDetail>(x => (x.Id == id)).FirstOrDefault();

            if (model == null) return RedirectToAction("Index", "ProductCategory", new { });

            return View(model);
        }

        [HttpPost]
        [ValidateInput(false)]
        [RequiredRole("Administrator")]
        public ActionResult Update_MaterialDetail(ProductCategoryMaterialDetail model, IEnumerable<HttpPostedFileBase> Thumbnail, string IsPresentive, string IsActive)
        {
            model.IsPresentive = IsPresentive != null ? true : false;

            model.IsActive = IsActive != null ? true : false;

            ProductCategoryMaterialDetail modelUpdated = new ProductCategoryMaterialDetail();

            if (model.Id > 0)
            {
                modelUpdated = Db.Select<ProductCategoryMaterialDetail>(x => x.Where(y => (y.Id == model.Id))).FirstOrDefault();

                if (modelUpdated == null)
                {
                    ViewBag.Error = "Please don't try to hack us";

                    return View("Add_MaterialDetail", model);
                }
            }

            if (string.IsNullOrEmpty(model.Name))
            {
                ViewBag.Error = "Please enter field » Name";

                return View("Add_MaterialDetail", model);
            }

            if (Thumbnail != null && Thumbnail.Count() > 0 && Thumbnail.First() != null)
            {
                model.Thumbnail = UploadFile(model.Id, CurrentUser.UserName, "category_material", Thumbnail);
            }
            else
            {
                model.Thumbnail = modelUpdated.Thumbnail;
            }

            if (model.Id == 0)
            {
                model.CreatedBy = AuthenticatedUserID;

                model.CreatedOn = DateTime.Now;

                Db.Insert<ProductCategoryMaterialDetail>(model);

                model.Id = Db.GetLastInsertId();

                SetOrderMaterialDetail(model.Id, model.Order);

                SetPresentiveMaterialDetail(model.Id, model.IsPresentive);
            }
            else
            {
                modelUpdated.Name = model.Name;

                modelUpdated.Thumbnail = model.Thumbnail;

                modelUpdated.Order = model.Order;

                modelUpdated.IsPresentive = model.IsPresentive;

                modelUpdated.IsActive = model.IsActive;

                modelUpdated.LastUpdatedBy = AuthenticatedUserID;

                modelUpdated.LastUpdatedOn = DateTime.Now;

                Db.Update<ProductCategoryMaterialDetail>(modelUpdated);

                SetOrderMaterialDetail(modelUpdated.Id, modelUpdated.Order);

                SetPresentiveMaterialDetail(modelUpdated.Id, modelUpdated.IsPresentive);
            }

            return RedirectToAction("Detail_Material", "ProductCategory", new { id = model.ProductCategoryMaterialId });
        }

        [RequiredRole("Administrator")]
        public ActionResult Delete_MaterialDetail(long id)
        {
            try
            {
                Db.DeleteById<ProductCategoryMaterialDetail>(id);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [RequiredRole("Administrator")]
        public ActionResult Active_MaterialDetail(long id, bool active)
        {
            try
            {
                var material = Db.Select<ProductCategoryMaterialDetail>(x => x.Where(y => (y.Id == id))).FirstOrDefault();

                if (material != null && material.IsActive != active)
                {
                    material.IsActive = active;

                    Db.Update(material);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [RequiredRole("Administrator")]
        public ActionResult Move_MaterialDetail(long id, int move)
        {
            try
            {
                var e = Db.SelectParam<ProductCategoryMaterialDetail>(m => (m.Id == id)).FirstOrDefault();

                var a = new List<ProductCategoryMaterialDetail>();

                var t = new ProductCategoryMaterialDetail();

                if (move == 1)
                {
                    a = Db.Where<ProductCategoryMaterialDetail>(m => (m.ProductCategoryMaterialId == e.ProductCategoryMaterialId && m.Order < e.Order)).OrderBy(m => (m.Order)).ToList();

                    if (a.Count != 0) t = a.LastOrDefault();
                }
                else
                {
                    a = Db.Where<ProductCategoryMaterialDetail>(m => (m.ProductCategoryMaterialId == e.ProductCategoryMaterialId && m.Order > e.Order)).OrderBy(m => (m.Order)).ToList();

                    if (a.Count != 0) t = a.FirstOrDefault();
                }

                if (t.Id > 0)
                {
                    int i = t.Order;

                    t.Order = e.Order;

                    e.Order = i;

                    Db.Update<ProductCategoryMaterialDetail>(t);

                    Db.Update<ProductCategoryMaterialDetail>(e);
                }
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [RequiredRole("Administrator")]
        public ActionResult Presentive_MaterialDetail(long id, bool isPresentive)
        {
            Dictionary<string, object> dic = SetPresentiveMaterialDetail(id, isPresentive);

            if (!(bool)dic["IsSuccess"]) return JsonError((string)dic["Message"]);

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        public void SetOrderMaterialDetail(long id, int order)
        {
            try
            {
                var materialId = Db.Select<ProductCategoryMaterialDetail>(x => x.Where(y => (y.Id == id))).First().ProductCategoryMaterialId;

                var isSame = Db.Select<ProductCategoryMaterialDetail>(x => x.Where(y => (y.ProductCategoryMaterialId == materialId && y.Order == order && y.Id != id))).Count != 0;

                if (!isSame) return;

                var materials = Db.Select<ProductCategoryMaterialDetail>(x => x.Where(y => (y.ProductCategoryMaterialId == materialId && y.Order >= order && y.Id != id)));

                for (int i = 0; i < materials.Count; i++)
                {
                    var item = materials[i];

                    item.Order += 1;

                    Db.Update<ProductCategoryMaterialDetail>(item);
                }
            }
            catch (Exception) { }
        }

        public Dictionary<string, object> SetPresentiveMaterialDetail(long id, bool isPresentive)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();

            dic.Add("IsSuccess", false); dic.Add("Message", null);

            try
            {
                var x = Db.Where<ProductCategoryMaterialDetail>(m => m.Id == id).FirstOrDefault();

                if (x != null)
                {
                    if (isPresentive)
                    {
                        Db.UpdateOnly<ProductCategoryMaterialDetail>(new ProductCategoryMaterialDetail() { IsPresentive = false }, ev => ev.Update(p => p.IsPresentive).Where(m => (m.ProductCategoryMaterialId == x.ProductCategoryMaterialId && m.Id != x.Id && m.IsPresentive != false)));
                    }

                    Db.UpdateOnly<ProductCategoryMaterialDetail>(new ProductCategoryMaterialDetail() { IsPresentive = isPresentive }, ev => ev.Update(p => p.IsPresentive).Where(m => (m.Id == x.Id && m.IsPresentive != isPresentive)));

                    dic["IsSuccess"] = true;
                }
                else
                {
                    dic["Message"] = "Material detail not found!";
                }
            }
            catch (Exception ex)
            {
                dic["Message"] = ex.Message;
            }

            return dic;
        }

        #endregion
    }
}