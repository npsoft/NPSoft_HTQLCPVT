using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ProsperOnline.ServiceMethods.Authentication
{
    #region Login
    [Route("/Kiosk_Login", "POST")]
    [Api("Kiosk Login into system")]
    public class Login : IReturn<LoginResponse>
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public ResponseStatus Status { get; set; }
        public string SecureToken { get; set; }

        public LoginResponse()
        {
            Status = new ResponseStatus() { ErrorCode = "" };
        }
    }

    #endregion

    #region Logout
    [Route("/Kiosk_Logout", "POST")]
    [Api("Kiosk Logout of system")]
    public class Logout : IReturn<LogoutResponse>
    {
    }

    public class LogoutResponse
    {
        public ResponseStatus Status { get; set; }

        public LogoutResponse()
        {
            Status = new ResponseStatus() { ErrorCode = "" };
        }
    }
    #endregion
}
