using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using HttpContext = System.Web;
using ServiceStack.DataAnnotations;
using ServiceStack.WebHost.Endpoints;
using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Sites;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.OrmLite;
using PhotoBookmart;

namespace PhotoBookmart.DataLayer.Models
{
    /// <summary>
    /// Base of all AB CMS Model
    /// </summary>
    public class BasicModelBase : IDisposable
    {
        static BasicModelBase()
        {
        }

        /* End Public Static */
        IDbConnection db = null;
        IDbConnection dbMisa = null;
        [Ignore]
        protected virtual IDbConnection Db
        {
            get
            {
                if (db == null)
                {
                    if (BasicModelBase.ServiceAppHost != null)
                    {
                        db = BasicModelBase.ServiceAppHost.TryResolve<IDbConnection>();
                    }
                }

                if (db != null && db.State != ConnectionState.Open)
                {
                    // force open new connection
                    db = BasicModelBase.ServiceAppHost.TryResolve<IDbConnectionFactory>().Open();
                }

                return db;
            }
        }


        [Ignore]
        public static IAppHost ServiceAppHost
        {
            get
            {
                var x = ServiceStack.WebHost.Endpoints.Support.HttpListenerBase.Instance as IAppHost;
                if (x != null)
                {
                    return x;
                }
                x = ServiceStack.WebHost.Endpoints.AppHostBase.Instance as IAppHost;
                if (x != null)
                {
                    return x;
                }
                return null;
            }
        }

        [PrimaryKey]
        [AutoIncrement]
        [IgnoreWhenGenerateList]
        public long Id { get; set; }

        public BasicModelBase()
        {

        }

        public void Dispose()
        {
            try
            {
                if (db != null)
                {
                    db.Close();
                }
                if (dbMisa != null)
                {
                    dbMisa.Close();
                }
            }
            catch
            {
            }
            GC.SuppressFinalize(this);
        }

        ~BasicModelBase()
        {
            Dispose();
        }

        /// <summary>
        /// Get the price object according to the price_id
        /// For internal use.
        /// In each of your class, you have to implement it again your self
        /// </summary>
        /// <param name="price_id"></param>
        /// <returns></returns>
        protected List<Price> getPrice(long master_id, Enum_Price_MasterType type, string countrycode = "")
        {
            countrycode = countrycode.ToUpper().Trim();
            if (string.IsNullOrEmpty(countrycode))
            {
                return Db.Select<Price>(x => x.Where(m => m.MasterType == type && m.MasterId == master_id));
            }
            else
            {
                return Db.Select<Price>(x => x.Where(m => m.MasterType == type && m.MasterId == master_id && m.CountryCode == countrycode));
            }
        }

    }
}