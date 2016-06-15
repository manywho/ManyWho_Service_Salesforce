using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Polly;
using Newtonsoft.Json;
using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Utils;
using ManyWho.Flow.SDK.Draw.Elements.Value;
using ManyWho.Flow.SDK.Draw.Elements.Config;
using ManyWho.Service.Salesforce.Utils;

namespace ManyWho.Service.Salesforce.Singletons
{
    public class SalesforceHttpSingleton
    {
        public static String CONSUMER_KEY = "fromconfig";
        public static String CONSUMER_SECRET = "fromconfig";

        private static SalesforceHttpSingleton salesforceHttpSingleton;

        private SalesforceHttpSingleton()
        {

        }

        public static SalesforceHttpSingleton GetInstance()
        {
            if (salesforceHttpSingleton == null)
            {
                salesforceHttpSingleton = new SalesforceHttpSingleton();
            }

            return salesforceHttpSingleton;
        }

        public String GetOAuth2InstallUrl(String authenticationUrl, String tenantId, String authorToken, String serviceElementId)
        {
            String oauth2Url = "";

            if (String.IsNullOrWhiteSpace(authenticationUrl))
            {
                throw new ArgumentNullException("AuthenticationUrl", "The AuthenticationUrl cannot be null when installing the Salesforce Service.");
            }

            if (String.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentNullException("TenantId", "The TenantId cannot be null when installing the Salesforce Service.");
            }

            if (String.IsNullOrWhiteSpace(authorToken))
            {
                throw new ArgumentNullException("AuthorToken", "The AuthorToken cannot be null when installing the Salesforce Service.");
            }

            // Create the base OAuth2 url
            oauth2Url = String.Format(authenticationUrl + "/services/oauth2/authorize?response_type=code&client_id={0}", Uri.EscapeDataString(CONSUMER_KEY));

            // Create the state so we have the necessary info when the response returns
            oauth2Url += "&state=";
            oauth2Url += "AuthenticationUrl____" + Uri.EscapeDataString(authenticationUrl) + "____AuthenticationUrl";
            oauth2Url += "TenantId____" + tenantId + "____TenantId";
            oauth2Url += "Token____" + Uri.EscapeDataString(authorToken) + "____Token";

            if (String.IsNullOrWhiteSpace(serviceElementId) == false)
            {
                oauth2Url += "ServiceElementId____" + serviceElementId + "____ServiceElementId";
            }

            // Make sure Salesforce redirects back to the correct place
            oauth2Url += "&redirect_uri=" + SettingUtils.GetStringSetting("Salesforce.ServerBasePath") + "/plugins/api/salesforce/1/oauth2";
            //oauth2Url += "&redirect_uri=http://localhost:20385/plugins/api/salesforce/1/oauth2";

            return oauth2Url;
        }

        public String GetAuthenticationUrlFromOAuthState(String state)
        {
            if (state != null &&
                state.IndexOf("AuthenticationUrl____", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Int32 start = state.IndexOf("AuthenticationUrl____", StringComparison.OrdinalIgnoreCase) + "AuthenticationUrl____".Length;
                Int32 end = state.IndexOf("____AuthenticationUrl", StringComparison.OrdinalIgnoreCase) - start;

                return state.Substring(start, end);
            }

            return null;
        }

        public String GetTenantIdFromOAuthState(String state)
        {
            if (state != null &&
                state.IndexOf("TenantId____", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Int32 start = state.IndexOf("TenantId____", StringComparison.OrdinalIgnoreCase) + "TenantId____".Length;
                Int32 end = state.IndexOf("____TenantId", StringComparison.OrdinalIgnoreCase) - start;

                return state.Substring(start, end);
            }

            return null;
        }

        public String GetServiceElementIdFromOAuthState(String state)
        {
            if (state != null &&
                state.IndexOf("ServiceElementId____", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Int32 start = state.IndexOf("ServiceElementId____", StringComparison.OrdinalIgnoreCase) + "ServiceElementId____".Length;
                Int32 end = state.IndexOf("____ServiceElementId", StringComparison.OrdinalIgnoreCase) - start;

                return state.Substring(start, end);
            }

            return null;
        }

        public String GetAuthorTokenFromOAuthState(String state)
        {
            if (state != null &&
                state.IndexOf("Token____", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Int32 start = state.IndexOf("Token____", StringComparison.OrdinalIgnoreCase) + "Token____".Length;
                Int32 end = state.IndexOf("____Token", StringComparison.OrdinalIgnoreCase) - start;

                return Uri.UnescapeDataString(state.Substring(start, end));
            }

            return null;
        }

        public ValueElementResponseAPI SaveValue(String authorToken, String tenantId, ValueElementRequestAPI valueElementRequest)
        {
            ValueElementResponseAPI valueElementResponse = null;
            HttpResponseMessage httpResponseMessage = null;
            HttpClient httpClient = null;
            HttpContent httpContent = null;

            if (String.IsNullOrWhiteSpace(authorToken))
            {
                throw new ArgumentNullException("AuthorToken", "The AuthorToken cannot be null when installing the Salesforce Service using OAuth2.");
            }

            if (String.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentNullException("TenantId", "The TenantId cannot be null when installing the Salesforce Service using OAuth2.");
            }

            httpClient = new HttpClient();

            // Serialize and add the author information to the header
            httpClient.DefaultRequestHeaders.Add(HttpUtils.HEADER_AUTHORIZATION, Uri.EscapeDataString(authorToken));
            httpClient.DefaultRequestHeaders.Add(HttpUtils.HEADER_MANYWHO_TENANT, tenantId);

            // Set the timeout for the request
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            String endpointUrl = null;

            Policy.Handle<Exception>().Retry(HttpUtils.MAXIMUM_RETRIES).Execute(() =>
            {
                using (httpClient)
                {
                    // Use the JSON formatter to create the content of the request body.
                    httpContent = new StringContent(JsonConvert.SerializeObject(valueElementRequest));
                    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    // Construct the URL for the save
                    endpointUrl = DrawSingleton.MANYWHO_BASE_URL + DrawSingleton.MANYWHO_DRAW_URI_PART_VALUE_ELEMENT;
                    //endpointUrl = "http://localhost:22935" + DrawSingleton.MANYWHO_DRAW_URI_PART_VALUE_ELEMENT;

                    // Send the value element data to save over to the service
                    httpResponseMessage = httpClient.PostAsync(endpointUrl, httpContent).Result;

                    // Check the status of the response and respond appropriately
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        // Get the value element response back from the save
                        valueElementResponse = JsonConvert.DeserializeObject<ValueElementResponseAPI>(httpResponseMessage.Content.ReadAsStringAsync().Result);
                    }
                    else
                    {
                        throw new ArgumentNullException("Value", "The Salesforce Service is unable to save a configuration Value needed to install the Service.");
                    }
                }
            });

            return valueElementResponse;
        }

        public ValueElementResponseAPI LoadValue(String authorToken, String tenantId, ValueElementIdAPI valueElementId)
        {
            ValueElementResponseAPI valueElementResponse = null;
            HttpResponseMessage httpResponseMessage = null;
            HttpClient httpClient = null;

            if (String.IsNullOrWhiteSpace(authorToken))
            {
                throw new ArgumentNullException("AuthorToken", "The AuthorToken cannot be null when installing the Salesforce Service using OAuth2.");
            }

            if (String.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentNullException("TenantId", "The TenantId cannot be null when installing the Salesforce Service using OAuth2.");
            }

            if (valueElementId == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(valueElementId.typeElementPropertyId) == false)
            {
                throw new ArgumentNullException("ValueElementId", "The ValueElementId cannot be part of an object when installing the Salesforce Service using OAuth2.");
            }

            httpClient = new HttpClient();

            // Serialize and add the author information to the header
            httpClient.DefaultRequestHeaders.Add(HttpUtils.HEADER_AUTHORIZATION, Uri.EscapeDataString(authorToken));
            httpClient.DefaultRequestHeaders.Add(HttpUtils.HEADER_MANYWHO_TENANT, tenantId);

            // Set the timeout for the request
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            String endpointUrl = null;

            Policy.Handle<Exception>().Retry(HttpUtils.MAXIMUM_RETRIES).Execute(() =>
            {
                using (httpClient)
                {
                    // Construct the URL for the load
                    endpointUrl = DrawSingleton.MANYWHO_BASE_URL + DrawSingleton.MANYWHO_DRAW_URI_PART_VALUE_ELEMENT + "/" + valueElementId;
                    //endpointUrl = "http://localhost:22935" + DrawSingleton.MANYWHO_DRAW_URI_PART_VALUE_ELEMENT + "/" + valueElementId;

                    // Send the value element data to save over to the service
                    httpResponseMessage = httpClient.GetAsync(endpointUrl).Result;

                    // Check the status of the response and respond appropriately
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        // Get the value element response back from the load
                        valueElementResponse = JsonConvert.DeserializeObject<ValueElementResponseAPI>(httpResponseMessage.Content.ReadAsStringAsync().Result);
                    }
                    else
                    {
                        throw new ArgumentNullException("Value", "The Salesforce Service is unable to load a configuration Value needed to install the Service.");
                    }
                }
            });

            return valueElementResponse;
        }

        public ServiceElementResponseAPI SaveService(String authorToken, String tenantId, ServiceElementRequestAPI serviceElementRequest)
        {
            ServiceElementResponseAPI serviceElementResponse = null;
            HttpResponseMessage httpResponseMessage = null;
            HttpClient httpClient = null;
            HttpContent httpContent = null;

            if (String.IsNullOrWhiteSpace(authorToken))
            {
                throw new ArgumentNullException("AuthorToken", "The AuthorToken cannot be null when installing the Salesforce Service using OAuth2.");
            }

            if (String.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentNullException("TenantId", "The TenantId cannot be null when installing the Salesforce Service using OAuth2.");
            }

            httpClient = new HttpClient();

            // Serialize and add the author information to the header
            httpClient.DefaultRequestHeaders.Add(HttpUtils.HEADER_AUTHORIZATION, Uri.EscapeDataString(authorToken));
            httpClient.DefaultRequestHeaders.Add(HttpUtils.HEADER_MANYWHO_TENANT, tenantId);

            // Set the timeout for the request
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            String endpointUrl = null;

            Policy.Handle<Exception>().Retry(HttpUtils.MAXIMUM_RETRIES).Execute(() =>
            {
                using (httpClient)
                {
                    // Use the JSON formatter to create the content of the request body.
                    httpContent = new StringContent(JsonConvert.SerializeObject(serviceElementRequest));
                    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    // Construct the URL for the save
                    endpointUrl = DrawSingleton.MANYWHO_BASE_URL + DrawSingleton.MANYWHO_DRAW_URI_PART_SERVICE_ELEMENT;
                    //endpointUrl = "http://localhost:22935" + DrawSingleton.MANYWHO_DRAW_URI_PART_SERVICE_ELEMENT;

                    // Send the value element data to save over to the service
                    httpResponseMessage = httpClient.PostAsync(endpointUrl, httpContent).Result;

                    // Check the status of the response and respond appropriately
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        // Get the value element response back from the save
                        serviceElementResponse = JsonConvert.DeserializeObject<ServiceElementResponseAPI>(httpResponseMessage.Content.ReadAsStringAsync().Result);
                    }
                    else
                    {
                        throw new ArgumentNullException("Service", "The Salesforce Service is unable to save.");
                    }
                }
            });

            return serviceElementResponse;
        }

        public ServiceElementResponseAPI LoadService(String authorToken, String tenantId, String serviceElementId)
        {
            ServiceElementResponseAPI serviceElementResponse = null;
            HttpResponseMessage httpResponseMessage = null;
            HttpClient httpClient = null;

            if (String.IsNullOrWhiteSpace(authorToken))
            {
                throw new ArgumentNullException("AuthorToken", "The AuthorToken cannot be null when installing the Salesforce Service using OAuth2.");
            }

            if (String.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentNullException("TenantId", "The TenantId cannot be null when installing the Salesforce Service using OAuth2.");
            }

            if (String.IsNullOrWhiteSpace(serviceElementId))
            {
                throw new ArgumentNullException("ServiceElementId", "The ServiceElementId cannot be null when installing the Salesforce Service using OAuth2.");
            }

            httpClient = new HttpClient();

            // Serialize and add the author information to the header
            httpClient.DefaultRequestHeaders.Add(HttpUtils.HEADER_AUTHORIZATION, Uri.EscapeDataString(authorToken));
            httpClient.DefaultRequestHeaders.Add(HttpUtils.HEADER_MANYWHO_TENANT, tenantId);

            // Set the timeout for the request
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            String endpointUrl = null;

            Policy.Handle<Exception>().Retry(HttpUtils.MAXIMUM_RETRIES).Execute(() =>
            {
                using (httpClient)
                {
                    // Construct the URL for the load
                    endpointUrl = DrawSingleton.MANYWHO_BASE_URL + DrawSingleton.MANYWHO_DRAW_URI_PART_SERVICE_ELEMENT + "/" + serviceElementId;
                    //endpointUrl = "http://localhost:22935" + DrawSingleton.MANYWHO_DRAW_URI_PART_SERVICE_ELEMENT + "/" + serviceElementId;

                    // Send the service element data to save over to the service
                    httpResponseMessage = httpClient.GetAsync(endpointUrl).Result;

                    // Check the status of the response and respond appropriately
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        // Get the service element response back from the load
                        serviceElementResponse = JsonConvert.DeserializeObject<ServiceElementResponseAPI>(httpResponseMessage.Content.ReadAsStringAsync().Result);
                    }
                    else
                    {
                        throw new ArgumentNullException("Service", "The Salesforce Service is unable to load.");
                    }
                }
            });

            return serviceElementResponse;
        }
    }
}