using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;

namespace PhotoBookmart
{
    /// <summary>
    /// Copyright Trung Dang (trungdt@absoft.vn)
    /// To control and dispose the page render view
    /// </summary>
    public class ThemableRazorView : RazorView, IDisposable
    {

        public ThemableRazorView(ControllerContext controllerContext, string viewPath, string layoutPath, bool runViewStartPages, IEnumerable<string> viewStartFileExtensions)
            : base(controllerContext, viewPath, layoutPath, runViewStartPages, viewStartFileExtensions)
        {
        }

        public ThemableRazorView(ControllerContext controllerContext, string viewPath, string layoutPath, bool runViewStartPages, IEnumerable<string> viewStartFileExtensions, IViewPageActivator viewPageActivator)
            : base(controllerContext, viewPath, layoutPath, runViewStartPages, viewStartFileExtensions, viewPageActivator)
        {

        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        protected override void RenderView(ViewContext viewContext, TextWriter writer, object instance) 
        {
            base.RenderView(viewContext, writer, instance);
            try
            {
                var x = instance as IDisposable;
                if (x != null)
                {
                    x.Dispose();
                }
            }
            catch(Exception ex)
            {
                var x = ex.Message;
            }
        }

    }
}