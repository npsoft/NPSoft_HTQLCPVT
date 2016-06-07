using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Threading;
using System.Web;
using System.IO;
using System.Configuration;
using System.Net;

namespace PhotoBookmart.Common.Helpers
{
    /// <summary>
    /// Keep Alive Thread, to keep the site alive.
    /// </summary>
    public class KeepAliveTask : IDisposable
    {
        Thread Timer_Task;
        private bool _disposed;
        string URL = "";
        int Interval = 1000 * 10;

        public KeepAliveTask(string url)
        {
            this.URL = url;
        }

        public void Dispose()
        {
            if ((this.Timer_Task != null) && !this._disposed)
            {
                lock (this)
                {
                    this.Timer_Task.Abort();
                    this.Timer_Task = null;
                    this._disposed = true;
                }
            }
        }

        public void Start()
        {
            Timer_Task = new Thread(new ThreadStart(Execute));
            Timer_Task.Start();
        }

        void Execute()
        {
            while (Timer_Task.IsAlive)
            {
                try
                {
                    using (var wc = new WebClient())
                    {
                        wc.DownloadString(URL);
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                    {
                        var resp = (HttpWebResponse)ex.Response;
                        if (resp.StatusCode == HttpStatusCode.NotFound) // HTTP 404
                        {
                            // the page was not found (as it can be expected with some webservers)
                            return;
                        }
                    }
                    Thread.Sleep(1000 * 60 * 5); // try again in 5 mins
                    // throw any other exception - this should not occur
                    //throw;
                }

                // 
                Thread.Sleep(0);
                Thread.Sleep(Interval);
                Thread.Sleep(0);
            }
        }
    }
}
