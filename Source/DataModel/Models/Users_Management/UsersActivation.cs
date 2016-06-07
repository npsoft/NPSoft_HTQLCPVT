using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Users_Management
{
    [Alias("UsersActivation")]
    [Schema("System")]
    public partial class UsersActivation
    {
        [PrimaryKey]
        [AutoIncrement]
        public Int64 Id { get; set; }

        [Default(0)]
        public Int64 UserID { get; set; }

        public DateTime ExpireOn { get; set; }

        [Default(typeof(string), "")]
        public string CodeActive { get; set; }

        public UsersActivation()
        {
            ExpireOn = DateTime.Now;
            CodeActive = "";
        }
    }
}
