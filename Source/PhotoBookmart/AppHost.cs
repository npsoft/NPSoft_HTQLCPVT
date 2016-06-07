using System;
using System.Linq;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Text;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;
using System.Configuration;
/* TODO:...
using ServiceStack.Redis;*/
using ServiceStack.Common.Utils;
using PhotoBookmart.ServiceInterface;
using PhotoBookmart.Common.ServiceStackHelper.ORM;
using PhotoBookmart.Common.ServiceStackHelper.Provider;
using ServiceStack.OrmLite.SqlServer;
using PhotoBookmart.DataLayer.Models;
using ServiceStack.Mvc;
using System.Web.Mvc;
using System.Data;

namespace PhotoBookmart
{
    //The ServiceStack AppHost 
    public class AppHost : AppHostBase, IDisposable
    {
        public static IDbConnection DatabaseConnection;
        public static OrmLiteConnectionFactory dbFactory;

        public AppHost() //Tell ServiceStack the name and where to find your web services  
            : base("ABSoft Software Solution - Local CMS API", typeof(LocalAPI).Assembly) { }

        public override void Configure(Funq.Container container)
        {
            var  host = ConfigurationManager.AppSettings.Get("PaypalWebsiteURL");
            SetConfig(new EndpointHostConfig
            {
                DebugMode = false, // Debugmode for stacktrace

                GlobalResponseHeaders =
                    {
                        { "Access-Control-Allow-Origin", "*" },
                        { "Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS" }
                    },
                //EnableFeatures = Feature.All.Remove(GetDisabledFeatures()),
                EnableFeatures = ServiceStack.ServiceHost.Feature.All,
                //RestrictAllCookiesToDomain = "www.photobookmart.com"
                //ServiceStackHandlerFactoryPath = "api",
                //AppendUtf8CharsetOnContentTypes = new HashSet<string> { ContentType.Html },
            });

            //Set JSON web services to return idiomatic JSON camelCase properties
            ServiceStack.Text.JsConfig.EmitCamelCaseNames = false;

            //Register a external dependency-free 
            //container.Register<ICacheClient>(new MemoryCacheClient());
            /* TODO:...
            var redis_host = ConfigurationManager.AppSettings.Get("RedisHost");
            var redis_db = long.Parse(ConfigurationManager.AppSettings.Get("RedisDBVersion"));
            var pooled_redis = new PooledRedisClientManager(redis_db, redis_host);
            pooled_redis.NamespacePrefix = System.Reflection.Assembly.GetExecutingAssembly().FullName;
            container.Register<IRedisClientsManager>(c => pooled_redis);
            container.Register<ICacheClient>(c =>
                (ICacheClient)c.Resolve<IRedisClientsManager>()
                .GetCacheClient())
                .ReusedWithin(Funq.ReuseScope.None);*/


            //Enable Authentication an Registration
            VMCCredenticalsProvider authProvider = new VMCCredenticalsProvider();
            Plugins.Add(new SessionFeature());
            AuthFeature authFeature = new AuthFeature(() => new AuthUserSession(), new IAuthProvider[] {
                  authProvider,new BasicAuthProvider()
                });
            //authFeature.ServiceRoutes.Clear(); // we clear all routes
            Plugins.Add(authFeature);
            Plugins.Add(new RegistrationFeature());


            //Create a DB Factory configured to access the UserAuth SQL Server DB
            dbFactory = GetDbConnectionFromConfig();
            //container.Register(c => GetDbConnectionFromConfig()).ReusedWithin(Funq.ReuseScope.Container);
            //var dbConnection = container.Resolve<OrmLiteConnectionFactory>();
            container.Register<IDbConnectionFactory>(dbFactory);
            //container.Register(c => GetDbConnectionFromConfig()).ReusedWithin(Funq.ReuseScope.Request);

            // register db connection
            //container.Register(c => dbFactory.OpenDbConnection()).ReusedWithin(Funq.ReuseScope.Container);
            DatabaseConnection = dbFactory.OpenDbConnection();
            container.Register(c => dbFactory.OpenDbConnection()).ReusedWithin(Funq.ReuseScope.Container);
            //container.Register<IDbConnection>(DatabaseConnection);
            // caching  - TODO: later must change to ORMLiteCache 
            //container.RegisterAs<OrmLiteCacheClient, ICacheClient>();
            //Create 'CacheEntry' RDBMS table if it doesn't exist already
            //container.Resolve<ICacheClient>().InitSchema(); 
            // end of caching register

            // create tables
            // ModelBase.InitDbTable(ConfigurationManager.AppSettings.Get("AdminUserNames"), true, true);

            //container.RegisterAutoWired<IDbConnection>();
            //container.Register<IDbConnectionFactory>(dbConnection);
            // Register ORM Lite Authentication Repository with our Authentication 
            var userRep = new ABORMLiteAuthRepository(dbFactory);

            container.Register<IAuthProvider>(authProvider);
            container.Register<IUserAuthRepository>(userRep);

            //Configure Custom User Defined REST Paths for your services
            ConfigureServiceRoutes();

            //Set MVC to use the same Funq IOC as ServiceStack
            ControllerBuilder.Current.SetControllerFactory(new FunqControllerFactory(container));
            //ServiceStackController.CatchAllController = reqCtx => container.TryResolve<HomeController>();
        }

        private static OrmLiteConnectionFactory GetDbConnectionFromConfig()
        {
            var cs = ConfigurationManager.AppSettings.Get("ConnectionString");
            var db_type = ConfigurationManager.AppSettings.Get("DatabaseType").ToString().ToString().ToLower();

            // for sqlite, need to check the app_data folder
            cs = cs.Replace("~/", "~/".MapHostAbsolutePath());
            if (!System.IO.Directory.Exists("~/App_Data".MapHostAbsolutePath()))
            {
                System.IO.Directory.CreateDirectory("~/App_Data".MapHostAbsolutePath());
            }

            // fix the bug Dialect Postgresql provider wrong datetime

            if (db_type == "sqlserver")
            {
                ModelBase.DatabaseType = PhotoBookmart.DataLayer.DatabaseTypeEnum.SQLServer;
                SqlServerOrmLiteDialectProvider dialect = SqlServerOrmLiteDialectProvider.Instance;
                dialect.UseUnicode = true;
                dialect.UseDatetime2(true);
                dialect.StringColumnDefinition = "nvarchar(MAX)";
                dialect.StringLengthColumnDefinitionFormat = dialect.StringColumnDefinition;
                dialect.StringLengthNonUnicodeColumnDefinitionFormat = dialect.StringColumnDefinition;
                dialect.StringLengthUnicodeColumnDefinitionFormat = dialect.StringColumnDefinition;
                dialect.NamingStrategy = new ABNamingStrategy();
                var ret = new OrmLiteConnectionFactory(cs, dialect);
                ret.AutoDisposeConnection = true;
                return ret;
            }
            #region For: Sqlite
            /* For: Sqlite
            else if (db_type == "sqlite")
            {
                ModelBase.DatabaseType = PhotoBookmart.DataLayer.DatabaseTypeEnum.Sqlite;
                SqliteOrmLiteDialectProvider dialect = SqliteOrmLiteDialectProvider.Instance;
                dialect.UseUnicode = true;
                dialect.NamingStrategy = new ABNamingStrategy();
                var ret = new OrmLiteConnectionFactory(cs, dialect);
                ret.AutoDisposeConnection = true;
                return ret;
            }*/
            #endregion
            #region For: Postgesql
            /* For: Postgesql
            if (db_type == "postgresql")
            {
                ModelBase.DatabaseType = PhotoBookmart.DataLayer.DatabaseTypeEnum.PostgreSQL;
                PostgreSQLDialectProvider dialect = PostgreSQLDialectProvider.Instance;
                dialect.UseUnicode = true;
                dialect.NamingStrategy = new ABNamingStrategy();
                var ret = new OrmLiteConnectionFactory(cs, dialect);
                ret.AutoDisposeConnection = true;
                return ret;
            }*/
            #endregion
            else
            {
                return null;
            }
        }

        private void ConfigureServiceRoutes()
        {
        }

        public static void Start()
        {
            new AppHost().Init();
        }
    }
}
