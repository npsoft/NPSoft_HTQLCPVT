using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.OrmLite;
using System.Data;
using PhotoBookmart.DataLayer.Models.Users_Management;
using ServiceStack.Common.Web;

namespace PhotoBookmart.Common.ServiceStackHelper.Provider
{
    public class VMCCredenticalsProvider : CredentialsAuthProvider
    {
        public IDbConnectionFactory DbFactory { get; set; }
        private IDbConnection db;
        public virtual IDbConnection Db
        {
            get { return db ?? (db = DbFactory.Open()); }
        }

        public override bool TryAuthenticate(IServiceBase authService, string userName, string password)
        {
            if (DbFactory == null)
                DbFactory = authService.TryResolve<IDbConnectionFactory>();

            var x = Db.Select<ABUserAuth>(m => m.UserName == userName && m.ActiveStatus);
            if (x.Count > 0)
            {
                return base.TryAuthenticate(authService, userName, password);
            }
            else
                return false;
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Auth request)
        {
            if (DbFactory == null)
                DbFactory = authService.TryResolve<IDbConnectionFactory>();

            var x = Db.Select<ABUserAuth>(m => m.UserName == request.UserName && m.ActiveStatus);
            if (x.Count > 0)
            {
                return base.Authenticate(authService, session, request);
            }
            return HttpError.Unauthorized("Your account is not actived. Please active your account or contact our Support team. Thanks");
        }

        public override void OnAuthenticated(IServiceBase authService, IAuthSession session, IOAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            base.OnAuthenticated(authService, session, tokens, authInfo);
        }
    }
}