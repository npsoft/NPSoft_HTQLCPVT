using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;

namespace ProsperOnline.ServiceMethods
{
    [Route("/ping", "POST")]
    public class Ping:IReturn<PingResponse>
    {
    }

    public class PingResponse
    {
    }
}
