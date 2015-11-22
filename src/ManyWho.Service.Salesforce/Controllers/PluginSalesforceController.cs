using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Describe;
using ManyWho.Flow.SDK.Draw.Elements.Type;
using ManyWho.Flow.SDK.Run.Elements.Config;
using ManyWho.Flow.SDK.Run.Elements.Map;
using ManyWho.Flow.SDK.Run.Elements.Type;
using ManyWho.Flow.SDK.Security;
using ManyWho.Flow.SDK.Social;
using ManyWho.Flow.SDK.Utils;
using ManyWho.Service.Salesforce.Models.Canvas;
using ManyWho.Service.Salesforce.Singletons;
using ManyWho.Service.Salesforce.Utils;
using Microsoft.AspNet.Mvc;
using Newtonsoft.Json;

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

namespace ManyWho.Service.Salesforce.Controllers
{
    public class PluginSalesforceController : Controller
    {
        [HttpGet]
        [ActionName("Health")]
        public String Health()
        {
            return "OK";
        }

        [HttpGet]
        [ActionName("TaskEmailOutcomeResponse")]
        public string TaskEmailOutcomeResponse(String token, String selectedOutcomeId, String redirectUri = null)
        {
            IAuthenticatedWho authenticatedWho = null;
            INotifier notifier = null;
            HttpResponseMessage response = null;
            ServiceResponseAPI serviceResponse = null;
            ServiceRequestAPI serviceRequest = null;
            String invokeType = null;
            String responseContent = null;

            try
            {
                if (String.IsNullOrWhiteSpace(token) == true)
                {
                    throw new ArgumentNullException("Token", "The token for the request is null or blank.");
                }

                if (String.IsNullOrWhiteSpace(selectedOutcomeId) == true)
                {
                    throw new ArgumentNullException("SelectedOutcomeId", "The selected outcome for the request is null or blank.");
                }

                // Get the email verification for this tracking code
                serviceRequest = JsonConvert.DeserializeObject<ServiceRequestAPI>(StorageUtils.GetStoredJson(token.ToLower()));

                if (serviceRequest == null)
                {
                    throw new ArgumentNullException("ServiceRequest", "The request has already been processed.");
                }

                // Get the notifier email
                authenticatedWho = new AuthenticatedWho();
                authenticatedWho.Email = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_ADMIN_EMAIL, serviceRequest.configurationValues, true);

                // Create the notifier
                notifier = EmailNotifier.GetInstance(serviceRequest.tenantId, authenticatedWho, null, "TaskEmailOutcomeResponse");

                // Create the service response to send back to ManyWho based on this outcome click
                serviceResponse = new ServiceResponseAPI();
                serviceResponse.invokeType = ManyWhoConstants.INVOKE_TYPE_FORWARD;
                serviceResponse.tenantId = serviceRequest.tenantId;
                serviceResponse.token = serviceRequest.token;
                serviceResponse.selectedOutcomeId = selectedOutcomeId;

                // Invoke the response on the manywho service
                invokeType = RunSingleton.GetInstance().Response(notifier, null, serviceRequest.tenantId, serviceRequest.callbackUri, serviceResponse);

                if (invokeType == null ||
                    invokeType.Trim().Length == 0)
                {
                    throw new ArgumentNullException("ServiceRequest", "The invokeType coming back from ManyWho cannot be null or blank.");
                }

                if (invokeType.IndexOf(ManyWhoConstants.INVOKE_TYPE_SUCCESS, StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    // The system has accepted our task email response so the token is now dead - we remove it from storage
                    StorageUtils.RemoveStoredJson(token.ToLower());
                }
                else
                {
                    // The system has not accepted our task email response, so we should simply keep waiting and responding to emails with this task token
                }

                // Tell the user the outcome selection was successful
                responseContent = "Your request has been successfully completed. Please close this window.";

                if (String.IsNullOrWhiteSpace(redirectUri) == false)
                {
                    // Redirect the user as specified
                    Response.Redirect(redirectUri);
                }

                // Send the user a response page
                return responseContent;
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("Canvas")]
        public void Canvas(String tenantId, String flowId, String playerUrl)
        {
            String redirectUrl = null;
            String signedRequest = null;
            CanvasRequest canvasRequest = null;
            HttpResponseMessage response = null;

            try
            {
                // Get the signed request from the form post
                signedRequest = Request.Form["signed_request"];

                // Grab the canvas request object from the post
                // The secret needs to be stored somewhere - actually, it doesn't - we don't need the secret at all
                canvasRequest = SalesforceCanvasUtils.VerifyAndDecode(null, signedRequest, "6156156167154975556");

                if (flowId == null ||
                    flowId.Trim().Length == 0)
                {
                    throw new ArgumentNullException("BadRequest", "A flow identifier is required.  Please pass in a parameter for \"flow-id\".");
                }

                if (tenantId == null ||
                    tenantId.Trim().Length == 0)
                {
                    throw new ArgumentNullException("BadRequest", "A tenant identifier is required.  Please pass in a parameter for \"tenant-id\".");
                }

                if (playerUrl == null ||
                    playerUrl.Trim().Length == 0)
                {
                    throw new ArgumentNullException("BadRequest", "A player is required.  Please pass in a parameter for \"player-url\".");
                }

                // Construct the redirect url so the player knows what to do
                redirectUrl = "";
                redirectUrl += SettingUtils.GetStringSetting(SettingUtils.SETTING_SERVER_BASE_PATH) + "/" + tenantId + "/play/" + playerUrl;
                redirectUrl += "?session-token=" + canvasRequest.client.oauthToken;
                redirectUrl += "&session-url=" + WebUtility.HtmlEncode(canvasRequest.client.instanceUrl + canvasRequest.context.links.partnerUrl);

                // Create the run url stuff using utils
                redirectUrl = RunUtils.CompleteRunUrl(redirectUrl, Guid.Parse(flowId));

                // Tell the caller to redirect back to the desired location
                Response.Redirect(redirectUrl);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpGet]
        [ActionName("SessionSignIn")]
        public void SessionSignIn(String tenantId, String flowId, String playerUrl, String sessionId, String sessionUrl)
        {
            String redirectUrl = null;
            HttpResponseMessage response = null;

            try
            {
                if (flowId == null ||
                    flowId.Trim().Length == 0)
                {
                    throw new ArgumentNullException("BadRequest", "A flow identifier is required.  Please pass in a parameter for \"flow-id\".");
                }

                if (tenantId == null ||
                    tenantId.Trim().Length == 0)
                {
                    throw new ArgumentNullException("BadRequest", "A tenant identifier is required.  Please pass in a parameter for \"tenant-id\".");
                }

                if (playerUrl == null ||
                    playerUrl.Trim().Length == 0)
                {
                    throw new ArgumentNullException("BadRequest", "A player is required.  Please pass in a parameter for \"player-url\".");
                }

                // Construct the redirect url so the player knows what to do
                redirectUrl = "";
                redirectUrl += SettingUtils.GetStringSetting(SettingUtils.SETTING_SERVER_BASE_PATH) + "/" + tenantId + "/play/" + playerUrl;
                redirectUrl += "?session-token=" + sessionId;
                redirectUrl += "&session-url=" + sessionUrl;

                // Create the run url stuff using utils
                redirectUrl = RunUtils.CompleteRunUrl(redirectUrl, Guid.Parse(flowId));

                // Tell the caller to redirect back to the desired location
                Response.Redirect(redirectUrl);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("Describe")]
        public DescribeServiceResponseAPI Describe([FromBody] DescribeServiceRequestAPI describeServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().Describe(this.GetWho(), describeServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("DescribeTables")]
        public List<TypeElementBindingAPI> DescribeTables([FromBody] ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().DescribeTables(this.GetWho(), objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("DescribeFields")]
        public List<TypeElementPropertyBindingAPI> DescribeFields([FromBody] ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().DescribeFields(this.GetWho(), objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("Invoke")]
        public ServiceResponseAPI Invoke(String actionName, [FromBody] ServiceRequestAPI serviceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().Invoke(EmailNotifier.GetInstance(this.GetWho(), serviceRequest.configurationValues, "PluginSalesforceController.Invoke"), this.GetWho(), actionName, serviceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("View")]
        public UIServiceResponseAPI View(String action, [FromBody] UIServiceRequestAPI uiServiceRequest)
        {
            return null;
        }

        [HttpPost]
        [ActionName("Vote")]
        public VoteResponseAPI Vote([FromBody] VoteRequestAPI voteRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().Vote(EmailNotifier.GetInstance(this.GetWho(), voteRequestAPI.configurationValues, "PluginSalesforceController.Vote"), this.GetWho(), voteRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("Listener")]
        public void Listener([FromBody] ListenerServiceRequestAPI listenerServiceRequestAPI)
        {
            try
            {
                SalesforceServiceSingleton.GetInstance().Listen(EmailNotifier.GetInstance(this.GetWho(), listenerServiceRequestAPI.configurationValues, "PluginSalesforceController.Listener"), this.GetWho(), listenerServiceRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("Notification")]
        public void Notification([FromBody] ServiceNotificationRequestAPI serviceNotificationRequestAPI)
        {
            try
            {
                SalesforceServiceSingleton.GetInstance().Notify(EmailNotifier.GetInstance(this.GetWho(), serviceNotificationRequestAPI.configurationValues, "PluginSalesforceController.Notification"), this.GetWho(), serviceNotificationRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPut]
        [ActionName("Save")]
        public ObjectDataResponseAPI Save([FromBody] ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().Save(EmailNotifier.GetInstance(this.GetWho(), objectDataRequestAPI.configurationValues, "PluginSalesforceController.Save"), this.GetWho(), objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("Load")]
        public ObjectDataResponseAPI Load([FromBody] ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().Load(this.GetWho(), objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("Delete")]
        public ObjectDataResponseAPI Delete([FromBody] ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().Delete(objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("LoadFiles")]
        public ObjectDataResponseAPI LoadFiles([FromBody] FileDataRequestAPI fileDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().LoadFiles(this.GetWho(), fileDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("DeleteFile")]
        public ObjectDataResponseAPI DeleteFile([FromBody] FileDataRequestAPI fileDataRequestAPI)
        {
            try
            {
                throw new NotImplementedException("File delete is not currently enabled.");
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("UploadFile")]
        public Task<ObjectDataResponseAPI> UploadFile()
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().UploadFile(this.GetWho(), Request.Form);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("GetUserInAuthorizationContext")]
        public ObjectDataResponseAPI GetUserInAuthorizationContext([FromBody] ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().GetUserInAuthorizationContext(EmailNotifier.GetInstance(this.GetWho(), objectDataRequestAPI.configurationValues, "PluginSalesforceController.GetUserInAuthorizationContext"), this.GetWho(), objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("LoadUsers")]
        public ObjectDataResponseAPI LoadUsers([FromBody] ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().LoadUsers(this.GetWho(), objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("LoadUserAttributes")]
        public ObjectDataResponseAPI LoadUserAttributes([FromBody] ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().LoadUserAttributes(objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("LoadGroups")]
        public ObjectDataResponseAPI LoadGroups([FromBody] ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().LoadGroups(this.GetWho(), objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("LoadGroupAttributes")]
        public ObjectDataResponseAPI LoadGroupAttributes([FromBody] ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().LoadGroupAttributes(objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("Login")]
        public AuthenticatedWhoResultAPI Login([FromBody] AuthenticationCredentialsAPI authenticationCredentialsAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().Login(authenticationCredentialsAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("CreateStream")]
        public String CreateStream([FromBody] SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().CreateStream(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.CreateStream"), this.GetWho(), socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("GetCurrentUserInfo")]
        public WhoAPI GetCurrentUserInfo(String streamId, [FromBody] SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().GetCurrentUserInfo(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.GetCurrentUserInfo"), this.GetWho(), streamId, socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("GetUserInfo")]
        public WhoAPI GetUserInfo(String streamId, String userId, [FromBody] SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().GetUserInfo(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.GetUserInfo"), this.GetWho(), streamId, userId, socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("GetStreamFollowers")]
        public List<WhoAPI> GetStreamFollowers(String streamId, [FromBody] SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().GetStreamFollowers(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.GetStreamFollowers"), this.GetWho(), streamId, socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("GetStreamMessages")]
        public Task<MessageListAPI> GetStreamMessages(String streamId, [FromBody] SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().GetStreamMessages(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.GetStreamMessages"), this.GetWho(), streamId, socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("ShareMessage")]
        public Task<MessageAPI> ShareMessage(String streamId)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().ShareMessage(this.GetWho(), streamId, Request.Form);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("PostNewMessage")]
        public Task<MessageAPI> PostNewMessage(String streamId)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().PostNewMessage(this.GetWho(), streamId, Request.Form);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("DeleteMessage")]
        public Task<String> DeleteMessage(String streamId, String messageId, [FromBody] SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().DeleteMessage(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.DeleteMessage"), this.GetWho(), streamId, messageId, socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("LikeMessage")]
        public Task<String> LikeMessage(String streamId, String messageId, String like, [FromBody] SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().LikeMessage(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.LikeMessage"), this.GetWho(), streamId, messageId, Boolean.Parse(like), socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("FollowStream")]
        public Task<String> FollowStream(String streamId, String follow, [FromBody] SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().FollowStream(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.FollowStream"), this.GetWho(), streamId, Boolean.Parse(follow), socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("SearchUsersByName")]
        public Task<List<MentionedWhoAPI>> SearchUsersByName(String streamId, String name, [FromBody] SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().SearchUsersByName(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.SearchUsersByName"), this.GetWho(), streamId, name, socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        private IAuthenticatedWho GetWho()
        {
            IAuthenticatedWho authenticatedWho = null;
            String authorizationHeader = null;

            // Get the authorization header from this invoke request
            authorizationHeader = Request.Headers[HttpUtils.HEADER_AUTHORIZATION];

            if (authorizationHeader == null)
            {
                throw new ArgumentNullException("BadRequest", "Not authorized to invoke this service.");
            }

            // Check to make sure the authorization header parses OK
            authenticatedWho = AuthenticationUtils.Deserialize(WebUtility.UrlDecode(authorizationHeader));

            return authenticatedWho;
        }
    }
}
