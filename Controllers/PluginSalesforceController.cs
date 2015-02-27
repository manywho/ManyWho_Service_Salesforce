using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using System.Web;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Utils;
using ManyWho.Flow.SDK.Draw.Flow;
using ManyWho.Flow.SDK.Draw.Content;
using ManyWho.Flow.SDK.Draw.Elements;
using ManyWho.Flow.SDK.Draw.Elements.UI;
using ManyWho.Flow.SDK.Draw.Elements.Map;
using ManyWho.Flow.SDK.Draw.Elements.Type;
using ManyWho.Flow.SDK.Draw.Elements.Value;
using ManyWho.Flow.SDK.Run;
using ManyWho.Flow.SDK.Run.State;
using ManyWho.Flow.SDK.Run.Elements.UI;
using ManyWho.Flow.SDK.Run.Elements.Map;
using ManyWho.Flow.SDK.Run.Elements.Type;
using ManyWho.Flow.SDK.Run.Elements.Config;
using ManyWho.Flow.SDK.Describe;
using ManyWho.Flow.SDK.Security;
using ManyWho.Flow.SDK.Social;
using ManyWho.Service.Salesforce;
using ManyWho.Service.Salesforce.Utils;
using ManyWho.Service.Salesforce.Models.Rest.Enums;
using ManyWho.Service.Salesforce.Models.Rest;
using ManyWho.Service.Salesforce.Models.Canvas;
using ManyWho.Service.ManyWho.Utils.Singletons;

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

namespace ManyWho.Flow.Web.Controllers
{
    public class PluginSalesforceController : ApiController
    {
        public const String SETTING_SERVER_BASE_PATH = "Salesforce.ServerBasePath";
        public const String SETTING_ADMIN_EMAIL = "Salesforce.AdminEmail";

        [HttpGet]
        [ActionName("Health")]
        public String Health()
        {
            return "OK";
        }

        [HttpGet]
        [ActionName("TaskEmailOutcomeResponse")]
        public HttpResponseMessage TaskEmailOutcomeResponse(String token, String selectedOutcomeId, String redirectUri = null)
        {
            IAuthenticatedWho authenticatedWho = null;
            INotifier notifier = null;
            HttpResponseMessage response = null;
            ServiceResponseAPI serviceResponse = null;
            EmailVerification emailVerification = null;
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
                emailVerification = ManyWhoUtilsSingleton.GetInstance().RetrieveTaskRequest(Guid.Parse(token));

                if (emailVerification == null)
                {
                    throw new ArgumentNullException("EmailVerification", "The EmailVerification could not be found for this request.");
                }

                // If the email verification is completed, we simply return as response OK to sendgrid - basically, we ignore the email
                if (emailVerification.IsCompleted == false)
                {
                    if (emailVerification.ServiceRequest == null)
                    {
                        throw new ArgumentNullException("ServiceRequest", "The ServiceRequest object is null in the task persistence.");
                    }

                    if (emailVerification.AuthenticatedWho == null)
                    {
                        throw new ArgumentNullException("AuthenticatedWho", "The AuthenticatedWho object is null in the task persistence.");
                    }

                    // Create the notifier
                    notifier = EmailNotifier.GetInstance(emailVerification.ServiceRequest.tenantId, emailVerification.AuthenticatedWho, null, "TaskEmailOutcomeResponse");

                    // Create the service response to send back to ManyWho based on this outcome click
                    serviceResponse = new ServiceResponseAPI();
                    serviceResponse.invokeType = ManyWhoConstants.INVOKE_TYPE_FORWARD;
                    serviceResponse.tenantId = emailVerification.ServiceRequest.tenantId;
                    serviceResponse.token = emailVerification.ServiceRequest.token;
                    serviceResponse.selectedOutcomeId = selectedOutcomeId;

                    // Get the authenticated who from the verification
                    authenticatedWho = emailVerification.AuthenticatedWho;

                    // Invoke the response on the manywho service
                    invokeType = RunSingleton.GetInstance().Response(notifier, authenticatedWho, emailVerification.ServiceRequest.tenantId, emailVerification.ServiceRequest.callbackUri, serviceResponse);

                    if (invokeType == null ||
                        invokeType.Trim().Length == 0)
                    {
                        throw new ArgumentNullException("ServiceRequest", "The invokeType coming back from ManyWho cannot be null or blank.");
                    }

                    if (invokeType.IndexOf(ManyWhoConstants.INVOKE_TYPE_SUCCESS, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        // The system has accepted our task email response so the token is now dead - we should mark it as completed in our db
                        ManyWhoUtilsSingleton.GetInstance().MarkTaskCompleted(Guid.Parse(emailVerification.ServiceRequest.tenantId), Guid.Parse(token));
                    }
                    else
                    {
                        // The system has not accepted our task email response, so we should simply keep waiting and responding to emails with this task token
                    }

                    // Tell the user the outcome selection was successful
                    responseContent = "Your request has been successfully completed. Please close this window.";
                }
                else
                {
                    // Tell the caller that we got the email response - though that does not mean that we accepted this response as finishing the workflow
                    responseContent = "This request has already been processed. Please close this window.";
                }

                if (String.IsNullOrWhiteSpace(redirectUri) == false)
                {
                    // Redirect the user as specified
                    response = Request.CreateResponse(HttpStatusCode.RedirectMethod, redirectUri);
                    response.Headers.Add("Location", redirectUri);
                }
                else
                {
                    // Send the user a response page
                    response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(responseContent);
                }
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }

            return response;
        }

        [HttpPost]
        [ActionName("Canvas")]
        public HttpResponseMessage Canvas(String tenantId, String flowId, String playerUrl)
        {
            String redirectUrl = null;
            String signedRequest = null;
            CanvasRequest canvasRequest = null;
            HttpResponseMessage response = null;

            try
            {
                // Get the signed request from the form post
                signedRequest = System.Web.HttpContext.Current.Request.Form["signed_request"];

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
                redirectUrl += SettingUtils.GetStringSetting(SETTING_SERVER_BASE_PATH) + "/" + tenantId + "/play/" + playerUrl;
                redirectUrl += "?session-token=" + canvasRequest.client.oauthToken;
                redirectUrl += "&session-url=" + HttpUtility.HtmlEncode(canvasRequest.client.instanceUrl + canvasRequest.context.links.partnerUrl);

                // Create the run url stuff using utils
                redirectUrl = RunUtils.CompleteRunUrl(redirectUrl, Guid.Parse(flowId));

                // Tell the caller to redirect back to the desired location
                response = Request.CreateResponse(HttpStatusCode.RedirectMethod, redirectUrl);
                response.Headers.Add("Location", redirectUrl);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }

            return response;
        }

        [HttpGet]
        [ActionName("SessionSignIn")]
        public HttpResponseMessage SessionSignIn(String tenantId, String flowId, String playerUrl, String sessionId, String sessionUrl)
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
                redirectUrl += SettingUtils.GetStringSetting(SETTING_SERVER_BASE_PATH) + "/" + tenantId + "/play/" + playerUrl;
                redirectUrl += "?session-token=" + sessionId;
                redirectUrl += "&session-url=" + sessionUrl;

                // Create the run url stuff using utils
                redirectUrl = RunUtils.CompleteRunUrl(redirectUrl, Guid.Parse(flowId));

                // Tell the caller to redirect back to the desired location
                response = Request.CreateResponse(HttpStatusCode.RedirectMethod, redirectUrl);
                response.Headers.Add("Location", redirectUrl);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }

            return response;
        }

        [HttpPost]
        [ActionName("Describe")]
        public DescribeServiceResponseAPI Describe(DescribeServiceRequestAPI describeServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().Describe(describeServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("DescribeTables")]
        public List<TypeElementBindingAPI> DescribeTables(ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().DescribeTables(objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("DescribeFields")]
        public List<TypeElementPropertyBindingAPI> DescribeFields(ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().DescribeFields(objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("Invoke")]
        public ServiceResponseAPI Invoke(String actionName, ServiceRequestAPI serviceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().Invoke(EmailNotifier.GetInstance(this.GetWho(), serviceRequest.configurationValues, "PluginSalesforceController.Invoke"), this.GetWho(), actionName, serviceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("View")]
        public UIServiceResponseAPI View(String action, UIServiceRequestAPI uiServiceRequest)
        {
            return null;
        }

        [HttpPost]
        [ActionName("Vote")]
        public VoteResponseAPI Vote(VoteRequestAPI voteRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().Vote(EmailNotifier.GetInstance(this.GetWho(), voteRequestAPI.configurationValues, "PluginSalesforceController.Vote"), this.GetWho(), voteRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("Listener")]
        public void Listener(ListenerServiceRequestAPI listenerServiceRequestAPI)
        {
            try
            {
                SalesforceServiceSingleton.GetInstance().Listen(EmailNotifier.GetInstance(this.GetWho(), listenerServiceRequestAPI.configurationValues, "PluginSalesforceController.Listener"), this.GetWho(), listenerServiceRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("Notification")]
        public void Notification(ServiceNotificationRequestAPI serviceNotificationRequestAPI)
        {
            try
            {
                SalesforceServiceSingleton.GetInstance().Notify(EmailNotifier.GetInstance(this.GetWho(), serviceNotificationRequestAPI.configurationValues, "PluginSalesforceController.Notification"), this.GetWho(), serviceNotificationRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPut]
        [ActionName("Save")]
        public ObjectDataResponseAPI Save(ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().Save(EmailNotifier.GetInstance(this.GetWho(), objectDataRequestAPI.configurationValues, "PluginSalesforceController.Save"), this.GetWho(), objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("Load")]
        public ObjectDataResponseAPI Load(ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().Load(this.GetWho(), objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("Delete")]
        public ObjectDataResponseAPI Delete(ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().Delete(objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("LoadFiles")]
        public ObjectDataResponseAPI LoadFiles(FileDataRequestAPI fileDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().LoadFiles(this.GetWho(), fileDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("DeleteFile")]
        public ObjectDataResponseAPI DeleteFile(FileDataRequestAPI fileDataRequestAPI)
        {
            try
            {
                throw new NotImplementedException("File delete is not currently enabled.");
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("UploadFile")]
        public Task<ObjectDataResponseAPI> UploadFile()
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().UploadFile(this.GetWho(), Request.Content);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("GetUserInAuthorizationContext")]
        public ObjectDataResponseAPI GetUserInAuthorizationContext(ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().GetUserInAuthorizationContext(EmailNotifier.GetInstance(this.GetWho(), objectDataRequestAPI.configurationValues, "PluginSalesforceController.GetUserInAuthorizationContext"), this.GetWho(), objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("LoadUsers")]
        public ObjectDataResponseAPI LoadUsers(ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().LoadUsers(this.GetWho(), objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("LoadUserAttributes")]
        public ObjectDataResponseAPI LoadUserAttributes(ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().LoadUserAttributes(objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("LoadGroups")]
        public ObjectDataResponseAPI LoadGroups(ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().LoadGroups(this.GetWho(), objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("LoadGroupAttributes")]
        public ObjectDataResponseAPI LoadGroupAttributes(ObjectDataRequestAPI objectDataRequestAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().LoadGroupAttributes(objectDataRequestAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("Login")]
        public AuthenticatedWhoResultAPI Login(AuthenticationCredentialsAPI authenticationCredentialsAPI)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().Login(authenticationCredentialsAPI);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("CreateStream")]
        public String CreateStream(SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().CreateStream(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.CreateStream"), this.GetWho(), socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("GetCurrentUserInfo")]
        public WhoAPI GetCurrentUserInfo(String streamId, SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().GetCurrentUserInfo(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.GetCurrentUserInfo"), this.GetWho(), streamId, socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("GetUserInfo")]
        public WhoAPI GetUserInfo(String streamId, String userId, SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().GetUserInfo(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.GetUserInfo"), this.GetWho(), streamId, userId, socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("GetStreamFollowers")]
        public List<WhoAPI> GetStreamFollowers(String streamId, SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().GetStreamFollowers(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.GetStreamFollowers"), this.GetWho(), streamId, socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("GetStreamMessages")]
        public Task<MessageListAPI> GetStreamMessages(String streamId, SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().GetStreamMessages(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.GetStreamMessages"), this.GetWho(), streamId, socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("ShareMessage")]
        public Task<MessageAPI> ShareMessage(String streamId)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().ShareMessage(this.GetWho(), streamId, Request.Content);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("PostNewMessage")]
        public Task<MessageAPI> PostNewMessage(String streamId)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().PostNewMessage(this.GetWho(), streamId, Request.Content);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("DeleteMessage")]
        public Task<String> DeleteMessage(String streamId, String messageId, SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().DeleteMessage(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.DeleteMessage"), this.GetWho(), streamId, messageId, socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("LikeMessage")]
        public Task<String> LikeMessage(String streamId, String messageId, String like, SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().LikeMessage(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.LikeMessage"), this.GetWho(), streamId, messageId, Boolean.Parse(like), socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("FollowStream")]
        public Task<String> FollowStream(String streamId, String follow, SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().FollowStream(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.FollowStream"), this.GetWho(), streamId, Boolean.Parse(follow), socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        [HttpPost]
        [ActionName("SearchUsersByName")]
        public Task<List<MentionedWhoAPI>> SearchUsersByName(String streamId, String name, SocialServiceRequestAPI socialServiceRequest)
        {
            try
            {
                return SalesforceServiceSingleton.GetInstance().SearchUsersByName(EmailNotifier.GetInstance(this.GetWho(), socialServiceRequest.configurationValues, "PluginSalesforceController.SearchUsersByName"), this.GetWho(), streamId, name, socialServiceRequest);
            }
            catch (Exception exception)
            {
                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        private IAuthenticatedWho GetWho()
        {
            IAuthenticatedWho authenticatedWho = null;
            String authorizationHeader = null;

            // Get the authorization header from this invoke request
            authorizationHeader = System.Web.HttpContext.Current.Request.Headers[HttpUtils.HEADER_AUTHORIZATION];

            if (authorizationHeader == null)
            {
                throw new ArgumentNullException("BadRequest", "Not authorized to invoke this service.");
            }

            // Check to make sure the authorization header parses OK
            authenticatedWho = AuthenticationUtils.Deserialize(HttpUtility.UrlDecode(authorizationHeader));

            return authenticatedWho;
        }
    }
}
