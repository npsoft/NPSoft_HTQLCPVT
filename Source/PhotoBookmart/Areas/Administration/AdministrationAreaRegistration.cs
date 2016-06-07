using System.Web.Mvc;

namespace PhotoBookmart.Areas.Administration
{
    public class AdministrationAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Administration";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            try
            {

                context.MapRoute(null, "Administration/connector", new { action = "FileManager_Index", controller = "Theme" }, new string[] { "PhotoBookmart.Areas.Administration.Controllers" });
                context.MapRoute(null, "Administration/Thumbnails/{tmb}", new { controller = "Theme", action = "FileManager_Thumbs", tmb = UrlParameter.Optional }, new string[] { "PhotoBookmart.Areas.Administration.Controllers" });

                context.MapRoute(
                    "Administration_default",
                    "Administration/{controller}/{action}/{id}",
                    new { action = "Index", controller = "Management", id = UrlParameter.Optional }, new string[] { "PhotoBookmart.Areas.Administration.Controllers" }
                );

            }
            catch
            {
            }
        }
    }
}
