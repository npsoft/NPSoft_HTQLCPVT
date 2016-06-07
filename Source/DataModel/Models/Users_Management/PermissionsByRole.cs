using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace ABSoft.DataLayer.Models.Users_Management
{
    /// <summary>
    /// This table keep the relative between Roles and Permissions
    /// </summary>
    [Schema("System")]
    public class PermissionsByRole
    {
        public long Id { get; set; }
        public string Role { get; set; }
        public string Permission { get; set; }
        public bool AllowAction { get; set; }
        public bool Status { get; set; }
        public long User_Id { get; set; }
    }
}
    