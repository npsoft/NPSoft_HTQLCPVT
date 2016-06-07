using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack.Common;
using ServiceStack.OrmLite;
using System.IO;
//using iTextSharp.text;
//using iTextSharp.text.pdf;
//using iTextSharp.text.html.simpleparser;
using PhotoBookmart.DataLayer.Models.Products;
using System.Web.Mvc;
using System.Web.Routing;
using System.Net;
using System.Data;

namespace PhotoBookmart.Lib
{
    /// <summary>
    /// Base library
    /// </summary>
    public class BaseLib : IDisposable
    {
        IDbConnection _connection = null;
        public virtual IDbConnection Db
        {
            get
            {
                if (_connection == null)
                {
                    _connection = AppHost.Resolve<IDbConnection>();

                }

                if (_connection != null && _connection.State != ConnectionState.Open)
                {
                    // force open new connection
                    _connection = AppHost.Resolve<IDbConnectionFactory>().Open();
                }

                return _connection;
            }
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
            }
            GC.SuppressFinalize(this);
        }
       
        #region Utilities
        /// <summary>
        /// Render a view
        /// </summary>
        /// <param name="view"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        protected string RenderView(string view, object model)
        {
            ViewDataDictionary viewData = new ViewDataDictionary();
            viewData.Model = model;

            // generate fake controller context
            var context = new HttpContextWrapper(HttpContext.Current);
            var routeData = new RouteData();
            routeData.Values.Add("controller", "Base");
            var controllerContext = new ControllerContext(new RequestContext(context, routeData), new PhotoBookmart.Areas.Administration.Controllers.WebAdminController());
            TempDataDictionary tempData = new TempDataDictionary();

            using (StringWriter sw = new StringWriter())
            {
                ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(controllerContext, view);

                ViewContext viewContext = new ViewContext(controllerContext, viewResult.View, viewData, tempData, sw);
                viewResult.View.Render(viewContext, sw);
                return WebUtility.HtmlDecode(sw.GetStringBuilder().ToString());
            }
        }

//        /// <summary>
//        /// Create PDf based on the HTML input
//        /// </summary>
//        /// <param name="html"></param>
//        /// <returns></returns>
//        protected MemoryStream CreatePdf(string html)
//        {
//            //MemoryStream msOutput = new MemoryStream();
//            //TextReader reader = new StringReader(html);

//            //// step 1: creation of a document-object
//            //Document document = new Document(PageSize.A4, 30, 30, 30, 30);

//            //// step 2:
//            //// we create a writer that listens to the document
//            //// and directs a XML-stream to a file
//            //PdfWriter writer = PdfWriter.GetInstance(document, msOutput);

//            //// step 3: we create a worker parse the document
//            //HTMLWorker worker = new HTMLWorker(document);

//            //// step 4: we open document and start the worker on the document
//            //document.Open();
//            //worker.StartDocument();

//            //// step 5: parse the html into the document
//            //worker.Parse(reader);

//            //// step 6: close the document and the worker
//            //worker.EndDocument();
//            //worker.Close();
//            //document.Close();

//            //return msOutput;

//            html = @"<?xml version=""1.0"" encoding=""UTF-8""?>
//                 <!DOCTYPE html 
//                     PUBLIC ""-//W3C//DTD XHTML 1.0 Strict//EN""
//                    ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"">
//                 <html xmlns=""http://www.w3.org/1999/xhtml"" xml:lang=""en"" lang=""en"">
//                  <body>
//                    " + html + "</body></html>";
//            var output = new MemoryStream(); // this MemoryStream is closed by FileStreamResult

//            var document = new iTextSharp.text.Document(iTextSharp.text.PageSize.LETTER, 50, 50, 50, 50);
//            var writer = PdfWriter.GetInstance(document, output);
//            writer.CloseStream = false;
//            document.Open();
//            List<IElement> htmlarraylist = HTMLWorker.ParseToList(new StringReader(html), null);
//            for (int k = 0; k < htmlarraylist.Count; k++)
//            {
//                document.Add((IElement)htmlarraylist[k]);
//            }
//            document.Close();
//            document.CloseDocument();

//            return output;
//        }
        #endregion

    }
}