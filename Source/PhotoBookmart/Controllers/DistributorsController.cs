using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RBVMC.DataLayer.Models;
using System.Xml;
using System.Data;
using System.Web.Security;
using ServiceStack.Mvc;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using RBVMC.Common.Helpers;
using RealBonusWeb.Areas.Administration.Models;
using RBVMC.DataLayer.Models.Users_Management;
using RBVMC.DataLayer.Models.Sites;
using ServiceStack.Common.Web;
using RealBonusWeb.Models;

namespace RealBonusWeb.Controllers
{
    public class DistributorsController : RBVMCController
    {
        public ActionResult Index(string id)
        {
            return View();
        }

        /// <summary>
        /// Show Distributor widget on homepage footer
        /// </summary>
        /// <returns></returns>
        [ChildActionOnly]
        public ActionResult FooterDistributorWidget()
        {
            // get region area
            var dis = Cache_GetAllDistributors().Where(m=>m.Status).ToList();

            // country / state
            var country = new List<string>();
            var region = new List<string>();
            var state = new List<StateByCountryModel>();

            foreach (var item in dis)
            {
                region = region.Union(item.Region).ToList();
                //
                if (country.Where(m => m.ToLower() == item.Country.ToLower()).Count() == 0)
                {
                    country.Add(item.Country);
                }
                //
                if (state.Where(m => m.Country.ToLower() == item.Country.ToLower() && m.State.ToLower() == item.State.ToLower()).Count() == 0)
                {
                    state.Add(new StateByCountryModel() { Country=item.Country.ToLower(), State=item.State, Accurate = !item.AddressIsNotAccurate, City=item.City});
                }
            }

            country = country.OrderBy(m => m).ToList();
            ViewData["Regions"] = region;
            ViewData["Country"] = country;
            ViewData["State"] = state;

            return View();
        }

        /// <summary>
        /// Called by Distributor Widget, search by state
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult SearchByState(string state, string country)
        { 
            // get region area
            var dis = Cache_GetAllDistributors().Where(m => m.Status && m.Country.ToLower() == country.ToLower() && m.State.ToLower() == state.ToLower()).ToList();
            ViewData["by_state"] = country + "/" + state;
            ViewData["search_term"] = country + "/" + state;
            return View(dis);
        }

        /// <summary>
        /// Called by Distributor Widget, search by region
        /// </summary>
        [HttpGet]
        public ActionResult SearchByRegion(string region)
        {
            var dis = Cache_GetAllDistributors().Where(m => m.Status && m.Region.Contains(region)).ToList();

            ViewData["search_term"] = region;
            return View("SearchByState",dis);
        }

    }
}