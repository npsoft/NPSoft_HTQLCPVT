using System;
using System.Data;
using System.Linq;
using System.Text;
using HttpContext = System.Web;
using ServiceStack.DataAnnotations;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.OrmLite;
using PhotoBookmart.DataLayer.Models.ExtraShipping;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.DataLayer.Models.Sites;
using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.DataLayer.Models.Reports;
using ServiceStack.ServiceInterface.Auth;
using PhotoBookmart.DataLayer.Models.SMS;

namespace PhotoBookmart.DataLayer.Models
{
    /// <summary>
    /// Base of all AB CMS Model
    /// </summary>
    public class ModelBase : BasicModelBase, IDisposable
    {
        /* Public Static For Common Use */
        public static DatabaseTypeEnum DatabaseType { get; set; }

        static ModelBase()
        {
            DatabaseType = DatabaseTypeEnum.Sqlite;
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
        public long Id { get; set; }
        public DateTime CreatedOn { get; set; }
        public long CreatedBy { get; set; }
        [Ignore]
        public string CreatedByUsername { get; set; }

        public ModelBase()
        {
            CreatedBy = 0;
            CreatedOn = DateTime.Now;
            CreatedByUsername = "";
        }

        public void Dispose()
        {
            try
            {
                Db.Close();
            }
            catch
            {
            }
            GC.SuppressFinalize(this);
        }

        ~ModelBase()
        {
            Dispose();
        }

        private static void CreateSchemaIfNotExists(IDbConnection db, string schema, bool gain_permission=false, string user="")
        {
            if (DatabaseType == DatabaseTypeEnum.SQLServer)
            {
                //in Sql2008, CREATE SCHEMA must be the first statement in a batch
                string createSchemaSQL = @"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '" + schema + @"')
                                        BEGIN
                                        EXEC( 'CREATE SCHEMA " + schema + @"' );
                                        END";
                db.ExecuteSql(createSchemaSQL);
            }
            else if (DatabaseType == DatabaseTypeEnum.PostgreSQL)
            {
                try
                {
                    db.ExecuteSql(string.Format("CREATE SCHEMA \"{0}\" AUTHORIZATION {1};", schema, user));
                    if (gain_permission)
                        db.ExecuteSql(string.Format("GRANT ALL ON SCHEMA \"{0}\" TO {1} WITH GRANT OPTION;", schema, user));
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// this function will do a inital for all tables 
        /// </summary>
        public static void InitDbTable(string user, bool GainPermission = false, bool support_schema=false)
        {
            var dbConn = BasicModelBase.ServiceAppHost.TryResolve<IDbConnection>();

            #region MMO
            if (support_schema)
            {
                CreateSchemaIfNotExists(dbConn, "MMO", GainPermission, user);
            }
            dbConn.CreateTableIfNotExists<MMO.MMO_Imgs>(); // dbConn.CreateTableIfNotExists<MMO.MMO_Imgs>(); | dbConn.CreateTable<MMO.MMO_Imgs>(overwrite: true);
            #endregion

            #region DanhMuc
            if (support_schema)
            {
                CreateSchemaIfNotExists(dbConn, "DanhMuc", GainPermission, user);
            }
            dbConn.CreateTableIfNotExists<DanhMuc_DangKhuyetTat>();
            dbConn.CreateTableIfNotExists<DanhMuc_DanToc>();
            dbConn.CreateTableIfNotExists<DanhMuc_DiaChi>();
            dbConn.CreateTableIfNotExists<DanhMuc_HanhChinh>();
            dbConn.CreateTableIfNotExists<DanhMuc_KhaNangPhucVu>();
            dbConn.CreateTableIfNotExists<DanhMuc_LoaiDT>();
            dbConn.CreateTableIfNotExists<DanhMuc_MucDoKhuyetTat>();
            dbConn.CreateTableIfNotExists<DanhMuc_TinhTrangDT>();
            dbConn.CreateTableIfNotExists<DanhMuc_TinhTrangHonNhan>();
            #endregion

            #region DoiTuong
            if (support_schema)
            {
                CreateSchemaIfNotExists(dbConn, "DoiTuong", GainPermission, user);
            }
            dbConn.CreateTableIfNotExists<DoiTuong>();
            dbConn.CreateTableIfNotExists<DoiTuong_BienDong>();
            dbConn.CreateTableIfNotExists<DoiTuong_LoaiDoiTuong_CT>();
            #endregion

            #region CMS
            if (support_schema)
            {
                CreateSchemaIfNotExists(dbConn, "System", GainPermission, user);
                CreateSchemaIfNotExists(dbConn, "CMS", GainPermission, user);
            }
            // User Management
            dbConn.CreateTableIfNotExists<ABUserAuth>();
            dbConn.CreateTableIfNotExists<ABUserOAuthProvider>();
            dbConn.CreateTableIfNotExists<UsersActivation>();

            // language
            dbConn.CreateTableIfNotExists<Language>();

            // for sites
            dbConn.CreateTableIfNotExists<Website>();
            dbConn.CreateTableIfNotExists<Site_ContactusConfig>(); //dbConn.CreateTable<Site_ContactusConfig>(overwrite: true);
            //dbConn.CreateTableIfNotExists<Site_Lang_Dis>();
            dbConn.CreateTableIfNotExists<Site_MemberGroup>();
            dbConn.CreateTableIfNotExists<Site_MemberGroupDetail>();
            dbConn.CreateTableIfNotExists<SiteTopic>();
            dbConn.CreateTableIfNotExists<SiteTopicLanguage>();
            dbConn.CreateTableIfNotExists<SiteSetting>();
            dbConn.CreateTableIfNotExists<Settings>();
            dbConn.CreateTableIfNotExists<SiteNewsletter>();
            dbConn.CreateTableIfNotExists<Site_MaillingListTemplate>();
            dbConn.CreateTableIfNotExists<Site_ContactUs>();
            dbConn.CreateTableIfNotExists<Testimonial>();
            dbConn.CreateTableIfNotExists<SocialAccount>();
            dbConn.CreateTableIfNotExists<Site_Banner>();
            dbConn.CreateTableIfNotExists<Site_FlashHeader>();

            // navigation
            dbConn.CreateTableIfNotExists<Navigation>();

            // news
            dbConn.CreateTableIfNotExists<Site_News_Category>();
            dbConn.CreateTableIfNotExists<Site_News>();

            // blog
            dbConn.CreateTableIfNotExists<Site_Blog_Category>();
            dbConn.CreateTableIfNotExists<Site_Blog>();

            // system
            dbConn.CreateTableIfNotExists<Country>();
            dbConn.CreateTableIfNotExists<Theme>();
            dbConn.CreateTableIfNotExists<Language_Translation>();
            dbConn.CreateTableIfNotExists<MailQueue>();
            dbConn.CreateTableIfNotExists<Exceptions>();
            #endregion

            #region SMS
            if (support_schema)
            {
                CreateSchemaIfNotExists(dbConn, "SMS", GainPermission, user);
            }
            dbConn.CreateTableIfNotExists<SMSTemplateModel>();
            #endregion

            #region Products & Category
            if (support_schema)
            {
                CreateSchemaIfNotExists(dbConn, "Products",GainPermission,user);
            }
            dbConn.CreateTableIfNotExists<Product_Category>(); /* dbConn.CreateTable<Product_Category>(overwrite:true); */
            dbConn.CreateTableIfNotExists<Product>();
            dbConn.CreateTableIfNotExists<Product_Images>();
            dbConn.CreateTableIfNotExists<ProductCategoryImage>();
            dbConn.CreateTableIfNotExists<ProductCategoryMaterial>();
            dbConn.CreateTableIfNotExists<ProductCategoryMaterialDetail>();

            // product price
            dbConn.CreateTableIfNotExists<Price>();

            // option
            dbConn.CreateTableIfNotExists<Product_Option>();

            // Payment
            dbConn.CreateTableIfNotExists<PayPalStandardPaymentSettings>();
            
            // Coupon
            dbConn.CreateTableIfNotExists<CouponPromo>();

            // order
            dbConn.CreateTableIfNotExists<AddressModel>();
            dbConn.CreateTableIfNotExists<Order>();
            dbConn.CreateTableIfNotExists<Order_History>();
            dbConn.CreateTableIfNotExists<Order_ProductOptionUsing>();
            dbConn.CreateTableIfNotExists<Order_ProductionJobSheet>();
            dbConn.CreateTableIfNotExists<Order_UploadFilesTicket>();

            // extra shipping
            // dbConn.CreateTableIfNotExists<Country_State_ExtraShipping>();
            #endregion

            #region Report
            if (support_schema)
            {
                CreateSchemaIfNotExists(dbConn, "Reports", GainPermission, user);
            }
            dbConn.CreateTableIfNotExists<StaffActivity>();
            #endregion

            #region Extra Shipping

            #endregion

            #region Init System
            if (dbConn.Count<ABUserAuth>(m => m.ActiveStatus) == 0)
            {
                // add default user
                ABUserAuth u = new ABUserAuth() { UserName = "trungdt", ActiveStatus = true, CreatedDate = DateTime.Now, DisplayName = "Trung Click4Corp", Email = "trungdt@absoft.vn", FirstName = "Imm", LastName = "Dang", FullName = "Imm Dang", Gender = "Male", Language = "EN", Roles = new global::System.Collections.Generic.List<string>() };

                var PasswordHasher = new SaltedHash();
                string salt;
                string hash;
                PasswordHasher.GetHashAndSaltString("123absoft.vn", out hash, out salt);
                u.PasswordHash = hash;
                u.Salt = salt;
                u.Roles.Add(RoleEnum.Administrator.ToString());
                dbConn.Insert<ABUserAuth>(u);
                u.Id = (int)dbConn.GetLastInsertId();

                if (dbConn.Count<Website>() == 0)
                {
                    Website w = new Website() { CreatedBy = u.Id, CreatedOn = DateTime.Now, Domain = new global::System.Collections.Generic.List<string>(), Name = "ABSoft CMS Site" };
                    w.Domain.Add("localhost");
                    dbConn.Insert<Website>(w);
                }
            }
            #endregion
        }
    }
}
