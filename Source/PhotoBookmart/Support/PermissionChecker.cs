using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.OrmLite;

using PhotoBookmart.Areas.Administration.Controllers;
using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.Helper
{
    /// <summary>
    /// Type Switching, to be use when switch case the type
    /// Sample
    /// var ts = new TypeSwitch()
    ///    .Case((int x) => Console.WriteLine("int"))
    ///    .Case((bool x) => Console.WriteLine("bool"))
    ///    .Case((string x) => Console.WriteLine("string"));
    ///ts.Switch(42);     
    ///ts.Switch(false);  
    ///ts.Switch("hello"); 
    /// </summary>
    public class TypeSwitch
    {
        Dictionary<Type, Func<object, bool>> matches = new Dictionary<Type, Func<object, bool>>();
        public TypeSwitch Case<T>(Func<T, bool> action)
        {
            matches.Add(typeof(T), (x) => { return action((T)x); });
            return this;
        }
        public bool Switch(object x)
        {
            var t = x.GetType();
            if (matches.ContainsKey(t))
            {
                return matches[x.GetType()](x);
            }
            else
            {
                return false;
            }
        }
    }
    /// <summary>
    /// Class to help checking the permission depend on the context and object
    /// </summary>
    public class PermissionChecker
    {
        WebAdminController service;
        TypeSwitch canGet;
        TypeSwitch canList;
        TypeSwitch canAdd;
        TypeSwitch canUpdate;
        TypeSwitch canDelete;
        public PermissionChecker(WebAdminController service)
        {
            this.service = service;
            // init the type switcher 
            //canUpdate = new TypeSwitch()
            //        .Case((ProspectCustomer x) => { return this.CanUpdate(x); })
            //        .Case((KIT x) => { return this.CanUpdate(x); });
            
            // for get
            canGet = new TypeSwitch()
                   .Case((DoiTuong x) => { return this.CanGet(x); });

            // for add
            canAdd = new TypeSwitch()
                   .Case((DoiTuong x) => { return this.CanAdd(x); });

            // for update
            canUpdate = new TypeSwitch()
                   .Case((DoiTuong x) => { return this.CanUpdate(x); });

            // for delete
            canDelete = new TypeSwitch()
                   .Case((DoiTuong x) => { return this.CanDelete(x); });
        }

        #region Functions
        /// <summary>
        /// Return true if the current user in this context has permission to do the udpate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool CanUpdate<T>(T item)
        {
            return canUpdate.Switch(item);
        }

        public bool CanAdd<T>(T item)
        {
            return canAdd.Switch(item);
        }

        public bool CanDelete<T>(T item)
        {
            return canDelete.Switch(item);
        }

        public bool CanGet<T>(T item)
        {
            return canGet.Switch(item);
        }

        //bool Check_A_isLeader_of_B(ABUserAuth A, ABUserAuth B)
        //{
        //    // always consume that A will have greater permission than B
        //    if (A.Id == B.Id)
        //    {
        //        return true;
        //    }
        //    // now we get all teams that A has the A_Role
        //    var Db = service.Db;
        //    var ret = false;

        //    var A_teams = Db.Where<Team_vs_User>(x => x.UserId == A.Id && x.Type != (int)Enum_Team_vs_User_Type.MarketArea);

        //    // if A is Area Manager, we have to get all sub teams
        //    if (A_teams.Count == 0)
        //    {
        //        return false; // there is no teams, actually this is false
        //    }
        //    var A_teams_Id = A_teams.Where(x => x.TypeEnum == Enum_Team_vs_User_Type.AreaManager).Select(x => x.TeamId); // for sure the array will have elements
        //    if (A_teams_Id.Count() > 0)
        //    {
        //        var A_sub_teams = Db.Select<TeamGroup>(x => x.Where(y => y.isTeam == false && Sql.In(y.ParentId, A_teams_Id) && y.ParentId > 0));
        //        A_sub_teams.ForEach(x =>
        //        {
        //            if (A_teams.Count(y => y.TeamId == x.Id) == 0)
        //            {
        //                A_teams.Add(new Team_vs_User()
        //                {
        //                    Id = 0,
        //                    TeamId = x.Id,
        //                    UserId = A.Id,
        //                    TypeEnum = Enum_Team_vs_User_Type.AreaManager
        //                });
        //            }
        //        });
        //    }


        //    var B_teams = Db.Where<Team_vs_User>(x => x.UserId == B.Id && x.Type != (int)Enum_Team_vs_User_Type.MarketArea);

        //    if (A_teams.Count(x => x.TypeEnum == Enum_Team_vs_User_Type.SaleManager) > 0)
        //    {
        //        // for A has permission Sale Manager, let's check B 
        //        if (B_teams.Count(x => x.TypeEnum == Enum_Team_vs_User_Type.SaleManager) > 0)
        //        {
        //            // both A and B are Sale Manager, we can not allow A to see B
        //            return false;
        //        }
        //        else
        //        {
        //            if ((B_teams.Count(x => x.TypeEnum == Enum_Team_vs_User_Type.AreaManager || x.TypeEnum == Enum_Team_vs_User_Type.TeamLeader || x.TypeEnum == Enum_Team_vs_User_Type.Consultant) > 0)
        //                || B.HasRole(RoleEnum.Sales_Consultant, RoleEnum.Sales_AreaManager, RoleEnum.Sales_TeamLeader))
        //            {
        //                // now we are surely know A has permission on B coz A is Sale Manager
        //                return true;
        //            }
        //        }
        //    }

        //    // now we try to match from A_teams to B_Teams, whether any element in b_teams can match A teams, we return true;
        //    foreach (var bt in B_teams)
        //    {
        //        var match = A_teams.Where(x => x.TeamId == bt.TeamId).FirstOrDefault();
        //        if (match != null)
        //        {
        //            // match (A) vs bt (B)
        //            // bt in A_teams? let's check it
        //            if (match.TypeEnum == Enum_Team_vs_User_Type.AreaManager
        //                && (bt.TypeEnum == Enum_Team_vs_User_Type.TeamLeader || bt.TypeEnum == Enum_Team_vs_User_Type.Consultant))
        //            {
        //                ret = true;
        //                break;
        //            }

        //            if (match.TypeEnum == Enum_Team_vs_User_Type.TeamLeader
        //                && bt.TypeEnum == Enum_Team_vs_User_Type.Consultant)
        //            {
        //                ret = true;
        //                break;
        //            }
        //        }
        //    }
        //    return ret;
        //}
        #endregion

        #region For: Đối tượng
        private bool CanGet(DoiTuong item)
        {
            if (item == null) { return false; }
            return true;
        }

        private bool CanAdd(DoiTuong item)
        {
            if (item == null) { return false; }
            ABUserAuth curr_user = service.CurrentUser;
            return curr_user.HasRole(RoleEnum.Admin) || item.MaHC.StartsWith(curr_user.MaHC);
        }

        private bool CanUpdate(DoiTuong item)
        {
            if (item == null) { return false; }
            ABUserAuth curr_user = service.CurrentUser;
            return curr_user.HasRole(RoleEnum.Admin) || item.MaHC.StartsWith(curr_user.MaHC);
        }

        private bool CanDelete(DoiTuong item)
        {
            if (item == null) { return false; }
            ABUserAuth curr_user = service.CurrentUser;
            return !item.IsDuyet && (curr_user.HasRole(RoleEnum.Admin) || item.MaHC.StartsWith(curr_user.MaHC));
        }
        #endregion
    }
}
