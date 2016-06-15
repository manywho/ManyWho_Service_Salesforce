using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Utils;
using ManyWho.Flow.SDK.Security;
using ManyWho.Flow.SDK.Run.Elements.Config;
using ManyWho.Service.Salesforce.Utils;
using ManyWho.Service.Salesforce.Singletons;
using ManyWho.Service.Salesforce.Salesforce;

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
    public class WorkflowRuleListenerMessageHandler : DelegatingHandler
    {
        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            WorkflowRuleNotification receivedNotification = null;
            IAuthenticatedWho authenticatedWho = null;
            INotifier emailNotifier = null;
            Guid tenantGuid = Guid.Empty;
            String tenantId = null;
            String mode = null;
            String email = null;

            try
            {
                // Get the mode from the request uri
                mode = BaseHttpUtils.GetModeFromQuery(request.RequestUri);

                // Get any provided notification email
                email = BaseHttpUtils.GetEmailFromQuery(request.RequestUri);

                // Check to make sure the incoming request has enough segments
                // Deployed we have an extra segment for the deployment sub-directory, hence this is one more than you would have thought
                if (request.RequestUri.Segments.Length < 9)
                {
                    throw new ArgumentNullException("Request.Segments", "The incoming request is not a valid Url for outbound messages.");
                }

                // Get the segments from the call so we know which tenant we're executing against
                tenantId = request.RequestUri.Segments[8].Replace("/", "");

                // Check to make sure we've received valid guids
                if (Guid.TryParse(tenantId, out tenantGuid) == false)
                {
                    throw new ArgumentNullException("Request.Segments", "The incoming request does not contain a valid tenant identifier. You provided: " + tenantId);
                }

                // Create a basic authenticated who for the notifier
                authenticatedWho = AuthenticationUtils.CreatePublicUser(tenantId);
                authenticatedWho.Email = email;

                // Create the notifier
                emailNotifier = EmailNotifier.GetInstance(tenantId, authenticatedWho, null, "WorkflowRuleListenerMessageHandler");

                // ExtractData would populate notification class' variables, which can be used to get desired data.
                receivedNotification = new WorkflowRuleNotification();
                receivedNotification.ExtractData(emailNotifier, request.Content.ReadAsStringAsync().Result, mode);

                // Now send ManyWho the notification that something has changed on a set of records, but only if ManyWho actually cares about them
                this.Execute(emailNotifier, tenantId, mode, receivedNotification);

                // Send the debug log if the user is running in debug mode
                if (SettingUtils.IsDebugging(mode)) { ErrorUtils.SendAlert(emailNotifier, null, ErrorUtils.ALERT_TYPE_WARNING, "Debug Log Entries"); }

                // Send a response back to SFDC
                // Note: since we are not calling base class' SendAsync function, the request will return from here, and will not reach our POST function.
                return Task.FromResult(receivedNotification.PrepareResponse(request));
            }
            catch (Exception exception)
            {
                // Send the debug log if the user is running in debug mode
                if (SettingUtils.IsDebugging(mode)) { ErrorUtils.SendAlert(emailNotifier, null, ErrorUtils.ALERT_TYPE_WARNING, BaseHttpUtils.GetExceptionMessage(exception)); }

                throw BaseHttpUtils.GetWebException(HttpStatusCode.BadRequest, BaseHttpUtils.GetExceptionMessage(exception));
            }
        }

        private void Execute(INotifier notifier, String tenantId, String mode, WorkflowRuleNotification workflowRuleNotification)
        {
            Dictionary<String, ListenerServiceRequestAPI> salesforceListenerEntries = null;
            ListenerServiceResponseAPI listenerServiceResponse = null;
            ListenerServiceRequestAPI listenerServiceRequest = null;
            SforceService sforceService = null;
            String authenticationStrategy = null;
            String authenticationUrl = null;
            String securityToken = null;
            String refreshToken = null;
            String invokeType = null;
            String username = null;
            String password = null;

            if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Executing listener notification."); }

            // Go through each object identifier in the notification
            if (workflowRuleNotification.ObjectIDs != null &&
                workflowRuleNotification.ObjectIDs.Count > 0)
            {
                if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Workflow event has object identifiers."); }

                foreach (String objectId in workflowRuleNotification.ObjectIDs)
                {
                    if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Processing object identifier: " + objectId); }

                    // Check to see if ManyWho has asked us to listen to any of them
                    salesforceListenerEntries = SalesforceListenerSingleton.GetInstance().GetListenerRequests(tenantId, objectId);

                    // Check to see if we're actually listening
                    if (salesforceListenerEntries != null &&
                        salesforceListenerEntries.Count > 0)
                    {
                        if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Object has listener entries."); }

                        // If it has, send a listen notification back to the workflow engine - one by one at the moment (no bulk!)
                        foreach (KeyValuePair<String, ListenerServiceRequestAPI> pair in salesforceListenerEntries)
                        {
                            // Get the listener request out
                            listenerServiceRequest = pair.Value;

                            if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Executing listener entry."); }

                            // Create the service response
                            listenerServiceResponse = new ListenerServiceResponseAPI();
                            listenerServiceResponse.annotations = listenerServiceRequest.annotations;
                            listenerServiceResponse.culture = listenerServiceRequest.culture;
                            listenerServiceResponse.tenantId = listenerServiceRequest.tenantId;
                            listenerServiceResponse.token = listenerServiceRequest.token;

                            // Apply the settings for the response from the request so the engine has the full details about the object
                            listenerServiceResponse.listeningEventValue = listenerServiceRequest.valueForListening;
                            listenerServiceResponse.listeningEventValue.contentType = listenerServiceRequest.valueForListening.contentType;
                            listenerServiceResponse.listeningEventValue.contentValue = listenerServiceRequest.valueForListening.contentValue;
                            listenerServiceResponse.listeningEventValue.developerName = listenerServiceRequest.valueForListening.developerName;
                            listenerServiceResponse.listeningEventValue.typeElementDeveloperName = listenerServiceRequest.valueForListening.typeElementDeveloperName;
                            listenerServiceResponse.listeningEventValue.typeElementId = listenerServiceRequest.valueForListening.typeElementId;
                            listenerServiceResponse.listeningEventValue.typeElementPropertyDeveloperName = listenerServiceRequest.valueForListening.typeElementPropertyDeveloperName;
                            listenerServiceResponse.listeningEventValue.typeElementPropertyId = listenerServiceRequest.valueForListening.typeElementPropertyId;
                            listenerServiceResponse.listeningEventValue.valueElementId = listenerServiceRequest.valueForListening.valueElementId;

                            // Get the configuration values out that are needed to check the voting status
                            // TODO: we should smart cache the login info and connection
                            authenticationUrl = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_AUTHENTICATION_URL, listenerServiceRequest.configurationValues, true);
                            username = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_USERNAME, listenerServiceRequest.configurationValues, false);
                            password = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_PASSWORD, listenerServiceRequest.configurationValues, false);
                            refreshToken = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_REFRESH_TOKEN, listenerServiceRequest.configurationValues, false);
                            securityToken = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_SECURITY_TOKEN, listenerServiceRequest.configurationValues, false);
                            authenticationStrategy = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_AUTHENTICATION_STRATEGY, listenerServiceRequest.configurationValues, false);

                            if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Logging into salesforce using: " + username); }

                            if (String.IsNullOrWhiteSpace(authenticationStrategy) == true ||
                                authenticationStrategy.Equals(SalesforceServiceSingleton.AUTHENTICATION_STRATEGY_STANDARD, StringComparison.OrdinalIgnoreCase) == true ||
                                authenticationStrategy.Equals(SalesforceServiceSingleton.AUTHENTICATION_STRATEGY_ACTIVE_USER, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                sforceService = SalesforceDataSingleton.GetInstance().LogUserInBasedOnSession(workflowRuleNotification.SessionID, workflowRuleNotification.SessionURL);
                            }
                            else if (authenticationStrategy.Equals(SalesforceServiceSingleton.AUTHENTICATION_STRATEGY_SUPER_USER, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                // Login to salesforce using the details in the service request
                                sforceService = SalesforceDataSingleton.GetInstance().LoginUsingCredentials(authenticationUrl, username, password, securityToken);
                            }
                            else
                            {
                                throw new ArgumentNullException("ConfigurationValues", String.Format("The provided authentication strategy is not supported: '{0}'", authenticationStrategy));
                            }

                            if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Getting full sObject for: " + objectId); }

                            // Load the latest object from salesforce so we have the data to send back
                            listenerServiceResponse.listeningEventValue.objectData = SalesforceDataSingleton.GetInstance().LoadSObjectByIdentifier(sforceService, workflowRuleNotification.ObjectName, objectId, true);

                            try
                            {
                                if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Executing event against ManyWho"); }

                                // Dispatch a listen response to the engine as an event has occurred
                                invokeType = RunSingleton.GetInstance().Event(notifier, null, tenantId, listenerServiceRequest.callbackUri, listenerServiceResponse);
                            }
                            catch (Exception exception)
                            {
                                if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Event execution failed with: " + BaseHttpUtils.GetExceptionMessage(exception)); }

                                // Something went wrong - but we ignore it for now
                                invokeType = ManyWhoConstants.INVOKE_TYPE_FORWARD;
                            }

                            if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Service returned invoke type of: " + invokeType); }

                            // If the engine returns nothing, errors or returns an response other than a "WAIT", we delete the listener for now
                            // TODO: make this a bit more intelligent so we can handle things like retry
                            if (String.IsNullOrWhiteSpace(invokeType) == false &&
                                invokeType.IndexOf(ManyWhoConstants.INVOKE_TYPE_WAIT, StringComparison.InvariantCultureIgnoreCase) < 0)
                            {
                                if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Removing entry from listeners for invoke type: " + invokeType); }

                                SalesforceListenerSingleton.GetInstance().UnregisterListener(tenantId, objectId, listenerServiceRequest);
                            }
                        }
                    }
                }
            }
        }
    }
}