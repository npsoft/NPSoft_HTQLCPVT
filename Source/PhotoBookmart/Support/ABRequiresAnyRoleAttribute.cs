using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ServiceStack.ServiceInterface;
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart
{
    /// <summary>
    /// Indicates that the request dto, which is associated with this attribute,
    /// can only execute, if the user has any of the specified roles.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class ABRequiresAnyRoleAttribute : RequiresAnyRoleAttribute
    {
        public ABRequiresAnyRoleAttribute(ApplyTo applyTo, params RoleEnum[] roles)
        {
            List<string> r = new List<string>();
            foreach (var x in roles)
            {
                r.Add(x.ToString());
            }

            this.RequiredRoles = r;
            this.ApplyTo = applyTo;
            this.Priority = (int)RequestFilterPriority.RequiredRole;
        }

        public ABRequiresAnyRoleAttribute(params RoleEnum[] roles)
            : this(ApplyTo.All, roles) { }
    }
}