using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Net.Http;
using System.Threading.Tasks;
using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Utils;
using ManyWho.Flow.SDK.Security;
using ManyWho.Flow.SDK.Run.Elements.Type;
using ManyWho.Flow.SDK.Run.Elements.Config;
using ManyWho.Service.Salesforce.Models.Rest;

/*!

Copyright 2013 Manywho, Inc.

Licensed under the Manywho License, Version 1.0 (the "License"); you may not use this
file except in compliance with the License.

You may obtain a copy of the License at: http://manywho.com/sharedsource

Unless required by applicable law or agreed to in writing, software distributed under
the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.

*/

namespace ManyWho.Service.Salesforce.Utils
{
    public class SalesforceHttpUtils : HttpUtils
    {
        public static String TOKEN_PREFIX = "Salesforce:";

        public static AuthenticationDetails GetAuthenticationDetails(String authenticationToken)
        {
            AuthenticationDetails authenticationDetails = null;

            authenticationDetails = new AuthenticationDetails();

            if (authenticationToken.IndexOf(TOKEN_PREFIX) < 0)
            {
                authenticationDetails.Token = authenticationToken;
            }
            else
            {
                authenticationDetails.Token = authenticationToken.Substring(TOKEN_PREFIX.Length, (authenticationToken.IndexOf("||") - TOKEN_PREFIX.Length));
                authenticationDetails.PartnerUrl = authenticationToken.Substring((authenticationToken.IndexOf("||") + 2), (authenticationToken.Length - (authenticationToken.IndexOf("||") + 2)));
            }

            return authenticationDetails;
        }

        public static HttpClient CreateHttpClient(String token)
        {
            HttpClient httpClient = null;

            httpClient = new HttpClient();

            // Put the token into the authorization header
            httpClient.DefaultRequestHeaders.Add(HEADER_AUTHORIZATION, "Bearer " + token);

            // Set the timeout for the request
            httpClient.Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS);

            return httpClient;
        }
    }

    public class AuthenticationDetails
    {
        public String Token
        {
            get;
            set;
        }

        public String PartnerUrl
        {
            get;
            set;
        }
    }
}