using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Utils;
using ManyWho.Flow.SDK.Security;
using ManyWho.Flow.SDK.Draw.Flow;
using ManyWho.Flow.SDK.Run;
using ManyWho.Flow.SDK.Run.Elements.Map;
using ManyWho.Service.Salesforce.Utils;

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

namespace ManyWho.Service.Salesforce
{
    public class WorkflowRuleMessageHandler : DelegatingHandler
    {
        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            WorkflowRuleNotification receivedNotification = null;
            IAuthenticatedWho authenticatedWho = null;
            INotifier emailNotifier = null;
            Guid tenantGuid = Guid.Empty;
            Guid flowGuid = Guid.Empty;
            String tenantId = null;
            String flowId = null;
            String player = null;
            String mode = null;
            String email = null;

            try
            {
                // Get the mode from the request uri
                mode = BaseHttpUtils.GetModeFromQuery(request.RequestUri);

                // Get any provided notification email
                email = BaseHttpUtils.GetEmailFromQuery(request.RequestUri);

                // Check to make sure the incoming request has enough segments
                if (request.RequestUri.Segments.Length < 9)
                {
                    throw new ArgumentNullException("Request.Segments", "The incoming request is not a valid Url for outbound messages.");
                }

                // Get the segments from the call so we know which tenant we're executing against
                tenantId = request.RequestUri.Segments[7].Replace("/", "");
                flowId = request.RequestUri.Segments[8].Replace("/", "");
                player = request.RequestUri.Segments[9];

                // Check to make sure we've received valid guids
                if (Guid.TryParse(tenantId, out tenantGuid) == false)
                {
                    throw new ArgumentNullException("Request.Segments", "The incoming request does not contain a valid tenant identifier.");
                }

                if (Guid.TryParse(flowId, out flowGuid) == false)
                {
                    throw new ArgumentNullException("Request.Segments", "The incoming request does not contain a valid flow identifier.");
                }

                // If a player has not been provided, we make it the default player
                if (String.IsNullOrWhiteSpace(player) == true)
                {
                    player = "default";
                }

                // Create a basic authenticated who for the notifier
                authenticatedWho = AuthenticationUtils.CreatePublicUser(tenantId);
                authenticatedWho.Email = email;

                // Create the notifier
                emailNotifier = EmailNotifier.GetInstance(tenantId, authenticatedWho, null, "WorkflowRuleMessageHandler");

                //ExtractData would populate notification class' variables, which can be used to get desired data.
                receivedNotification = new WorkflowRuleNotification();
                receivedNotification.ExtractData(emailNotifier, request.Content.ReadAsStringAsync().Result, mode);

                // Execute the notifications against ManyWho
                this.Execute(emailNotifier, tenantId, flowId, player, mode, receivedNotification);

                // Send the debug log if the user is running in debug mode
                if (ErrorUtils.IsDebugging(mode)) { ErrorUtils.SendAlert(emailNotifier, null, ErrorUtils.ALERT_TYPE_WARNING, "Debug Log Entries"); }

                //Send a response back to SFDC
                //Note: since we are not calling base class' SendAsync function, the request will return from here, and will not reach our POST function.
                return Task.FromResult(receivedNotification.PrepareResponse(request));
            }
            catch (Exception exception)
            {
                // Send the debug log if the user is running in debug mode
                if (ErrorUtils.IsDebugging(mode)) { ErrorUtils.SendAlert(emailNotifier, null, ErrorUtils.ALERT_TYPE_WARNING, ErrorUtils.GetExceptionMessage(exception)); }

                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, ErrorUtils.GetExceptionMessage(exception));
            }
        }

        public void Execute(INotifier notifier, String tenantId, String flowId, String player, String mode, WorkflowRuleNotification workflowRuleNotification)
        {
            IAuthenticatedWho authenticatedWho = null;
            FlowResponseAPI flowResponse = null;
            EngineInvokeRequestAPI engineInvokeRequest = null;
            EngineInvokeResponseAPI engineInvokeResponse = null;
            EngineInitializationRequestAPI engineInitializationRequest = null;
            EngineInitializationResponseAPI engineInitializationResponse = null;
            AuthenticationCredentialsAPI authenticationCredentials = null;

            if (ErrorUtils.IsDebugging(mode)) { notifier.AddLogEntry("Executing notification."); }

            // Check to see if we have object identifiers to process
            if (workflowRuleNotification.ObjectIDs != null &&
                workflowRuleNotification.ObjectIDs.Count > 0)
            {
                if (ErrorUtils.IsDebugging(mode)) { notifier.AddLogEntry("Notification has object identifiers."); }
                if (ErrorUtils.IsDebugging(mode)) { notifier.AddLogEntry(String.Format("Loading flow for tenant ({0}) and identifier ({1}).", tenantId, flowId)); }

                // Now we have the data from the message, we can execute the workflow
                // Load the flow by the unique identifier
                flowResponse = RunSingleton.GetInstance().LoadFlowById(notifier, authenticatedWho, tenantId, flowId);

                // Check to make sure we have a flow response
                if (flowResponse == null)
                {
                    throw new ArgumentNullException("FlowResponse", "The flow is null for the provided tenant and flow identifier.");
                }

                foreach (String objectID in workflowRuleNotification.ObjectIDs)
                {
                    if (ErrorUtils.IsDebugging(mode)) { notifier.AddLogEntry("Sending initialization request to ManyWho."); }

                    // Create an engine initialization request to kick off the flow
                    engineInitializationRequest = new EngineInitializationRequestAPI();
                    engineInitializationRequest.flowId = new FlowIdAPI();
                    engineInitializationRequest.flowId.id = flowResponse.id.id;
                    engineInitializationRequest.flowId.versionId = flowResponse.id.versionId;
                    engineInitializationRequest.mode = mode;

                    // Initialize the workflow with the values provided
                    engineInitializationRequest.inputs = new List<EngineValueAPI>();
                    engineInitializationRequest.inputs.Add(new EngineValueAPI() { developerName = "SalesforceNotificationRecordId", contentValue = objectID, contentType = ManyWhoConstants.CONTENT_TYPE_STRING });
                    engineInitializationRequest.inputs.Add(new EngineValueAPI() { developerName = "SalesforceNotificationObjectName", contentValue = workflowRuleNotification.ObjectName, contentType = ManyWhoConstants.CONTENT_TYPE_STRING });

                    if (ErrorUtils.IsDebugging(mode)) { notifier.AddLogEntry("SalesforceNotificationRecordId: " + objectID); }
                    if (ErrorUtils.IsDebugging(mode)) { notifier.AddLogEntry("SalesforceNotificationObjectName: " + workflowRuleNotification.ObjectName); }

                    // Initialize the engine with the bare basics
                    engineInitializationResponse = RunSingleton.GetInstance().Initialize(notifier, authenticatedWho, tenantId, engineInitializationRequest);

                    // Check to see if the workflow is authorized to execute - if not, we need to login using the session
                    if (engineInitializationResponse.statusCode.Equals(ManyWhoConstants.AUTHORIZATION_STATUS_NOT_AUTHORIZED, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        if (ErrorUtils.IsDebugging(mode)) { notifier.AddLogEntry("Event not authorized, attempting a login using session info."); }
                        if (ErrorUtils.IsDebugging(mode)) { notifier.AddLogEntry("SessionId: " + workflowRuleNotification.SessionID); }
                        if (ErrorUtils.IsDebugging(mode)) { notifier.AddLogEntry("SessionURL: " + workflowRuleNotification.SessionURL); }

                        // Create the authentication credentials for the service
                        authenticationCredentials = new AuthenticationCredentialsAPI();
                        authenticationCredentials.loginUrl = engineInitializationResponse.authorizationContext.loginUrl;
                        authenticationCredentials.sessionToken = workflowRuleNotification.SessionID;
                        authenticationCredentials.sessionUrl = workflowRuleNotification.SessionURL;

                        if (ErrorUtils.IsDebugging(mode)) { notifier.AddLogEntry("Logging into service again using session info."); }

                        // Login to the system
                        authenticatedWho = this.Login(notifier, tenantId, engineInitializationResponse.stateId, authenticationCredentials);

                        // Apply the state back
                        engineInitializationRequest.stateId = engineInitializationResponse.stateId;

                        if (ErrorUtils.IsDebugging(mode)) { notifier.AddLogEntry("Initializing engine again for state identifier: " + engineInitializationResponse.stateId); }

                        // Initialize the engine again - re-using the state identifier
                        engineInitializationResponse = RunSingleton.GetInstance().Initialize(notifier, authenticatedWho, tenantId, engineInitializationRequest);
                    }

                    // Now create the fist engine invoke request so we can get the content of the first ivr
                    engineInvokeRequest = new EngineInvokeRequestAPI();
                    engineInvokeRequest.currentMapElementId = engineInitializationResponse.currentMapElementId;
                    engineInvokeRequest.invokeType = ManyWhoConstants.INVOKE_TYPE_FORWARD;
                    engineInvokeRequest.mapElementInvokeRequest = new MapElementInvokeRequestAPI();
                    engineInvokeRequest.currentMapElementId = engineInitializationResponse.currentMapElementId;
                    engineInvokeRequest.stateId = engineInitializationResponse.stateId;
                    engineInvokeRequest.stateToken = engineInitializationResponse.stateToken;
                    engineInvokeRequest.mode = mode;

                    if (ErrorUtils.IsDebugging(mode)) { notifier.AddLogEntry("Sending invoke request to ManyWho."); }

                    // Invoke the engine with the first request
                    engineInvokeResponse = RunSingleton.GetInstance().Execute(notifier, authenticatedWho, tenantId, engineInvokeRequest);

                    // If we're running in step through mode, we notify the author with the join identifier so they can debug the workflow
                    if (ErrorUtils.IsDebugging(engineInvokeRequest.mode) == true &&
                        engineInvokeRequest.mode.Equals(ManyWhoConstants.MODE_DEBUG_STEPTHROUGH, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        notifier.AddLogEntry("Flow is waiting to be joined in order to be debugged.");
                        notifier.AddLogEntry("JoinUrl: " + engineInvokeResponse.joinFlowUri + "&mode=" + engineInvokeRequest.mode);
                    }
                }
            }
            else
            {
                if (ErrorUtils.IsDebugging(mode)) { notifier.AddLogEntry("Notification does not have object identifiers."); }
            }
        }

        public IAuthenticatedWho Login(INotifier notifier, String tenantId, String stateId, AuthenticationCredentialsAPI authenticationCredentials)
        {
            WebException webException = null;
            String endpointUrl = null;
            HttpClient httpClient = null;
            HttpContent httpContent = null;
            HttpResponseMessage httpResponseMessage = null;
            IAuthenticatedWho authenticatedWho = null;
            String authenticationToken = null;

            // We enclose the request in a for loop to handle http errors
            for (int i = 0; i < HttpUtils.MAXIMUM_RETRIES; i++)
            {
                try
                {
                    // Create the http client to handle our request
                    httpClient = HttpUtils.CreateHttpClient(authenticatedWho, tenantId, null);

                    // Use the JSON formatter to create the content of the request body
                    httpContent = new StringContent(JsonConvert.SerializeObject(authenticationCredentials));
                    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    // Construct the URL for the engine initialization request
                    endpointUrl = "https://flow.manywho.com/api/run/1/authentication/" + stateId;

                    // Post the engine initialization request over to ManyWho
                    httpResponseMessage = httpClient.PostAsync(endpointUrl, httpContent).Result;

                    // Check the status of the response and respond appropriately
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        // Get the engine initialization response object from the response message
                        authenticationToken = httpResponseMessage.Content.ReadAsStringAsync().Result;
                        authenticationToken = authenticationToken.Substring(1, authenticationToken.Length - 2);

                        // Get the authenticated who from the token
                        authenticatedWho = AuthenticationUtils.Deserialize(Uri.UnescapeDataString(authenticationToken));

                        // We successfully executed the request, we can break out of the retry loop
                        break;
                    }
                    else
                    {
                        // Make sure we handle the lack of success properly
                        webException = BaseHttpUtils.HandleUnsuccessfulHttpResponseMessage(notifier, authenticatedWho, i, httpResponseMessage, endpointUrl);

                        if (webException != null)
                        {
                            throw webException;
                        }
                    }
                }
                catch (Exception exception)
                {
                    // Make sure we handle the exception properly
                    webException = BaseHttpUtils.HandleHttpException(notifier, authenticatedWho, i, exception, endpointUrl);

                    if (webException != null)
                    {
                        throw webException;
                    }
                }
                finally
                {
                    // Clean up the objects from the request
                    HttpUtils.CleanUpHttp(httpClient, httpContent, httpResponseMessage);
                }
            }

            return authenticatedWho;
        }
    }
}