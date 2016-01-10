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
            String reportingMode = null;

            try
            {
                // Get the mode from the request uri
                mode = BaseHttpUtils.GetModeFromQuery(request.RequestUri);

                // Get the reporting mode from the request uri
                reportingMode = BaseHttpUtils.GetReportingModeFromQuery(request.RequestUri);

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
                this.Execute(emailNotifier, tenantId, flowId, player, mode, reportingMode, receivedNotification);

                // Send the debug log if the user is running in debug mode
                if (SettingUtils.IsDebugging(mode)) { ErrorUtils.SendAlert(emailNotifier, null, ErrorUtils.ALERT_TYPE_WARNING, "Debug Log Entries"); }

                //Send a response back to SFDC
                //Note: since we are not calling base class' SendAsync function, the request will return from here, and will not reach our POST function.
                return Task.FromResult(receivedNotification.PrepareResponse(request));
            }
            catch (Exception exception)
            {
                // Send the debug log if the user is running in debug mode
                if (SettingUtils.IsDebugging(mode)) { ErrorUtils.SendAlert(emailNotifier, null, ErrorUtils.ALERT_TYPE_WARNING, BaseHttpUtils.GetExceptionMessage(exception)); }

                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        public void Execute(INotifier notifier, String tenantId, String flowId, String player, String mode, String reportingMode, WorkflowRuleNotification workflowRuleNotification)
        {
            FlowResponseAPI flowResponse = null;
            EngineInvokeRequestAPI engineInvokeRequest = null;
            EngineInvokeResponseAPI engineInvokeResponse = null;
            EngineInitializationRequestAPI engineInitializationRequest = null;
            EngineInitializationResponseAPI engineInitializationResponse = null;
            AuthenticationCredentialsAPI authenticationCredentials = null;
            String authenticationToken = null;

            if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Executing notification."); }

            // Check to see if we have object identifiers to process
            if (workflowRuleNotification.ObjectIDs != null &&
                workflowRuleNotification.ObjectIDs.Count > 0)
            {
                if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Notification has object identifiers."); }
                if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry(String.Format("Loading flow for tenant ({0}) and identifier ({1}).", tenantId, flowId)); }

                // Now we have the data from the message, we can execute the workflow
                // Load the flow by the unique identifier
                flowResponse = RunSingleton.GetInstance().LoadFlowById(notifier, authenticationToken, tenantId, flowId);

                // Check to make sure we have a flow response
                if (flowResponse == null)
                {
                    throw new ArgumentNullException("FlowResponse", "The flow is null for the provided tenant and flow identifier.");
                }

                foreach (String objectID in workflowRuleNotification.ObjectIDs)
                {
                    if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Sending initialization request to ManyWho."); }

                    // Create an engine initialization request to kick off the flow
                    engineInitializationRequest = new EngineInitializationRequestAPI();
                    engineInitializationRequest.flowId = new FlowIdAPI();
                    engineInitializationRequest.flowId.id = flowResponse.id.id;
                    engineInitializationRequest.flowId.versionId = flowResponse.id.versionId;
                    engineInitializationRequest.mode = mode;
                    engineInitializationRequest.reportingMode = reportingMode;

                    // Initialize the workflow with the values provided
                    engineInitializationRequest.inputs = new List<EngineValueAPI>();
                    engineInitializationRequest.inputs.Add(new EngineValueAPI() { developerName = "SalesforceNotificationRecordId", contentValue = objectID, contentType = ManyWhoConstants.CONTENT_TYPE_STRING });
                    engineInitializationRequest.inputs.Add(new EngineValueAPI() { developerName = "SalesforceNotificationObjectName", contentValue = workflowRuleNotification.ObjectName, contentType = ManyWhoConstants.CONTENT_TYPE_STRING });

                    if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("SalesforceNotificationRecordId: " + objectID); }
                    if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("SalesforceNotificationObjectName: " + workflowRuleNotification.ObjectName); }

                    // Initialize the engine with the bare basics
                    engineInitializationResponse = RunSingleton.GetInstance().Initialize(notifier, authenticationToken, tenantId, engineInitializationRequest);

                    // Check to see if the workflow is authorized to execute - if not, we need to login using the session
                    if (engineInitializationResponse.statusCode.Equals(ManyWhoConstants.AUTHORIZATION_STATUS_NOT_AUTHORIZED, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Event not authorized, attempting a login using session info."); }
                        if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("SessionId: " + workflowRuleNotification.SessionID); }
                        if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("SessionURL: " + workflowRuleNotification.SessionURL); }

                        // Create the authentication credentials for the service
                        authenticationCredentials = new AuthenticationCredentialsAPI();
                        authenticationCredentials.loginUrl = engineInitializationResponse.authorizationContext.loginUrl;
                        authenticationCredentials.sessionToken = workflowRuleNotification.SessionID;
                        authenticationCredentials.sessionUrl = workflowRuleNotification.SessionURL;

                        if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Logging into service again using session info."); }

                        // Login to the system
                        authenticationToken = RunSingleton.GetInstance().Login(notifier, tenantId, engineInitializationResponse.stateId, authenticationCredentials);

                        // Apply the state back
                        engineInitializationRequest.stateId = engineInitializationResponse.stateId;

                        if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Initializing engine again for state identifier: " + engineInitializationResponse.stateId); }

                        // Initialize the engine again - re-using the state identifier
                        engineInitializationResponse = RunSingleton.GetInstance().Initialize(notifier, authenticationToken, tenantId, engineInitializationRequest);
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

                    if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Sending invoke request to ManyWho."); }

                    // Invoke the engine with the first request
                    engineInvokeResponse = RunSingleton.GetInstance().Execute(notifier, authenticationToken, tenantId, engineInvokeRequest);

                    // If we're running in step through mode, we notify the author with the join identifier so they can debug the workflow
                    if (SettingUtils.IsDebugging(engineInvokeRequest.mode) == true &&
                        engineInvokeRequest.mode.Equals(ManyWhoConstants.MODE_DEBUG_STEPTHROUGH, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        notifier.AddLogEntry("Flow is waiting to be joined in order to be debugged.");
                        notifier.AddLogEntry("JoinUrl: " + engineInvokeResponse.joinFlowUri + "&mode=" + engineInvokeRequest.mode);
                    }
                }
            }
            else
            {
                if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Notification does not have object identifiers."); }
            }
        }
    }
}