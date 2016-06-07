using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PhotoBookmart.DataLayer.Models.Sites;

namespace PhotoBookmart.Areas.Administration.Models
{
    public class MenuList
    {
        public long ID { get; set; }
        public string Name { get; set; }
        public long ParentID { get; set; }
        public static void Mapfrom(Navigation entity, ref MenuList model)
        {
            model.ID = entity.Id;
            model.Name = entity.Name;
            model.ParentID = entity.ParentId;
        }
    }

}