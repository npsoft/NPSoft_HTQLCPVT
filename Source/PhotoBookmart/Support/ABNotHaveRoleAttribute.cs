using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack.ServiceInterface;
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart
{
    /// <summary>
    /// Can only excute if does not have the listed role
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class ABNotHaveRoleAttribute : RequiresAnyRoleAttribute
    {
        public ABNotHaveRoleAttribute(ApplyTo applyTo, params RoleEnum[] roles)
        {
            List<string> r = new List<string>();
            foreach (RoleEnum e in (RoleEnum[])Enum.GetValues(typeof(RoleEnum)))
            {
                if (roles.Contains(e))
                {
                    continue;
                }
                r.Add(e.ToString());
            }

            this.RequiredRoles = r;
            this.ApplyTo = applyTo;
            this.Priority = (int)RequestFilterPriority.RequiredRole;
        }

        public ABNotHaveRoleAttribute(params RoleEnum[] roles)
            : this(ApplyTo.All, roles) { }
    }
}