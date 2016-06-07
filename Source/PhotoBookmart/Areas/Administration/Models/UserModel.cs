using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.Areas.Administration.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool Status { get; set; }
        public DateTime DataCreate { get; set; }
        public DateTime DateUpdate { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Address { get; set; }
        public string Zipcode { get; set; }
        public string Country { get; set; }
        public DateTime DateUpgrade { get; set; }
        public DateTime ExpiredDate { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
        public string Phone { get; set; }
        public string Gender { get; set; }
        public string Avatar { get; set; }
        
        public int SiteId { get; set; }
        public string Site_name { get; set; }
        
        public int GroupId { get; set; }
        public string Group_Name { get; set; }

        public List<MoreInfoAdmin> ModelCheck { get; set; }
        // change Info
        public string PassNews { get; set; }
        public string PassNewsAgain { get; set; }
        public string EmailChange { get; set; }
        public string Emailchange2 { get; set; }
        public string Tendangnhapchange { get; set; }
        public string NameAddUser { get; set; }

        //extra
        /// <summary>
        /// Get from Advertiser table, by built-in functions but will not be saved when update object
        /// </summary>
        public string Companyname { get; set; }
        public Int64 CompanyId { get; set; }
        public List<string> RoleName { get; set; }
        public List<string> Permission { get; set; }

        // Use only to get post value from form to server
        public string[] rolesId { get; set; }

        public string MaHC { get; set; }
        public string Website { get; set; }
     

        public UserModel()
        {
            RoleName = new List<string>();
            Permission = new List<string>();
            ModelCheck = new List<MoreInfoAdmin>();

            Site_name = "";
            Group_Name = "";
        }

        public static void ToEntity(UserModel model, ref ABUserAuth entity)
        {
            entity.Id = model.Id;
            entity.FullName = model.FullName;
            entity.DisplayName = model.FullName;
            entity.UserName = model.UserName;
            entity.Email = model.Email;
            entity.PrimaryEmail = model.Email;
            entity.PasswordHash = model.Password;
            entity.ActiveStatus = model.Status;
            entity.CreatedDate = model.DataCreate;
            entity.ModifiedDate = model.DateUpdate;
            entity.MailAddress = model.Address;
            entity.PostalCode = model.Zipcode;
            entity.Country = model.Country;
            entity.BirthDate = model.BirthDate;
            entity.Phone = model.Phone;
            entity.Permissions = model.Permission;
            entity.Gender=model.Gender;
            entity.MaHC = model.MaHC;
        }

        public static void ToModel(ABUserAuth entity, ref UserModel model)
        {
            model.Id = entity.Id;
            model.FullName = entity.FullName;
            model.FirstName = entity.FirstName;
            model.LastName = entity.LastName;
            model.UserName = entity.UserName;
            model.Email = entity.Email;
            model.Password = entity.PasswordHash;
            model.Status = entity.ActiveStatus;
            model.DataCreate = entity.CreatedDate;
            model.DateUpdate = entity.ModifiedDate;
            model.Address = entity.MailAddress;
            model.Zipcode = entity.PostalCode;
            model.Country = entity.Country;
            model.RoleName = entity.Roles;
            model.rolesId = entity.Roles.ToArray();
            model.BirthDate = entity.BirthDate;
            model.Phone = entity.Phone;
            model.Permission = entity.Permissions;
            model.Gender = entity.Gender;
            model.Avatar = entity.Nickname;
            model.MaHC = entity.MaHC;
        }
    }

    public class MoreInfoAdmin
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Checkbox { get; set; }
    }

    public class UserSearchModel
    {
        public int Page { get; set; }
        public string SearchKey { get; set; }
        public string UserStatus { get; set; }
        public string UserRole { get; set; }
        public string ResultType { get; set; }
    }
}
