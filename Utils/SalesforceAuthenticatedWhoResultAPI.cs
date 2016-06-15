using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ManyWho.Flow.SDK.Security;

namespace ManyWho.Service.Salesforce.Utils
{
    public class SalesforceAuthenticatedWhoResultAPI : AuthenticatedWhoResultAPI
    {
        public String refreshToken
        {
            get;
            set;
        }

        public String chatterBaseUrl
        {
            get;
            set;
        }
    }
}