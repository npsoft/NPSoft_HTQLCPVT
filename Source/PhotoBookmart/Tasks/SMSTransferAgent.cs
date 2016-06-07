using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Data;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using ServiceStack.ServiceClient.Web;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.Models;
using ServiceStack.CacheAccess;

namespace PhotoBookmart.Tasks
{
    public static class SMSTransferAgentTask
    {
        static JsonServiceClient server_client;
        /// <summary>
        /// Queue list to process the sms sending
        /// </summary>
        static List<SMS_Item> Queue = new List<SMS_Item>();

        static string cache_key = "";
        //
        static Thread thread;
        static int Interval = 1000;

        //
        static IDbConnection _connection = null;
        public static IDbConnection Db
        {
            get
            {
                if (_connection == null)
                {
                    _connection = AppHost.Resolve<IDbConnection>();
                }

                if (_connection != null && _connection.State != ConnectionState.Open)
                {
                    _connection.Open();
                }

                if (_connection != null && _connection.State != ConnectionState.Open)
                {
                    // force open new connection
                    _connection = AppHost.Resolve<IDbConnectionFactory>().Open();
                }

                return _connection;
            }
        }

        // cache client
        static ICacheClient _cache = null;
        static ICacheClient Cache
        {
            get
            {
                if (_cache == null)
                {
                    _cache = AppHost.Resolve<ICacheClient>();
                }

                return _cache;
            }
        }

        #region Common Functions
        static void InsertException(string message, Exception exp = null)
        {
            Exceptions ex = new Exceptions();
            ex.ExMessage = message;
            ex.ContextBrowserAgent = "SMS Transfer Agent Task";
            ex.ExceptionOn = DateTime.Now;
            if (exp != null)
            {
                ex.ExSource = exp.Source;
                ex.ExStackTrace = exp.StackTrace;
            }
            // insert into db
            Db.Insert<Exceptions>(ex);
            Db.Close();
        }

        /// <summary>
        /// Prepare the conection to SMS server
        /// It will throw the Exception if can not prepare the connection
        /// </summary>
        static void _PrepareConnection()
        {
            // connect to server
            if (server_client == null)
            {
                var server_url = (string)Settings.Get(Enum_Settings_Key.SMS_SERVICE_URL, "", Enum_Settings_DataType.String);

                if (string.IsNullOrEmpty(server_url))
                {
                    InsertException("Please contact Admin to configure the SMS Settings");
                    return;
                }
                server_client = new JsonServiceClient(server_url);

                if (!_LoginToServer())
                {
                    InsertException("Please contact Admin to configure the SMS Settings. Can not login into SMS Server");
                    return;
                }
            }
        }

        /// <summary>
        /// Return true if we can connect to server
        /// </summary>
        /// <returns></returns>
        static bool _LoginToServer()
        {
            var username = (string)Settings.Get(Enum_Settings_Key.SMS_SERVICE_USERNAME, "", Enum_Settings_DataType.String);
            var password = (string)Settings.Get(Enum_Settings_Key.SMS_SERVICE_PASSWORD, "", Enum_Settings_DataType.String);

            try
            {
                SMSServer_LoginModel loginmodel = new SMSServer_LoginModel() { Username = username, Password = password };
                var login = server_client.Post(loginmodel);
                if (login != null && login.Status != null && login.Status.ErrorCode == "1")
                {
                    return true;
                }
                else
                {
                    if (login != null && login.Status != null)
                    {
                        InsertException("Login to SMS Server not success. Status code = " + login.Status.ErrorCode + ", message =" + login.Status.Message);
                    }
                    else
                    {
                        InsertException("Login to SMS Server not success with unknown reason");
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                InsertException("Login to SMS Server not success, " + ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// Put into the queue to send sms
        /// </summary>
        /// <param name="item"></param>
        public static void Send(string number, string content, bool isFlash)
        {
            if (Queue == null)
            {
                Queue = new List<SMS_Item>();
            }

            if (!IsEnable())
            {
                return;
            }

            if (string.IsNullOrEmpty(number) || string.IsNullOrEmpty(content))
            {
                return;
            }

            Queue.Add(new SMS_Item() { Body = content, PhoneNumber = number, IsFlashSMS = isFlash, IsProcessing = false });
        }

        /// <summary>
        /// Return true if in settings allow us to send sms
        /// </summary>
        /// <returns></returns>
        static bool IsEnable()
        {
            //var data = Cache.Get<string>(cache_key);
            //if (string.IsNullOrEmpty(data))
            //{
            var data = Settings.Get(Enum_Settings_Key.SMS_SERVICE_ENABLE, null, 0, Enum_Settings_DataType.String).ToString();
            //    Cache.Add<string>(cache_key, data, TimeSpan.FromMinutes(5));
            //}
            return data == "true";
        }
        #endregion

        public static void Start()
        {
            Random r = new Random();
            cache_key = r.NextDouble().ToString() + "_" + r.NextDouble().ToString();

            thread = new Thread(new ThreadStart(Execute));
            thread.Start();
        }

        static void Execute()
        {
            while (thread.IsAlive)
            {
                try
                {

                    if (Queue.Where(x => x.IsProcessing == false).Count() == 0)
                    {
                        try
                        {
                            Thread.Sleep(Interval);
                        }
                        catch
                        {
                        }
                        continue;
                    }

                    if (server_client == null)
                    {
                        _PrepareConnection();
                    }

                    // still null? exit
                    if (server_client == null)
                    {
                        try
                        {
                            Thread.Sleep(1000 * 60);// sleep in 1 mins
                        }
                        catch
                        {
                        }
                        continue;
                    }


                    for (int i = 0; i < 10; i++)
                    {
                        //
                        var item = Queue.Where(x => x.IsProcessing == false).FirstOrDefault();
                        if (item != null)
                        {
                            item.IsProcessing = true;

                            SMSServer_SMSSendModel r = new SMSServer_SMSSendModel()
                            {
                                Receivers = new List<string>() { item.PhoneNumber },
                                IsFlashSMS = item.IsFlashSMS,
                                Content = item.Body
                            };

                            if (server_client != null)
                            {
                                var ret = server_client.Post(r);
                                if (ret.Status != null && ret.Status.ErrorCode == "1")
                                {
                                    // remove item out of the queue
                                    Queue.Remove(item);
                                }
                                else
                                {
                                    if (ret.Status == null)
                                    {
                                        ret.Status = new ServiceStack.ServiceInterface.ServiceModel.ResponseStatus();
                                    }
                                    InsertException(string.Format("Exception: Failed to send request to send SMS to SMS Server. Number = {0}, Content = {1}, Reason = {2}", item.PhoneNumber, item.Body, ret.Status.Message));
                                    // if exception, then no need to continue sending, something wrong. let's way
                                    break;
                                }
                            }
                            else
                            {
                                server_client = null;
                            }
                        }
                        else
                        {
                            break;
                        }
                        Thread.Sleep(0);
                        Thread.Sleep(0);
                        Thread.Sleep(350);
                        Thread.Sleep(0);
                        Thread.Sleep(0);
                    }
                }
                catch (Exception ex)
                {
                    // prepare again in case we can not process 
                    //_PrepareConnection();
                    server_client = null;
                    // throw any other exception - this should not occur
                    if (ex.Message != "Thread was being aborted")
                    {
                        InsertException(ex.Message, ex);
                    }
                }

                // sleep
                Thread.Sleep(0);
                Thread.Sleep(Interval);
                Thread.Sleep(0);
            }
        }
    }
}
