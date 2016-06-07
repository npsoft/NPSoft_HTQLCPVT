using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ServiceStack.Common;
using ServiceStack.Text;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface.Auth;
using PhotoBookmart.DataLayer.Models.Users_Management;

namespace PhotoBookmart.Common.ServiceStackHelper.Provider
{
    /// <summary>
    /// Copyright: Trung Dang (immanuel192@gmail.com)
    /// Just copy from ServiceStack source code and customize to put UserAuth into Schema
    /// </summary>
    public class ABORMLiteAuthRepository : IUserAuthRepository, IClearable
    {
        //http://stackoverflow.com/questions/3588623/c-sharp-regex-for-a-username-with-a-few-restrictions
        public Regex ValidUserNameRegEx = new Regex(@"^(?=.{3,15}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled);

        private readonly IDbConnectionFactory dbFactory;
        private readonly IHashProvider passwordHasher;

        public ABORMLiteAuthRepository(IDbConnectionFactory dbFactory)
            : this(dbFactory, new SaltedHash())
        { }

        public ABORMLiteAuthRepository(IDbConnectionFactory dbFactory, IHashProvider passwordHasher)
        {
            this.dbFactory = dbFactory;
            this.passwordHasher = passwordHasher;
        }

        public void CreateMissingTables()
        {
            dbFactory.Run(db =>
            {
                db.CreateTable<ABUserAuth>(false);
                db.CreateTable<ABUserOAuthProvider>(false);
            });
        }

        public void DropAndReCreateTables()
        {
            dbFactory.Run(db =>
            {
                db.CreateTable<ABUserAuth>(true);
                db.CreateTable<ABUserOAuthProvider>(true);
            });
        }

        private void ValidateNewUser(ABUserAuth newUser, string password)
        {
            newUser.ThrowIfNull("newUser");
            password.ThrowIfNullOrEmpty("password");

            if (newUser.UserName.IsNullOrEmpty() && newUser.Email.IsNullOrEmpty())
                throw new ArgumentNullException("UserName or Email is required");

            if (!newUser.UserName.IsNullOrEmpty())
            {
                if (!ValidUserNameRegEx.IsMatch(newUser.UserName))
                    throw new ArgumentException("UserName contains invalid characters", "UserName");
            }
        }

        public UserAuth CreateUserAuth(UserAuth newUser, string password)
        {
            ABUserAuth ab_newuser = newUser.TranslateTo<ABUserAuth>();
            ValidateNewUser(ab_newuser, password);

            string salt;
            string hash;
            passwordHasher.GetHashAndSaltString(password, out hash, out salt);

            return dbFactory.Run(db =>
            {
                AssertNoExistingUser(db, ab_newuser);

                var digestHelper = new DigestAuthFunctions();
                ab_newuser.DigestHa1Hash = digestHelper.CreateHa1(ab_newuser.UserName, DigestAuthProvider.Realm, password);
                ab_newuser.PasswordHash = hash;
                ab_newuser.Salt = salt;
                ab_newuser.CreatedDate = DateTime.UtcNow;
                ab_newuser.ModifiedDate = newUser.CreatedDate;

                db.Insert(ab_newuser);

                ab_newuser = db.GetById<ABUserAuth>(db.GetLastInsertId());
                return ab_newuser.TranslateTo<UserAuth>();
            });
        }

        private static void AssertNoExistingUser(IDbConnection db, ABUserAuth newUser, ABUserAuth exceptForExistingUser = null)
        {
            if (newUser.UserName != null)
            {
                var existingUser = GetUserAuthByUserName(db, newUser.UserName);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException("User {0} already exists".Fmt(newUser.UserName));
            }
            if (newUser.Email != null)
            {
                var existingUser = GetUserAuthByUserName(db, newUser.Email);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException("Email {0} already exists".Fmt(newUser.Email));
            }
        }

        public UserAuth UpdateUserAuth(UserAuth eUser, UserAuth nUser, string password)
        {
            // cast to our table
            ABUserAuth existingUser = eUser.TranslateTo<ABUserAuth>();
            ABUserAuth newUser = nUser.TranslateTo<ABUserAuth>();

            ValidateNewUser(newUser, password);

            return dbFactory.Run(db =>
            {
                AssertNoExistingUser(db, newUser, existingUser);

                var hash = existingUser.PasswordHash;
                var salt = existingUser.Salt;
                if (password != null)
                {
                    passwordHasher.GetHashAndSaltString(password, out hash, out salt);
                }
                // If either one changes the digest hash has to be recalculated
                var digestHash = existingUser.DigestHa1Hash;
                if (password != null || existingUser.UserName != newUser.UserName)
                {
                    var digestHelper = new DigestAuthFunctions();
                    digestHash = digestHelper.CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);
                }
                newUser.Id = existingUser.Id;
                newUser.PasswordHash = hash;
                newUser.Salt = salt;
                newUser.DigestHa1Hash = digestHash;
                newUser.CreatedDate = existingUser.CreatedDate;
                newUser.ModifiedDate = DateTime.UtcNow;

                db.Save(newUser);

                return newUser.TranslateTo<UserAuth>();
            });
        }

        public UserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            return dbFactory.Run(db => GetUserAuthByUserName(db, userNameOrEmail));
        }

        private static UserAuth GetUserAuthByUserName(IDbConnection db, string userNameOrEmail)
        {
            var isEmail = userNameOrEmail.Contains("@");
            try
            {
                var userAuth = isEmail
                    ? db.Select<ABUserAuth>(q => q.Email == userNameOrEmail).FirstOrDefault()
                    : db.Select<ABUserAuth>(q => q.UserName == userNameOrEmail).FirstOrDefault();

                return userAuth.TranslateTo<UserAuth>();
            }
            catch
            {
                return null;
            }
        }

        public bool TryAuthenticate(string userName, string password, out UserAuth userAuth)
        {
            //userId = null;
            userAuth = GetUserAuthByUserName(userName);
            if (userAuth == null) return false;

            if (passwordHasher.VerifyHashString(password, userAuth.PasswordHash, userAuth.Salt))
            {
                //userId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            userAuth = null;
            return false;
        }

        public bool TryAuthenticate(Dictionary<string, string> digestHeaders, string PrivateKey, int NonceTimeOut, string sequence, out UserAuth userAuth)
        {
            //userId = null;
            userAuth = GetUserAuthByUserName(digestHeaders["username"]);
            if (userAuth == null) return false;

            var digestHelper = new DigestAuthFunctions();
            if (digestHelper.ValidateResponse(digestHeaders, PrivateKey, NonceTimeOut, userAuth.DigestHa1Hash, sequence))
            {
                //userId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            userAuth = null;
            return false;
        }

        public void LoadUserAuth(IAuthSession session, IOAuthTokens tokens)
        {
            session.ThrowIfNull("session");

            var userAuth = GetUserAuth(session, tokens);
            LoadUserAuth(session, userAuth);
        }

        private void LoadUserAuth(IAuthSession session, UserAuth userAuth)
        {
            if (userAuth == null) return;

            var idSesije = session.Id;  //first record session Id (original session Id)
            session.PopulateWith(userAuth); //here, original sessionId is overwritten with facebook user Id
            session.Id = idSesije;  //we return Id of original session here

            session.UserAuthId = userAuth.Id.ToString(CultureInfo.InvariantCulture);
            session.ProviderOAuthAccess = GetUserOAuthProviders(session.UserAuthId)
                .ConvertAll(x => (IOAuthTokens)x);

        }

        public UserAuth GetUserAuth(string userAuthId)
        {
            var x=dbFactory.Run(db => db.GetByIdOrDefault<ABUserAuth>(userAuthId));
            return x.TranslateTo<UserAuth>();
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            dbFactory.Run(db =>
            {

                var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                    ? db.GetByIdOrDefault<ABUserAuth>(authSession.UserAuthId)
                    : authSession.TranslateTo<ABUserAuth>();

                if (userAuth.Id == default(int) && !authSession.UserAuthId.IsNullOrEmpty())
                    userAuth.Id = int.Parse(authSession.UserAuthId);

                userAuth.ModifiedDate = DateTime.UtcNow;
                if (userAuth.CreatedDate == default(DateTime))
                    userAuth.CreatedDate = userAuth.ModifiedDate;

                db.Save(userAuth);
            });
        }

        public void SaveUserAuth(UserAuth user)
        {
            ABUserAuth userAuth = user.TranslateTo<ABUserAuth>();
            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default(DateTime))
                userAuth.CreatedDate = userAuth.ModifiedDate;

            dbFactory.Run(db => db.Save(userAuth));
        }

        public List<UserOAuthProvider> GetUserOAuthProviders(string userAuthId)
        {
            var id = int.Parse(userAuthId);
            var k = dbFactory.Run(db =>
                db.Select<ABUserOAuthProvider>(q => q.UserAuthId == id)).OrderBy(x => x.ModifiedDate).ToList();
            List<UserOAuthProvider> t = new List<UserOAuthProvider>();
            foreach (var z in k)
                t.Add(z.TranslateTo<UserOAuthProvider>());
            return t;
        }

        public UserAuth GetUserAuth(IAuthSession authSession, IOAuthTokens tokens)
        {
            if (!authSession.UserAuthId.IsNullOrEmpty())
            {
                var userAuth = GetUserAuth(authSession.UserAuthId);
                if (userAuth != null) return userAuth;
            }
            if (!authSession.UserAuthName.IsNullOrEmpty())
            {
                var userAuth = GetUserAuthByUserName(authSession.UserAuthName);
                if (userAuth != null) return userAuth;
            }

            if (tokens == null || tokens.Provider.IsNullOrEmpty() || tokens.UserId.IsNullOrEmpty())
                return null;

            return dbFactory.Run(db =>
            {
                var oAuthProvider = db.Select<ABUserOAuthProvider>(q =>
                    q.Provider == tokens.Provider && q.UserId == tokens.UserId).FirstOrDefault();

                if (oAuthProvider != null)
                {
                    var userAuth = db.GetByIdOrDefault<ABUserAuth>(oAuthProvider.UserAuthId);
                    return userAuth.TranslateTo<UserAuth>();
                }
                return null;
            });
        }

        public string CreateOrMergeAuthSession(IAuthSession authSession, IOAuthTokens tokens)
        {
            var userAuth = GetUserAuth(authSession, tokens) ?? new ABUserAuth();

            return dbFactory.Run(db =>
            {

                var oAuthProvider = db.Select<ABUserOAuthProvider>(q =>
                    q.Provider == tokens.Provider && q.UserId == tokens.UserId).FirstOrDefault();

                if (oAuthProvider == null)
                {
                    oAuthProvider = new ABUserOAuthProvider
                    {
                        Provider = tokens.Provider,
                        UserId = tokens.UserId,
                    };
                }

                oAuthProvider.PopulateMissing(tokens);
                userAuth.PopulateMissing(oAuthProvider);

                userAuth.ModifiedDate = DateTime.UtcNow;
                if (userAuth.CreatedDate == default(DateTime))
                    userAuth.CreatedDate = userAuth.ModifiedDate;

                db.Save(userAuth);

                oAuthProvider.UserAuthId = userAuth.Id != default(int)
                    ? userAuth.Id
                    : (int)db.GetLastInsertId();

                if (oAuthProvider.CreatedDate == default(DateTime))
                    oAuthProvider.CreatedDate = userAuth.ModifiedDate;
                oAuthProvider.ModifiedDate = userAuth.ModifiedDate;

                db.Save(oAuthProvider);

                return oAuthProvider.UserAuthId.ToString(CultureInfo.InvariantCulture);
            });
        }

        public void Clear()
        {
            dbFactory.Run(db =>
            {
                db.DeleteAll<ABUserAuth>();
                db.DeleteAll<ABUserOAuthProvider>();
            });
        }
    }
}