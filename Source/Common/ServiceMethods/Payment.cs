using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

/// Methods to communicate with Products / Products Categories
namespace ProsperOnline.ServiceMethods.Payment
{
    #region Transaction Payment
    [Route("/Payment_SubmitTransaction", "POST")]
    [Api("Submit a transaction payment from VMC to server")]
    public class Payment_SubmitTransaction : IReturn<Payment_SubmitTransactionResponse>
    {

    }

  
    public class Payment_SubmitTransactionResponse
    {
        public ResponseStatus Status { get; set; }

        public Payment_SubmitTransactionResponse()
        {
            Status = new ResponseStatus() { ErrorCode = "" };
        }
    }

    #endregion
}
