using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Principal;
using PhotoBookmart.DataLayer.Models.Users_Management;
using ServiceStack.ServiceInterface.Auth;

namespace PhotoBookmart.Support
{
    /// <summary>
    /// Custom Principal for MVC3
    /// </summary>
    public class ApplicationPrincipal : IPrincipal
    {
        private readonly IIdentity _identity;
        //private List<string> Roles;
        //private readonly IPrincipal _principal;
        //private readonly ISecurityConfiguration _config;

        public ApplicationPrincipal()
        {
            _identity = new GenericIdentity("");
            this.AuthSession = null;
        }

        public ApplicationPrincipal(IAuthSession session)
        {
            _identity = new GenericIdentity(session.UserName, "vn.absoft.cms");
            this.AuthSession = session;
            //this.Roles = session.Roles;
            //// convert all to lowcase
            //for (int i = 0; i < Roles.Count; i++)
            //    Roles[i] = Roles[i].ToLower();
            ////_config = config;
            ////_identity=new GenericIdentity(
        }

        public IIdentity Identity { get { return _identity; } }

        public IAuthSession AuthSession { get; private set; }

        public bool IsInRole(string role)
        {
            return this.AuthSession.HasRole(role);
            //if (this.Roles != null && this.Roles.Contains(role.ToLower()))
            //    return true;
            //else
            //    return false;
        }

        public bool IsInRole(RoleEnum role)
        {
            return this.AuthSession.HasRole(role.ToString());
            //if (this.Roles != null && this.Roles.Contains(role.ToLower()))
            //    return true;
            //else
            //    return false;
        }

        public bool HasPermission(string permission)
        {
            return this.AuthSession.HasPermission(permission);
        }
    }
}