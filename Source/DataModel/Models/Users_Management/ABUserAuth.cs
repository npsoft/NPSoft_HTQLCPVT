using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using ServiceStack.ServiceInterface.Auth;
using PhotoBookmart.Common.Helpers;

namespace PhotoBookmart.DataLayer.Models.Users_Management
{
    #region ENUM: Roles
    public enum RoleEnum
    {
        [Display(Name = "Quản trị")]
        Admin,
        [Display(Name = "Cấp tỉnh")]
        Province,
        [Display(Name = "Cấp huyện")]
        District,
        [Display(Name = "Cấp xã")]
        Village,
        Administrator,
        OrderManagement,
        Employee,
        Customer,
        /// <summary>
        /// Step 1, just receive order
        /// </summary>
        Received,

        /// <summary>
        /// Step 2
        /// </summary>
        Verify,

        /// <summary>
        /// Step 3
        /// </summary>
        Processing,

        /// <summary>
        /// Step 4
        /// </summary>
        QC_AfterFileProcessing,

        /// <summary>
        /// Step 5
        /// </summary>
        Printing,

        /// <summary>
        /// Step 6
        /// </summary>
        QC_AfterFilePriting,

        /// <summary>
        /// Step 7
        /// </summary>
        Production,

        /// <summary>
        /// Step 8
        /// </summary>
        Shipping,

        /// <summary>
        /// Step 9
        /// </summary>
        Finished
    }
    #endregion

    #region [System].[Users]

    [Alias("Users")]
    [Schema("System")]
    public partial class ABUserAuth : UserAuth
    {
        public string MaHC { get; set; }
        public bool ActiveStatus { get; set; }
        public string Phone { get; set; }
        public int GroupId { get; set; }
        [Ignore]
        public string Avatar
        {
            get { return this.Nickname; }
            set { this.Nickname = value; }
        }
        [Ignore]
        public string EmailChange { get; set; }
        [Ignore]
        public string NameAddUser { get; set; }
        [Ignore]
        public string Password { get; set; }
        [Ignore]
        public string PassNews { get; set; }
        [Ignore]
        public string ConfirmPassword { get; set; }
        [Ignore]
        public string RedirectTo { get; set; }

        public ABUserAuth()
        {
            ActiveStatus = false;
        }

        public bool HasRole(RoleEnum role_enum)
        {
            var role = role_enum.ToString().ToLower();

            return (this.Roles.Where(m => m.ToLower() == role).Count() > 0);
        }

        public bool HasRole(string role)
        {
            role = role.ToLower();

            return (this.Roles.Where(m => m.ToLower() == role).Count() > 0);
        }
        
        public static List<string> GetListDescRole(List<string> roles)
        {
            for (int i = 0; i < roles.Count; i++)
            {
                RoleEnum role = (RoleEnum)Enum.Parse(typeof(RoleEnum), roles[i]);
                roles[i] = role.DisplayName();
            }
            return roles;
        }
    }

    #endregion

    #region [System].[UsersOAuth]

    [Alias("UsersOAuth")]
    [Schema("System")]
    public class ABUserOAuthProvider : UserOAuthProvider { }

    #endregion
}
