using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using ServiceStack.OrmLite;
using PhotoBookmart.DataLayer.Models.MMO;

namespace PhotoBookmart.Controllers
{
    public class MMOSvcController : BaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            return Content("MMO Service by npe.etc@gmail.com!");
        }

        [HttpGet]
        public ActionResult RetrieveMisa()
        {
            return Content("Retrieve data from Misa database!");
        }

        [HttpPost]
        public ActionResult ExtractCaptcha(IEnumerable<HttpPostedFileBase> Imgs, string Type, string From, long TimeOut = 60 * 1000)
        {
            ExtractCaptchaRes res = new ExtractCaptchaRes();
            try
            {
                if (Imgs != null && Imgs.Count() != 0)
                {
                    HttpPostedFileBase img = Imgs.First();
                    if (img != null && img.ContentLength > 0)
                    {
                        var ext = Path.GetExtension(img.FileName);
                        if (new string[6] { ".gif", ".jpg", ".png", ".jpeg", ".bmp", ".pdf" }.Contains(ext.ToLower()))
                        {
                            string mmo_imgs_dir = string.Format("{0}", ConfigurationManager.AppSettings["MMO_Imgs_Dir"]);
                            string name = string.Format("{0:yyMMdd-HHmmss}-{1}{2}", DateTime.Now, DateTime.Now.ToFileTime(), ext);
                            string path = Path.Combine(Server.MapPath(string.Format("~/{0}", mmo_imgs_dir)), name);
                            img.SaveAs(path);

                            MMO_Imgs model = new MMO_Imgs();
                            model.Type = Type;
                            model.From = From;
                            model.TimeOut = TimeOut;
                            model.NameOrg = img.FileName;
                            model.PathFTP = Path.Combine(mmo_imgs_dir, name);
                            model.Status = "NEW";
                            model.Content = null;
                            model.CreatedOn = DateTime.Now;
                            model.CreatedBy = 1;
                            model.LastModifiedOn = DateTime.Now;
                            model.LastModifiedBy = 1;
                            using (IDbTransaction dbTrans = Db.OpenTransaction())
                            {
                                Db.Save(model);
                                dbTrans.Commit();
                            }
                            model.Id = Db.GetLastInsertId();
                            res.Status.ErrCode = "-5";
                            do
                            {
                                MMO_Imgs mmo_img = Db.Select<MMO_Imgs>(x => x.Where(y => y.Id == model.Id).Limit(0, 1)).FirstOrDefault();
                                if (new string[1] { "SUCCESS" }.Contains(mmo_img.Status))
                                {
                                    res.Content = mmo_img.Content;
                                    res.Status.ErrCode = ErrCodeDefine.Success;
                                    break;
                                }
                                if (new string[1] { "FAILURE" }.Contains(mmo_img.Status))
                                {
                                    res.Status.ErrCode = "-6";
                                    break;
                                }
                                TimeOut -= 1000;
                                Thread.Sleep(1000);
                            } while (TimeOut > 0);
                        }
                        else
                        {
                            res.Status.ErrCode = "-4";
                        }
                    }
                    else
                    {
                        res.Status.ErrCode = "-3";
                    }
                }
                else
                {
                    res.Status.ErrCode = "-2";
                }
            }
            catch (Exception ex)
            {
                res.Status.ErrCode = "-1";
                res.Status.Msg = ex.Message;
                res.Status.StackTrace = ex.StackTrace;
            }
            finally { }
            return Content(JsonConvert.SerializeObject(res));
        }
    }

    #region Service: Common class
    public class ResStatus
    {
        public string ErrCode;
        public string Msg;
        public string StackTrace;

        public ResStatus() { }
    }

    public class ErrCodeDefine
    {
        public static string Success
        {
            get
            {
                return "1";
            }
        }
        public static string UnknowErr
        {
            get
            {
                return "0";
            }
        }
        public static ResStatus ResSuccess
        {
            get
            {
                ResStatus resStatus = new ResStatus();
                resStatus.ErrCode = Success;
                resStatus.Msg = "";
                resStatus.StackTrace = "";
                return resStatus;
            }
        }
    }

    public class ResModelBase
    {
        public ResStatus Status;

        public ResModelBase()
        {
            Status = ErrCodeDefine.ResSuccess;
        }
    }

    public abstract class ReqModelPaging
    {
        public int PageIndex;
        public int PageSize;

        public ReqModelPaging() { }
    }

    public abstract class ResModelPaging : ResModelBase
    {
        public int PageIndex;
        public int PageSize;
        public int TotalPage;
        public int TotalItem;

        public ResModelPaging() { }

        public void FetchingPaging()
        {
            PageSize = PageSize > 0 ? PageSize : int.MaxValue;
            TotalPage = (int)Math.Ceiling((double)TotalItem / PageSize);
            PageIndex = PageIndex > 0 && PageIndex < TotalPage + 1 ? PageIndex : 1;
        }
    }
    #endregion

    #region Service: Define class
    public class ExtractCaptchaRes : ResModelBase
    {
        public string Content;

        public ExtractCaptchaRes() { }
    }
    #endregion

    #region Service: Object class
    #endregion
}
