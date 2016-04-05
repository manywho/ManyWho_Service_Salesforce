using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Security;
using ManyWho.Flow.SDK.Utils;

///*!

//Copyright 2013 Manywho, Inc.

//Licensed under the Manywho License, Version 1.0 (the "License"); you may not use this
//file except in compliance with the License.

//You may obtain a copy of the License at: http://manywho.com/sharedsource

//Unless required by applicable law or agreed to in writing, software distributed under
//the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
//KIND, either express or implied. See the License for the specific language governing
//permissions and limitations under the License.

//*/

namespace ManyWho.Service.Salesforce.Utils
{
    public class BaseHttpUtils
    {
        public const Int32 MAXIMUM_RETRIES = 3;
        public const Int32 TIMEOUT_SECONDS = 20;
        public const Int32 SYSTEM_TIMEOUT_SECONDS = 500;

        public const String HEADER_AUTHORIZATION = "Authorization";
        public const String HEADER_MANYWHO_STATE = "ManyWhoState";
        public const String HEADER_MANYWHO_TENANT = "ManyWhoTenant";
        public const String HEADER_CULTURE = "Culture";

        /// <summary>
        /// Utility method for getting the authenticated who from the header.
        /// </summary>
        public static IAuthenticatedWho GetWho(String authorizationHeader)
        {
            IAuthenticatedWho authenticatedWho = null;

            // Check to see if it's null - it can be in some situations
            if (authorizationHeader != null &&
                authorizationHeader.Trim().Length > 0)
            {
                // Deserialize into an object
                authenticatedWho = AuthenticationUtils.Deserialize(Uri.EscapeDataString(authorizationHeader));
            }

            return authenticatedWho;
        }

        public static WebException HandleUnsuccessfulHttpResponseMessage(INotifier notifier, IAuthenticatedWho authenticatedWho, Int32 iteration, HttpResponseMessage httpResponseMessage, String endpointUrl)
        {
            WebException webException = null;

            if (iteration >= (MAXIMUM_RETRIES - 1))
            {
                // The the alert email the fault
                ErrorUtils.SendAlert(notifier, authenticatedWho, "Fault", "The system has attempted multiple retries (" + MAXIMUM_RETRIES + ") with no luck on: " + endpointUrl + ". The status code is: " + httpResponseMessage.StatusCode + ". The reason is: " + httpResponseMessage.ReasonPhrase);

                // Throw the fault up to the caller
                webException = new WebException(httpResponseMessage.ReasonPhrase);
            }
            else
            {
                // Alert the admin that a retry has happened
                ErrorUtils.SendAlert(notifier, authenticatedWho, "Warning", "The system is attempting a retry (" + iteration + ") on: " + endpointUrl + ". The status code is: " + httpResponseMessage.StatusCode + ". The reason is: " + httpResponseMessage.ReasonPhrase);
            }

            return webException;
        }

        public static WebException HandleHttpException(INotifier notifier, IAuthenticatedWho authenticatedWho, Int32 iteration, Exception exception, String endpointUrl)
        {
            WebException webException = null;

            if (iteration >= (MAXIMUM_RETRIES - 1))
            {
                // The the alert email the fault
                ErrorUtils.SendAlert(notifier, authenticatedWho, "Fault", "The system has attempted multiple retries (" + MAXIMUM_RETRIES + ") with no luck on: " + endpointUrl + ". The error message we're getting back is: " + GetExceptionMessage(exception));

                // Throw the fault up to the caller
                webException = new WebException(GetExceptionMessage(exception));
            }
            else
            {
                // Alert the admin that a retry has happened
                ErrorUtils.SendAlert(notifier, authenticatedWho, "Warning", "The system is attempting a retry (" + iteration + ") on: " + endpointUrl + ". The error message we're getting back is: " + GetExceptionMessage(exception));
            }

            return webException;
        }

        public static HttpClient CreateHttpClient(IAuthenticatedWho authenticatedWho, String tenantId, String stateId)
        {
            return CreateHttpClient(authenticatedWho, tenantId, stateId, TIMEOUT_SECONDS);
        }

        public static HttpClient CreateHttpClient(IAuthenticatedWho authenticatedWho, String tenantId, String stateId, Int32 timeOut)
        {
            HttpClient httpClient = null;

            httpClient = new HttpClient();

            if (authenticatedWho != null)
            {
                // Serialize and add the user information to the header
                httpClient.DefaultRequestHeaders.Add(HEADER_AUTHORIZATION, Uri.EscapeDataString(AuthenticationUtils.Serialize(authenticatedWho)));
            }

            if (tenantId != null &&
                tenantId.Trim().Length > 0)
            {
                // Add the tenant to the header
                httpClient.DefaultRequestHeaders.Add(HEADER_MANYWHO_TENANT, tenantId);
            }

            if (stateId != null &&
                stateId.Trim().Length > 0)
            {
                // Add the state to the header
                httpClient.DefaultRequestHeaders.Add(HEADER_MANYWHO_STATE, stateId);
            }

            // Set the timeout for the request
            httpClient.Timeout = TimeSpan.FromSeconds(timeOut);

            return httpClient;
        }

        public static HttpResponseException GetWebException(HttpStatusCode statusCode, String reasonPhrase)
        {
            HttpResponseException httpResponseException = null;
            HttpResponseMessage httpResponseMessage = null;

            // Create the new http response message with the status code
            httpResponseMessage = new HttpResponseMessage(statusCode);

            // Reason phrases cannot have carriage returns
            httpResponseMessage.ReasonPhrase = reasonPhrase.Replace("\n", " ").Replace("\r", " ").Replace(Environment.NewLine, " ");

            // Add the response message to the exception
            httpResponseException = new HttpResponseException(httpResponseMessage);

            return httpResponseException;
        }

        public static String GetModeFromQuery(Uri uri)
        {
            String mode = null;

            // Check to see if the caller passed in the mode
            if (uri.Query != null)
            {
                if (uri.Query.IndexOf(ManyWhoConstants.MODE_DEBUG_STEPTHROUGH, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    mode = ManyWhoConstants.MODE_DEBUG_STEPTHROUGH;
                }
                else if (uri.Query.IndexOf(ManyWhoConstants.MODE_DEBUG, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    mode = ManyWhoConstants.MODE_DEBUG;
                }
            }
            
            return mode;
        }

        public static String GetReportingModeFromQuery(Uri uri)
        {
            String reportingMode = null;

            // Check to see if the caller passed in the mode
            if (uri.Query != null)
            {
                if (uri.Query.IndexOf(ManyWhoConstants.REPORT_PATH_AND_VALUES, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    reportingMode = ManyWhoConstants.REPORT_PATH_AND_VALUES;
                }
                else if (uri.Query.IndexOf(ManyWhoConstants.REPORT_PATH, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    reportingMode = ManyWhoConstants.REPORT_PATH;
                }
                else if (uri.Query.IndexOf(ManyWhoConstants.REPORT_VALUES, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    reportingMode = ManyWhoConstants.REPORT_VALUES;
                }
            }

            return reportingMode;
        }

        public static String GetEmailFromQuery(Uri uri)
        {
            NameValueCollection nameValueCollection = null;
            String email = null;

            if (uri != null)
            {
                nameValueCollection = uri.ParseQueryString();

                if (nameValueCollection != null)
                {
                    // Get the email from the collection
                    email = nameValueCollection.Get("email");
                }
            }

            return email;
        }

                public static void CleanUpHttp(HttpClient httpClient, HttpContent httpContent, HttpResponseMessage httpResponseMessage)
        {
            if (httpClient != null)
            {
                httpClient.Dispose();
                httpClient = null;
            }

            if (httpContent != null)
            {
                httpContent.Dispose();
                httpContent = null;
            }

            if (httpResponseMessage != null)
            {
                httpResponseMessage.Dispose();
                httpResponseMessage = null;
            }
        }
        public static HttpResponseException GetWebException(HttpStatusCode statusCode, Exception exception)
        {
            // Aggregate the exception and return as a single reason phrase
            return GetWebException(statusCode, AggregateAndWriteErrorMessage(exception, "", false));
        }

        public static String GetExceptionMessage(Exception exception)
        {
            return AggregateAndWriteErrorMessage(exception, "", false);
        }

        public static String GetExceptionMessage(Exception exception, Boolean includeStackTrace)
        {
            return AggregateAndWriteErrorMessage(exception, "", includeStackTrace);
        }

        private static String AggregateAndWriteErrorMessage(Exception exception, String message, Boolean includeTrace)
        {
            if (exception != null)
            {
                if (exception is AggregateException)
                {
                    message = AggregateAndWriteAggregateErrorMessage((AggregateException)exception, message, includeTrace);
                }
                else if (exception is WebException)
                {
                    message = AggregateAndWriteHttpResponseErrorMessage((WebException)exception, message);
                }
                else
                {
                    message = AggregateAndWriteExceptionErrorMessage(exception, message, includeTrace);
                }
            }

            return message;
        }

        private static String AggregateAndWriteAggregateErrorMessage(Exception exception, String message, Boolean includeTrace)
        {
            if (exception is AggregateException)
            {
                AggregateException aex = (AggregateException)exception;

                message += "The exception is an aggregate of the following exceptions:" + Environment.NewLine + Environment.NewLine;

                if (aex.InnerExceptions != null &&
                    aex.InnerExceptions.Any())
                {
                    foreach (Exception innerException in aex.InnerExceptions)
                    {
                        if (innerException is AggregateException)
                        {
                            message = AggregateAndWriteAggregateErrorMessage((AggregateException)innerException, message, includeTrace);
                        }
                        else if (innerException is WebException)
                        {
                            message = AggregateAndWriteHttpResponseErrorMessage((WebException)innerException, message);
                        }
                        else
                        {
                            message = AggregateAndWriteErrorMessage(innerException, message, includeTrace);
                        }
                    }
                }
            }

            return message;
        }

        private static String AggregateAndWriteHttpResponseErrorMessage(WebException exception, String message)
        {
            WebResponse webResponse = null;
            String statusDescription = null;

            if (exception != null)
            {
                if (exception.Response != null)
                {
                    webResponse = exception.Response;

                    if (webResponse is HttpWebResponse)
                    {
                        statusDescription = ((HttpWebResponse)webResponse).StatusDescription;

                        // Grab the message from the 
                        if (statusDescription != null &&
                            statusDescription.Trim().Length > 0)
                        {
                            message += "HttpResponseException:" + Environment.NewLine;
                            message += statusDescription + Environment.NewLine + Environment.NewLine;
                        }
                    }
                }
                else
                {
                    message += exception.Message;
                }
            }

            return message;
        }

        private static String AggregateAndWriteExceptionErrorMessage(Exception exception, String message, Boolean includeTrace)
        {
            if (exception != null)
            {
                message += "Exception:" + Environment.NewLine;
                message += exception.Message + Environment.NewLine + Environment.NewLine;

                if (includeTrace == true)
                {
                    message += "Stack Trace:" + Environment.NewLine;
                    message += exception.StackTrace + Environment.NewLine + Environment.NewLine;
                }
            }

            return message;
        }
    }
}
