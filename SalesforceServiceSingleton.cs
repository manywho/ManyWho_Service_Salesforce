using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Translate;
using ManyWho.Flow.SDK.Utils;
using ManyWho.Flow.SDK.Social;
using ManyWho.Flow.SDK.Security;
using ManyWho.Flow.SDK.Describe;
using ManyWho.Flow.SDK.Draw.Elements.UI;
using ManyWho.Flow.SDK.Draw.Elements.Type;
using ManyWho.Flow.SDK.Draw.Elements.Config;
using ManyWho.Flow.SDK.Draw.Elements.Group;
using ManyWho.Flow.SDK.Run.Elements.UI;
using ManyWho.Flow.SDK.Run.Elements.Map;
using ManyWho.Flow.SDK.Run.Elements.Type;
using ManyWho.Flow.SDK.Run.Elements.Config;
using ManyWho.Flow.SDK.Run;
using ManyWho.Flow.SDK.Run.State;
using ManyWho.Service.Salesforce.Models.Rest.Enums;
using ManyWho.Service.Salesforce.Models.Rest;
using ManyWho.Service.Salesforce.Utils;
using ManyWho.Service.Salesforce.Singletons;
using ManyWho.Flow.Web.Controllers;
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

namespace ManyWho.Service.Salesforce
{
    public class SalesforceServiceSingleton
    {
        public const String SERVICE_ACTION_TASK = "task";
        public const String SERVICE_ACTION_NOTIFY = "notify";
        public const String SERVICE_ACTION_CREATE_TASK = "createtask";
        public const String SERVICE_ACTION_CREATE_EVENT = "createevent";
        public const String SERVICE_ACTION_SEND_TASK_EMAIL = "createtaskemail";

        public const String SERVICE_VALUE_SECURITY_TOKEN = "SecurityToken";
        public const String SERVICE_VALUE_USERNAME = "Username";
        public const String SERVICE_VALUE_PASSWORD = "Password";
        public const String SERVICE_VALUE_AUTHENTICATION_URL = "AuthenticationUrl";
        public const String SERVICE_VALUE_CHATTER_BASE_URL = "ChatterBaseUrl";
        public const String SERVICE_VALUE_ADMIN_EMAIL = "AdminEmail";
        public const String SERVICE_VALUE_CONSUMER_SECRET = "Consumer Secret";
        public const String SERVICE_VALUE_CONSUMER_KEY = "Consumer Key";
        public const String SERVICE_VALUE_DATA_ACCESS_USING_STORED_CREDENTIALS = "Data Access Using Stored Credentials";

        public const String SERVICE_VALUE_COLLEAGUES = "COLLEAGUES";
        public const String SERVICE_VALUE_DELEGATES = "DELEGATES";
        public const String SERVICE_VALUE_DIRECTS = "DIRECTS";
        public const String SERVICE_VALUE_FOLLOWERS = "FOLLOWERS";
        public const String SERVICE_VALUE_FOLLOWING = "FOLLOWING";
        public const String SERVICE_VALUE_MANAGERS = "MANAGERS";
        public const String SERVICE_VALUE_USER = "USER";

        public const String SERVICE_INPUT_POST = "Post";
        public const String SERVICE_INPUT_WHEN = "When";
        public const String SERVICE_INPUT_DESCRIPTION = "Description";
        public const String SERVICE_INPUT_PRIORITY = "Priority";
        public const String SERVICE_INPUT_STATUS = "Status";
        public const String SERVICE_INPUT_SUBJECT = "Subject";
        public const String SERVICE_INPUT_DURATION = "Duration";

        public const String SERVICE_OUTPUT_ID = "Id";

        public const String SERVICE_VALUE_MEMBERS = "MEMBERS";
        public const String SERVICE_VALUE_OWNERS = "OWNERS";

        public const String CHATTER_URI_PART_API_VERSION = "/services/data/v27.0/chatter";
        public const String CHATTER_URI_PART_USERS = "/users";
        public const String CHATTER_URI_PART_FOLLOWING_ME = "/users/me/following";
        public const String CHATTER_URI_PART_STREAM_FOLLOWERS = "/records/{0}/followers";
        public const String CHATTER_URI_PART_POSTS = "/feeds/record/{0}/feed-items";
        public const String CHATTER_URI_PART_COMMENTS = "/feed-items/{0}/comments";
        public const String CHATTER_URI_PART_MY_FEED = "/feeds/news/{0}/feed-items";
        public const String CHATTER_URI_PART_FEED_ITEMS = "/feed-items/";
        public const String CHATTER_URI_PART_LIKE = "/feed-items/{0}/likes";
        public const String CHATTER_URI_PART_DELETE_LIKE = "/likes/";
        public const String CHATTER_URI_PART_FOLLOWING_TO_ME = "/users/me/following";
        public const String CHATTER_URI_PART_SUBSCRIPTIONS = "/subscriptions/";
        public const String CHATTER_URI_PART_AUTOCOMPLETE = "/users?q={0}*";

        public const String CHATTER_DEFAULT_FILE_IMAGE_URL = "/extensions/glyphicons/attachment.png";

        // This is the format for the at mentioned user that's needed for the generic runtime
        public const String CHATTER_MENTIONED_USER_NAME_SPAN = "<span class=\"manywho-who-reference\" id=\"{0}\" data-whoId=\"{1}\">{2}</span>";

        public const String CHATTER_HASH_TAG_OR_MENTION_START = @"(?<=^|\s)";
        public const String CHATTER_HASH_TAG_OR_MENTION_FINISH = @"(?=\s|$)";

        public const String CHATTER_ME = "me";

        private static SalesforceServiceSingleton salesforceServiceSingleton;

        private SalesforceServiceSingleton()
        {

        }

        public static SalesforceServiceSingleton GetInstance()
        {
            if (salesforceServiceSingleton == null)
            {
                salesforceServiceSingleton = new SalesforceServiceSingleton();
            }

            return salesforceServiceSingleton;
        }

        /// <summary>
        /// This method performs the actual describe for the service setup. The code here provides the configuration information needed to use the salesforce.com plugin.
        /// </summary>
        public DescribeServiceResponseAPI Describe(DescribeServiceRequestAPI describeServiceRequest)
        {
            DescribeServiceInstallResponseAPI describeServiceInstallResponse = null;
            DescribeServiceActionResponseAPI describeServiceAction = null;
            DescribeServiceResponseAPI describeServiceResponse = null;
            Boolean dataAccessUsingStoredCredentials = false;
            String chatterBaseUrl = null;
            String authenticationUrl = null;
            String username = null;
            String password = null;
            String securityToken = null;
            String adminEmail = null;
            String consumerSecret = null;
            String consumerKey = null;
            String defaultEmail = null;
            String emailUsername = null;
            String emailPassword = null;
            String emailSmtp = null;

            if (describeServiceRequest == null)
            {
                throw new ArgumentNullException("DescribeServiceRequest", "DescribeServiceRequest object cannot be null.");
            }

            // We do not require configuration values in the describe call as this is a refresh type operation
            if (describeServiceRequest.configurationValues != null &&
                describeServiceRequest.configurationValues.Count > 0)
            {
                // If the configuration values are provided, then all of them are required
                authenticationUrl = ValueUtils.GetContentValue(SERVICE_VALUE_AUTHENTICATION_URL, describeServiceRequest.configurationValues, true);
                username = ValueUtils.GetContentValue(SERVICE_VALUE_USERNAME, describeServiceRequest.configurationValues, true);
                password = ValueUtils.GetContentValue(SERVICE_VALUE_PASSWORD, describeServiceRequest.configurationValues, true);
                chatterBaseUrl = ValueUtils.GetContentValue(SERVICE_VALUE_CHATTER_BASE_URL, describeServiceRequest.configurationValues, true);
                adminEmail = ValueUtils.GetContentValue(SERVICE_VALUE_ADMIN_EMAIL, describeServiceRequest.configurationValues, true);

                // Get the optional values
                securityToken = ValueUtils.GetContentValue(SERVICE_VALUE_SECURITY_TOKEN, describeServiceRequest.configurationValues, false);
                consumerSecret = ValueUtils.GetContentValue(SERVICE_VALUE_CONSUMER_SECRET, describeServiceRequest.configurationValues, false);
                consumerKey = ValueUtils.GetContentValue(SERVICE_VALUE_CONSUMER_KEY, describeServiceRequest.configurationValues, false);
                //dataAccessUsingStoredCredentials = Boolean.Parse(ValueUtils.GetContentValue(SERVICE_VALUE_DATA_ACCESS_USING_STORED_CREDENTIALS, describeServiceRequest.configurationValues, false));

                // Get the optional email properties
                defaultEmail = ValueUtils.GetContentValue(ManyWhoUtilsSingleton.APP_SETTING_DEFAULT_FROM_EMAIL, describeServiceRequest.configurationValues, false);
                emailUsername = ValueUtils.GetContentValue(ManyWhoUtilsSingleton.APP_SETTING_EMAIL_ACCOUNT_USERNAME, describeServiceRequest.configurationValues, false);
                emailPassword = ValueUtils.GetContentValue(ManyWhoUtilsSingleton.APP_SETTING_EMAIL_ACCOUNT_PASSWORD, describeServiceRequest.configurationValues, false);
                emailSmtp = ValueUtils.GetContentValue(ManyWhoUtilsSingleton.APP_SETTING_EMAIL_ACCOUNT_SMTP, describeServiceRequest.configurationValues, false);
            }

            // Start building the describe service response so the caller knows what they need to provide to use this service
            describeServiceResponse = new DescribeServiceResponseAPI();
            describeServiceResponse.culture = new CultureAPI();
            describeServiceResponse.culture.country = "US";
            describeServiceResponse.culture.language = "EN";
            describeServiceResponse.culture.variant = null;
            describeServiceResponse.providesDatabase = true;
            describeServiceResponse.providesLogic = true;
            describeServiceResponse.providesViews = false;
            describeServiceResponse.providesIdentity = true;
            describeServiceResponse.providesSocial = true;

            // Create the main configuration values
            describeServiceResponse.configurationValues = new List<DescribeValueAPI>();
            describeServiceResponse.configurationValues.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_VALUE_AUTHENTICATION_URL, authenticationUrl, true));
            describeServiceResponse.configurationValues.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_VALUE_USERNAME, username, true));
            describeServiceResponse.configurationValues.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_PASSWORD, SERVICE_VALUE_PASSWORD, password, true));
            describeServiceResponse.configurationValues.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_VALUE_CHATTER_BASE_URL, chatterBaseUrl, true));
            describeServiceResponse.configurationValues.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_VALUE_ADMIN_EMAIL, adminEmail, true));

            // The optional configuration values
            describeServiceResponse.configurationValues.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_VALUE_SECURITY_TOKEN, securityToken, false));
            describeServiceResponse.configurationValues.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_PASSWORD, SERVICE_VALUE_CONSUMER_SECRET, consumerSecret, false));
            describeServiceResponse.configurationValues.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_VALUE_CONSUMER_KEY, consumerKey, false));
            //describeServiceResponse.configurationValues.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_BOOLEAN, SERVICE_VALUE_DATA_ACCESS_USING_STORED_CREDENTIALS, dataAccessUsingStoredCredentials.ToString(), false));

            // The configuration values for the email stuff
            describeServiceResponse.configurationValues.Add(new DescribeValueAPI() { contentType = ManyWhoConstants.CONTENT_TYPE_STRING, developerName = ManyWhoUtilsSingleton.APP_SETTING_DEFAULT_FROM_EMAIL, contentValue = defaultEmail, isRequired = false });
            describeServiceResponse.configurationValues.Add(new DescribeValueAPI() { contentType = ManyWhoConstants.CONTENT_TYPE_STRING, developerName = ManyWhoUtilsSingleton.APP_SETTING_EMAIL_ACCOUNT_USERNAME, contentValue = emailUsername, isRequired = false });
            describeServiceResponse.configurationValues.Add(new DescribeValueAPI() { contentType = ManyWhoConstants.CONTENT_TYPE_PASSWORD, developerName = ManyWhoUtilsSingleton.APP_SETTING_EMAIL_ACCOUNT_PASSWORD, contentValue = emailPassword, isRequired = false });
            describeServiceResponse.configurationValues.Add(new DescribeValueAPI() { contentType = ManyWhoConstants.CONTENT_TYPE_STRING, developerName = ManyWhoUtilsSingleton.APP_SETTING_EMAIL_ACCOUNT_SMTP, contentValue = emailSmtp, isRequired = false });

            // If the user has provided these values as part of a re-submission, we can then go about configuring the rest of the service
            if (authenticationUrl != null &&
                authenticationUrl.Trim().Length > 0 && 
                username != null &&
                username.Trim().Length > 0 &&
                password != null &&
                password.Trim().Length > 0)
            {
                describeServiceResponse.actions = new List<DescribeServiceActionResponseAPI>();

                // We have one message available under this service for creating tasks
                describeServiceAction = new DescribeServiceActionResponseAPI();
                describeServiceAction.uriPart = SERVICE_ACTION_TASK;
                describeServiceAction.developerName = "Create Task";
                describeServiceAction.developerSummary = "This action creates a new task in salesforce.com";
                describeServiceAction.isViewMessageAction = false;

                // Create the inputs for the task creation
                describeServiceAction.serviceInputs = new List<DescribeValueAPI>();
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_DATETIME, "Activity Date", null, true));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, "Description", null, true));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, "Priority", null, true));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, "Status", null, true));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, "Subject", null, true));

                // Create the outputs for the task creation
                describeServiceAction.serviceOutputs = new List<DescribeValueAPI>();
                describeServiceAction.serviceOutputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_BOOLEAN, "IsClosed", null, false));
                describeServiceAction.serviceOutputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, "OwnerId", null, false));
                describeServiceAction.serviceOutputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, "WhatId", null, false));
                describeServiceAction.serviceOutputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, "WhoId", null, false));
                describeServiceAction.serviceOutputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, "WhoId", null, false));
                describeServiceAction.serviceOutputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_DATETIME, "Activity Date", null, false));
                describeServiceAction.serviceOutputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, "Description", null, false));
                describeServiceAction.serviceOutputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, "Priority", null, false));
                describeServiceAction.serviceOutputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, "Status", null, false));
                describeServiceAction.serviceOutputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, "Subject", null, false));

                // Add the task action to the response
                describeServiceResponse.actions.Add(describeServiceAction);

                // We have another message available under this service for notifying users with a message
                describeServiceAction = new DescribeServiceActionResponseAPI();
                describeServiceAction.uriPart = SERVICE_ACTION_NOTIFY;
                describeServiceAction.developerName = "Notify Users";
                describeServiceAction.developerSummary = "This action notifies the users in the authorization context of the flow or group.";
                describeServiceAction.isViewMessageAction = false;

                // Create the inputs for the task creation
                describeServiceAction.serviceInputs = new List<DescribeValueAPI>();
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_INPUT_POST, null, true));

                // Add the task action to the response
                describeServiceResponse.actions.Add(describeServiceAction);

                // We have another message available under this service for creating simple tasks with no async
                describeServiceAction = new DescribeServiceActionResponseAPI();
                describeServiceAction.uriPart = SERVICE_ACTION_SEND_TASK_EMAIL;
                describeServiceAction.developerName = "Send Task Email";
                describeServiceAction.developerSummary = "This action sends an email to all users in the authorization context with buttons for each outcome.";
                describeServiceAction.isViewMessageAction = false;

                // Create the inputs for the task creation
                describeServiceAction.serviceInputs = new List<DescribeValueAPI>();
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, ManyWhoUtilsSingleton.SERVICE_VALUE_TO_EMAIL, null, false));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, ManyWhoUtilsSingleton.SERVICE_VALUE_SUBJECT, null, true));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, ManyWhoUtilsSingleton.SERVICE_VALUE_FROM_EMAIL, null, false));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_CONTENT, ManyWhoUtilsSingleton.SERVICE_VALUE_HTML_BODY, null, false));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, ManyWhoUtilsSingleton.SERVICE_VALUE_TEXT_BODY, null, false));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, ManyWhoUtilsSingleton.SERVICE_VALUE_REDIRECT_URI, null, false));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_BOOLEAN, ManyWhoUtilsSingleton.SERVICE_VALUE_INCLUDE_OUTCOMES_AS_BUTTONS, null, false));

                // Add the email task action to the response
                describeServiceResponse.actions.Add(describeServiceAction);

                // We have another message available under this service for creating simple tasks with no async
                describeServiceAction = new DescribeServiceActionResponseAPI();
                describeServiceAction.uriPart = SERVICE_ACTION_CREATE_TASK;
                describeServiceAction.developerName = "Create A Task";
                describeServiceAction.developerSummary = "This action creates a task in salesforce that is not tied to async.";
                describeServiceAction.isViewMessageAction = false;

                // Create the inputs for the task creation
                describeServiceAction.serviceInputs = new List<DescribeValueAPI>();
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_INPUT_WHEN, null, true));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_INPUT_DESCRIPTION, null, true));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_INPUT_PRIORITY, null, true));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_INPUT_STATUS, null, true));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_INPUT_SUBJECT, null, true));

                // Create the outputs for the task creation
                describeServiceAction.serviceOutputs = new List<DescribeValueAPI>();
                describeServiceAction.serviceOutputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_OUTPUT_ID, null, false));

                // Add the task action to the response
                describeServiceResponse.actions.Add(describeServiceAction);

                // We have another message available under this service for creating simple calendar events
                describeServiceAction = new DescribeServiceActionResponseAPI();
                describeServiceAction.uriPart = SERVICE_ACTION_CREATE_EVENT;
                describeServiceAction.developerName = "Create A Calendar Event";
                describeServiceAction.developerSummary = "This action creates a calendar event in salesforce that is not tied to async.";
                describeServiceAction.isViewMessageAction = false;

                // Create the inputs for the calendar creation
                describeServiceAction.serviceInputs = new List<DescribeValueAPI>();
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_INPUT_WHEN, null, true));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_NUMBER, SERVICE_INPUT_DURATION, null, true));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_INPUT_DESCRIPTION, null, true));
                describeServiceAction.serviceInputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_INPUT_SUBJECT, null, true));

                // Create the outputs for the calendar creation
                describeServiceAction.serviceOutputs = new List<DescribeValueAPI>();
                describeServiceAction.serviceOutputs.Add(DescribeUtils.CreateDescribeValue(ManyWhoConstants.CONTENT_TYPE_STRING, SERVICE_OUTPUT_ID, null, false));

                // Add the calendar action to the response
                describeServiceResponse.actions.Add(describeServiceAction);
                
                // We now create the associated things for this service that we'd like to install into the manywho account
                describeServiceInstallResponse = new DescribeServiceInstallResponseAPI();
                describeServiceInstallResponse.typeElements = new List<TypeElementRequestAPI>();
                describeServiceInstallResponse.typeElements = SalesforceDataSingleton.GetInstance().GetTypeElements(authenticationUrl, username, password, securityToken);

                // Assign the installation object to our main describe response
                describeServiceResponse.install = describeServiceInstallResponse;
            }

            return describeServiceResponse;
        }

        /// <summary>
        /// This method returns the list of tables available for the org being queried.
        /// </summary>
        public List<TypeElementBindingAPI> DescribeTables(ObjectDataRequestAPI objectDataRequestAPI)
        {
            List<TypeElementBindingAPI> typeElementBindings = null;
            String authenticationUrl = null;
            String username = null;
            String password = null;
            String securityToken = null;

            if (objectDataRequestAPI == null)
            {
                throw new ArgumentNullException("ObjectDataRequest", "ObjectDataRequest object cannot be null.");
            }

            if (objectDataRequestAPI.configurationValues == null ||
                objectDataRequestAPI.configurationValues.Count == 0)
            {
                throw new ArgumentNullException("ObjectDataRequest", "ObjectDataRequest.ConfigurationValues cannot be null or empty.");
            }

            // Pull out the configuration values that are needed to query for tables
            authenticationUrl = ValueUtils.GetContentValue(SERVICE_VALUE_AUTHENTICATION_URL, objectDataRequestAPI.configurationValues, true);
            username = ValueUtils.GetContentValue(SERVICE_VALUE_USERNAME, objectDataRequestAPI.configurationValues, true);
            password = ValueUtils.GetContentValue(SERVICE_VALUE_PASSWORD, objectDataRequestAPI.configurationValues, true);
            securityToken = ValueUtils.GetContentValue(SERVICE_VALUE_SECURITY_TOKEN, objectDataRequestAPI.configurationValues, false);

            // Grab the table description
            typeElementBindings = SalesforceDataSingleton.GetInstance().DescribeTables(authenticationUrl, username, password, securityToken);

            return typeElementBindings;
        }

        /// <summary>
        /// This method returns the list of fields for the specified object and org being queried.
        /// </summary>
        public List<TypeElementPropertyBindingAPI> DescribeFields(ObjectDataRequestAPI objectDataRequestAPI)
        {
            List<TypeElementPropertyBindingAPI> typeElementFieldBindings = null;
            String authenticationUrl = null;
            String username = null;
            String password = null;
            String securityToken = null;
            String tableName = null;

            if (objectDataRequestAPI == null)
            {
                throw new ArgumentNullException("ObjectDataRequest", "ObjectDataRequest object cannot be null.");
            }

            if (objectDataRequestAPI.configurationValues == null ||
                objectDataRequestAPI.configurationValues.Count == 0)
            {
                throw new ArgumentNullException("ObjectDataRequest.ConfigurationValues", "ObjectDataRequest.ConfigurationValues cannot be null or empty.");
            }

            if (objectDataRequestAPI.listFilter == null ||
                objectDataRequestAPI.listFilter.where == null ||
                objectDataRequestAPI.listFilter.where.Count == 0)
            {
                throw new ArgumentNullException("ObjectDataReqeust.ListFilter.Where", "ObjectDataRequest.ListFilter.Where cannot be null or empty.");
            }

            // Pull out the configuration values that are needed to query for fields
            authenticationUrl = ValueUtils.GetContentValue(SERVICE_VALUE_AUTHENTICATION_URL, objectDataRequestAPI.configurationValues, true);
            username = ValueUtils.GetContentValue(SERVICE_VALUE_USERNAME, objectDataRequestAPI.configurationValues, true);
            password = ValueUtils.GetContentValue(SERVICE_VALUE_PASSWORD, objectDataRequestAPI.configurationValues, true);
            securityToken = ValueUtils.GetContentValue(SERVICE_VALUE_SECURITY_TOKEN, objectDataRequestAPI.configurationValues, false);

            // We send the name of the table through as part of the where query
            foreach (ListFilterWhereAPI listFilterWhere in objectDataRequestAPI.listFilter.where)
            {
                // Check to see if this is the where for the table name - there should only be one
                if (ManyWhoConstants.SERVICE_DESCRIPTION_VALUE_TABLE_NAME.Equals(listFilterWhere.columnName, StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    // Grab the name of the table and exit from the loop
                    tableName = listFilterWhere.value;
                    break;
                }
            }

            // Throw an error if the table name is null or blank
            if (tableName == null ||
                tableName.Trim().Length == 0)
            {
                throw new ArgumentNullException("ObjectDataRequest.ListFilter.Where", "ObjectDataRequest.ListFilter.Where must contain a filter for: " + ManyWhoConstants.SERVICE_DESCRIPTION_VALUE_TABLE_NAME);
            }

            // Grab the fields for the provided table 
            typeElementFieldBindings = SalesforceDataSingleton.GetInstance().DescribeFields(authenticationUrl, username, password, securityToken, tableName);

            return typeElementFieldBindings;
        }

        /// <summary>
        /// This method is used to invoke particular messages on the service.
        /// </summary>
        public ServiceResponseAPI Invoke(INotifier notifier, IAuthenticatedWho authenticatedWho, String action, ServiceRequestAPI serviceRequest)
        {
            ServiceResponseAPI serviceResponse = null;

            if (action == null ||
                action.Trim().Length == 0)
            {
                throw new ArgumentNullException("Action", "Action cannot be null or blank.");
            }

            if (serviceRequest == null)
            {
                throw new ArgumentNullException("ServiceRequest", "ServiceRequest cannot be null.");
            }

            if (action.Equals(SERVICE_ACTION_TASK, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                // Invoke the task action
                //serviceResponse = SalesforceInvokeSingleton.GetInstance().InvokeCreateTask(serviceRequest);
            }
            else if (action.Equals(SERVICE_ACTION_NOTIFY, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                // Perform the notification for the authorization context
                serviceResponse = SalesforceInvokeSingleton.GetInstance().InvokeNotifyUsers(notifier, authenticatedWho, serviceRequest);
            }
            else if (action.Equals(SERVICE_ACTION_CREATE_TASK, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                serviceResponse = SalesforceInvokeSingleton.GetInstance().InvokeCreateTask(notifier, authenticatedWho, serviceRequest);
            }
            else if (action.Equals(SERVICE_ACTION_CREATE_EVENT, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                serviceResponse = SalesforceInvokeSingleton.GetInstance().InvokeCreateEvent(notifier, authenticatedWho, serviceRequest);
            }
            else if (action.Equals(SERVICE_ACTION_SEND_TASK_EMAIL, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                // Tell the engine to wait until we have a response
                serviceResponse = new ServiceResponseAPI();
                serviceResponse.culture = null;
                serviceResponse.outputs = null;
                serviceResponse.token = serviceRequest.token;

                // Send the email with our task identifier added
                if (ManyWhoUtilsSingleton.GetInstance().SendEmail(notifier, serviceRequest, false) == true)
                {
                    // Store this email task request so we have it for any replies from the user - getting back the unique identifier for the task
                    ManyWhoUtilsSingleton.GetInstance().StoreTaskRequest(authenticatedWho, serviceRequest);

                    // We need to wait as this is a task email
                    serviceResponse.invokeType = ManyWhoConstants.INVOKE_TYPE_WAIT;
                }
                else
                {
                    // Don't wait, we are not expecting the user to respond
                    serviceResponse.invokeType = ManyWhoConstants.INVOKE_TYPE_FORWARD;
                }
            }
            else
            {
                // We don't have an action by that name
                throw new ArgumentNullException("Action", "Action cannot be found for name: " + action);
            }

            return serviceResponse;
        }

        /// <summary>
        /// This method is used to check the voting with Salesforce.
        /// </summary>
        public VoteResponseAPI Vote(INotifier notifier, IAuthenticatedWho authenticatedWho, VoteRequestAPI voteRequestAPI)
        {
            VoteResponseAPI voteResponseAPI = null;
            String authenticationUrl = null;
            String username = null;
            String password = null;
            String securityToken = null;
            String adminEmail = null;
            Int32 outcomeCount = 0;
            Int32 percentage = 0;
            Int32 userCount = 0;

            if (voteRequestAPI == null)
            {
                throw new ArgumentNullException("VoteRequest", "VoteRequest object cannot be null.");
            }

            if (String.IsNullOrWhiteSpace(voteRequestAPI.voteType) == true)
            {
                throw new ArgumentNullException("VoteRequest.VoteType", "VoteRequest.VoteType cannot be null or blank.");
            }

            if (voteRequestAPI.configurationValues == null ||
                voteRequestAPI.configurationValues.Count == 0)
            {
                throw new ArgumentNullException("VoteRequest.ConfigurationValues", "VoteRequest.ConfigurationValues cannot be null or empty.");
            }

            // Get the configuration values out that are needed to check the voting status
            authenticationUrl = ValueUtils.GetContentValue(SERVICE_VALUE_AUTHENTICATION_URL, voteRequestAPI.configurationValues, true);
            username = ValueUtils.GetContentValue(SERVICE_VALUE_USERNAME, voteRequestAPI.configurationValues, true);
            password = ValueUtils.GetContentValue(SERVICE_VALUE_PASSWORD, voteRequestAPI.configurationValues, true);
            securityToken = ValueUtils.GetContentValue(SERVICE_VALUE_SECURITY_TOKEN, voteRequestAPI.configurationValues, false);
            adminEmail = ValueUtils.GetContentValue(SERVICE_VALUE_ADMIN_EMAIL, voteRequestAPI.configurationValues, true);

            // Get the votes for the provided outcome
            outcomeCount = this.CountVotesForSelectedOutcome(voteRequestAPI);

            // Create the vote response object
            voteResponseAPI = new VoteResponseAPI();

            if (voteRequestAPI.voteType.Equals(ManyWhoConstants.VOTE_TYPE_COUNT, StringComparison.OrdinalIgnoreCase) == true)
            {
                // We're dealing with a count based vote, which is pretty straight-forward
                if (voteRequestAPI.minimumCount <= 0)
                {
                    voteResponseAPI.isComplete = true;
                }
                else
                {
                    // If the number of outcomes clicked for this outcome is greater than the minimum count, then this is our winning vote
                    if (outcomeCount >= voteRequestAPI.minimumCount)
                    {
                        voteResponseAPI.isComplete = true;
                    }
                }
            }
            else
            {
                if (voteRequestAPI.minimumPercent <= 0)
                {
                    voteResponseAPI.isComplete = true;
                }
                else
                {
                    // Get the count of users for this authorization context
                    userCount = SalesforceAuthenticationSingleton.GetInstance().GetAuthorizationContextCount(notifier, authenticatedWho, authenticationUrl, username, password, securityToken, voteRequestAPI.authorization);

                    // Round the percentage up to the nearest whole digit
                    percentage = (Int32)Math.Ceiling((double)(((double)outcomeCount / (double)userCount) * 100));

                    // If the percentage is bigger than the minimum, then we're good to go
                    if (percentage >= voteRequestAPI.minimumPercent)
                    {
                        voteResponseAPI.isComplete = true;
                    }
                }
            }

            return voteResponseAPI;
        }

        private Int32 CountVotesForSelectedOutcome(VoteRequestAPI voteRequestAPI)
        {
            Int32 outcomeCount = 0;

            // Check to see if we have the right number of voters to proceed on in the workflow
            if (voteRequestAPI.userVotes != null &&
                voteRequestAPI.userVotes.Count > 0)
            {
                // Count the votes for the incoming selected outcome
                foreach (UserVoteAPI userVote in voteRequestAPI.userVotes)
                {
                    if (userVote.selectedOutcomeId.Equals(voteRequestAPI.selectedOutcomeId, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        outcomeCount++;
                    }
                }
            }

            return outcomeCount;
        }

        /// <summary>
        /// This method is used to check the voting with Salesforce.
        /// </summary>
        public void Listen(INotifier notifier, IAuthenticatedWho authenticatedWho, ListenerServiceRequestAPI listenerServiceRequestAPI)
        {
            EngineValueAPI listeningValue = null;
            String authenticationUrl = null;
            String username = null;
            String password = null;
            String securityToken = null;
            String adminEmail = null;

            if (listenerServiceRequestAPI == null)
            {
                throw new ArgumentNullException("ListenerServiceRequest", "ListenerServiceRequest object cannot be null.");
            }

            if (listenerServiceRequestAPI.configurationValues == null ||
                listenerServiceRequestAPI.configurationValues.Count == 0)
            {
                throw new ArgumentNullException("ListenerServiceRequest.ConfigurationValues", "ListenerServiceRequest.ConfigurationValues cannot be null or empty.");
            }

            // Get the configuration values out that are needed to check the voting status
            authenticationUrl = ValueUtils.GetContentValue(SERVICE_VALUE_AUTHENTICATION_URL, listenerServiceRequestAPI.configurationValues, true);
            username = ValueUtils.GetContentValue(SERVICE_VALUE_USERNAME, listenerServiceRequestAPI.configurationValues, true);
            password = ValueUtils.GetContentValue(SERVICE_VALUE_PASSWORD, listenerServiceRequestAPI.configurationValues, true);
            securityToken = ValueUtils.GetContentValue(SERVICE_VALUE_SECURITY_TOKEN, listenerServiceRequestAPI.configurationValues, false);
            adminEmail = ValueUtils.GetContentValue(SERVICE_VALUE_ADMIN_EMAIL, listenerServiceRequestAPI.configurationValues, true);

            // TODO: Check the user is allowed to register a listener

            // Get the value we want to listen to
            listeningValue = listenerServiceRequestAPI.valueForListening;

            if (String.IsNullOrWhiteSpace(listenerServiceRequestAPI.listenType) == true ||
                listenerServiceRequestAPI.listenType.Equals(ManyWhoConstants.LISTENER_TYPE_EDIT, StringComparison.OrdinalIgnoreCase) == true)
            {
                // If we have a null listener type or edit listener type, register the listener here so we know we're meant to be responding to records
                SalesforceListenerSingleton.GetInstance().RegisterListener(authenticatedWho, listenerServiceRequestAPI);
            }
            else
            {
                // We've not sorted out the voting just yet
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// This method is used to save data back to salesforce.com.
        /// </summary>
        public ObjectDataResponseAPI Save(INotifier notifier, IAuthenticatedWho authenticatedWho, ObjectDataRequestAPI objectDataRequestAPI)
        {
            ObjectDataResponseAPI objectDataResponseAPI = null;
            String authenticationUrl = null;
            String username = null;
            String password = null;
            String securityToken = null;
            String adminEmail = null;

            if (objectDataRequestAPI == null)
            {
                throw new ArgumentNullException("ObjectDataRequest", "ObjectDataRequest object cannot be null.");
            }

            if (objectDataRequestAPI.configurationValues == null ||
                objectDataRequestAPI.configurationValues.Count == 0)
            {
                throw new ArgumentNullException("ObjectDataRequest.ConfigurationValues", "ObjectDataRequest.ConfigurationValues cannot be null or empty.");
            }

            // Get the configuration values out that are needed to save data to salesforce.com
            authenticationUrl = ValueUtils.GetContentValue(SERVICE_VALUE_AUTHENTICATION_URL, objectDataRequestAPI.configurationValues, true);
            username = ValueUtils.GetContentValue(SERVICE_VALUE_USERNAME, objectDataRequestAPI.configurationValues, true);
            password = ValueUtils.GetContentValue(SERVICE_VALUE_PASSWORD, objectDataRequestAPI.configurationValues, true);
            securityToken = ValueUtils.GetContentValue(SERVICE_VALUE_SECURITY_TOKEN, objectDataRequestAPI.configurationValues, false);
            adminEmail = ValueUtils.GetContentValue(SERVICE_VALUE_ADMIN_EMAIL, objectDataRequestAPI.configurationValues, true);

            // We only perform the save if there's actually something to save!
            if (objectDataRequestAPI.objectData != null &&
                objectDataRequestAPI.objectData.Count > 0)
            {
                // Save the data back to salesforce.com
                objectDataRequestAPI.objectData = SalesforceDataSingleton.GetInstance().Save(notifier, authenticatedWho, authenticationUrl, username, password, securityToken, objectDataRequestAPI.objectData);
            }

            // Create the object data response object
            objectDataResponseAPI = new ObjectDataResponseAPI();
            // TODO: Should really get the culture that the authenticated user is running under
            objectDataResponseAPI.culture = objectDataRequestAPI.culture;
            // We can do this as we've applied the changes to the request objects
            objectDataResponseAPI.objectData = objectDataRequestAPI.objectData;

            return objectDataResponseAPI;
        }

        /// <summary>
        /// This method is used to load data from salesforce.com.
        /// </summary>
        public ObjectDataResponseAPI Load(IAuthenticatedWho authenticatedWho, ObjectDataRequestAPI objectDataRequestAPI)
        {
            ObjectDataResponseAPI objectDataResponseAPI = null;
            ObjectDataTypeAPI objectDataType = null;
            String authenticationUrl = null;
            String username = null;
            String password = null;
            String securityToken = null;

            if (objectDataRequestAPI == null)
            {
                throw new ArgumentNullException("ObjectDataRequest", "ObjectDataRequest object cannot be null.");
            }

            if (objectDataRequestAPI.configurationValues == null ||
                objectDataRequestAPI.configurationValues.Count == 0)
            {
                throw new ArgumentNullException("ObjectDataRequest.ConfigurationValues", "ObjectDataRequest.ConfigurationValues cannot be null or empty.");
            }

            // Get the configuration values out that are needed to load data from salesforce.com
            authenticationUrl = ValueUtils.GetContentValue(SERVICE_VALUE_AUTHENTICATION_URL, objectDataRequestAPI.configurationValues, true);
            username = ValueUtils.GetContentValue(SERVICE_VALUE_USERNAME, objectDataRequestAPI.configurationValues, true);
            password = ValueUtils.GetContentValue(SERVICE_VALUE_PASSWORD, objectDataRequestAPI.configurationValues, true);
            securityToken = ValueUtils.GetContentValue(SERVICE_VALUE_SECURITY_TOKEN, objectDataRequestAPI.configurationValues, false);

            // Create a new response object to house our results
            objectDataResponseAPI = new ObjectDataResponseAPI();

            // TODO: Should really get the culture that the authenticated user is running under
            objectDataResponseAPI.culture = objectDataRequestAPI.culture;
            // We can do this as we've applied the changes to the request objects
            objectDataResponseAPI.objectData = objectDataRequestAPI.objectData;

            // We take the object data type information to give us the properties definition as defined by the calling flow
            objectDataType = objectDataRequestAPI.objectDataType;

            // Check to see if a command was supplied
            if (objectDataRequestAPI.command != null)
            {
                String soql = null;

                // The user specified a command, we should use that to get the data
                if (objectDataRequestAPI.command.commandType == null ||
                    objectDataRequestAPI.command.commandType.Trim().Length == 0)
                {
                    throw new ArgumentNullException("ObjectDataRequest.Command.CommandType", "ObjectDataRequestAPI.Command.CommandType cannot be null or blank when executing commands - use 'SOQL'.");
                }

                if (objectDataRequestAPI.command.properties == null ||
                    objectDataRequestAPI.command.properties.Count == 0)
                {
                    throw new ArgumentNullException("ObjectDataRequest.Command.Properties", "ObjectDataRequestAPI.Command.Properties cannot be null or empty when executing commands. For SOQL commands, add a key for 'soql' with the value being your actual SOQL code.");
                }

                if (objectDataRequestAPI.command.properties.ContainsKey("soql") == false)
                {
                    throw new ArgumentNullException("ObjectDataRequest.Command.Properties", "ObjectDataRequestAPI.Command.Properties does not contain a key for 'soql'. The key check is case sensitive.");
                }

                // Get the soql out of the request
                objectDataRequestAPI.command.properties.TryGetValue("soql", out soql);

                if (soql == null ||
                    soql.Trim().Length == 0)
                {
                    throw new ArgumentNullException("ObjectDataRequest.Command.Properties", "ObjectDataRequestAPI.Command.Properties 'soql' value cannot be null or blank.");
                }

                // Execute the soql command
                objectDataResponseAPI.objectData = SalesforceDataSingleton.GetInstance().Select(authenticatedWho, authenticationUrl, username, password, securityToken, objectDataType.developerName, objectDataType.properties, objectDataRequestAPI.listFilter, soql);
            }
            else
            {
                // Do the actual selection to populate one or many of these object types
                objectDataResponseAPI.objectData = SalesforceDataSingleton.GetInstance().Select(authenticatedWho, authenticationUrl, username, password, securityToken, objectDataType.developerName, objectDataType.properties, objectDataRequestAPI.listFilter);
            }

            // Check to see if we should be telling the caller there are more results
            if (objectDataRequestAPI.listFilter != null &&
                objectDataRequestAPI.listFilter.limit > 0 &&
                objectDataResponseAPI.objectData != null &&
                objectDataResponseAPI.objectData.Count > 0)
            {
                // We've got more records than the limit - so the hasMoreRecords flag should be true
                if (objectDataResponseAPI.objectData.Count > objectDataRequestAPI.listFilter.limit)
                {
                    // Set the flag that we have more records
                    objectDataResponseAPI.hasMoreResults = true;

                    // Remove the last object in the list - this is extra and was only retrieved to allow us to set this indicator
                    objectDataResponseAPI.objectData.RemoveAt(objectDataResponseAPI.objectData.Count - 1);
                }
            }

            return objectDataResponseAPI;
        }

        /// <summary>
        /// This method is used to check to see if the user is in the provided authorization context.
        /// </summary>
        public ObjectDataResponseAPI GetUserInAuthorizationContext(INotifier notifier, IAuthenticatedWho authenticatedWho, ObjectDataRequestAPI objectDataRequestAPI)
        {
            ObjectDataResponseAPI objectDataResponseAPI = null;
            Boolean dataAccessUsingStoredCredentials = false;
            Boolean loginUsingOAuth2 = false;
            String authenticationUrl = null;
            String chatterBaseUrl = null;
            String username = null;
            String password = null;
            String securityToken = null;
            String adminEmail = null;
            String consumerSecret = null;
            String consumerKey = null;

            if (authenticatedWho == null)
            {
                throw new ArgumentNullException("AuthenticatedWho", "AuthenticatedWho object cannot be null.");
            }

            if (objectDataRequestAPI == null)
            {
                throw new ArgumentNullException("ObjectDataRequest", "ObjectDataRequest object cannot be null.");
            }

            if (objectDataRequestAPI.configurationValues == null ||
                objectDataRequestAPI.configurationValues.Count == 0)
            {
                throw new ArgumentNullException("ObjectDataRequest.ConfigurationValues", "ObjectDataRequest.ConfigurationValues cannot be null or empty.");
            }

            // Get the configuration values out that are needed for the context lookup
            authenticationUrl = ValueUtils.GetContentValue(SERVICE_VALUE_AUTHENTICATION_URL, objectDataRequestAPI.configurationValues, true);
            chatterBaseUrl = ValueUtils.GetContentValue(SERVICE_VALUE_CHATTER_BASE_URL, objectDataRequestAPI.configurationValues, true);
            username = ValueUtils.GetContentValue(SERVICE_VALUE_USERNAME, objectDataRequestAPI.configurationValues, true);
            password = ValueUtils.GetContentValue(SERVICE_VALUE_PASSWORD, objectDataRequestAPI.configurationValues, true);
            adminEmail = ValueUtils.GetContentValue(SERVICE_VALUE_ADMIN_EMAIL, objectDataRequestAPI.configurationValues, true);
            securityToken = ValueUtils.GetContentValue(SERVICE_VALUE_SECURITY_TOKEN, objectDataRequestAPI.configurationValues, false);
            consumerSecret = ValueUtils.GetContentValue(SERVICE_VALUE_CONSUMER_SECRET, objectDataRequestAPI.configurationValues, false);
            consumerKey = ValueUtils.GetContentValue(SERVICE_VALUE_CONSUMER_KEY, objectDataRequestAPI.configurationValues, false);

            if (String.IsNullOrWhiteSpace(ValueUtils.GetContentValue(SERVICE_VALUE_DATA_ACCESS_USING_STORED_CREDENTIALS, objectDataRequestAPI.configurationValues, false)) == false)
            {
                dataAccessUsingStoredCredentials = Boolean.Parse(ValueUtils.GetContentValue(SERVICE_VALUE_DATA_ACCESS_USING_STORED_CREDENTIALS, objectDataRequestAPI.configurationValues, false));
            }

            // Check to see if the admin wants users to login using oauth2
            if (String.IsNullOrWhiteSpace(consumerSecret) == false &&
                String.IsNullOrWhiteSpace(consumerKey) == false)
            {
                // We have the consumer information, we should login using oauth
                loginUsingOAuth2 = true;
            }

            // Create a new response object to house our results
            objectDataResponseAPI = new ObjectDataResponseAPI();

            // TODO: Should really get the culture that the authenticated user is running under
            objectDataResponseAPI.culture = objectDataRequestAPI.culture;
            // We can do this as we've applied the changes to the request objects
            objectDataResponseAPI.objectData = objectDataRequestAPI.objectData;

            // We take the object data type information to give us the properties definition as defined by the calling flow
            ObjectDataTypeAPI objectDataType = objectDataRequestAPI.objectDataType;

            // Do the actual selection to populate one or many of these object types
            objectDataResponseAPI.objectData = SalesforceAuthenticationSingleton.GetInstance().GetUserInAuthorizationContext(notifier, authenticatedWho, adminEmail, authenticationUrl, chatterBaseUrl, username, password, securityToken, consumerKey, loginUsingOAuth2, objectDataRequestAPI);

            return objectDataResponseAPI;
        }

        /// <summary>
        /// This method is used to load the attributes that are available for user authentication queries.
        /// </summary>
        public ObjectDataResponseAPI LoadUserAttributes(ObjectDataRequestAPI objectDataRequestAPI)
        {
            ObjectDataResponseAPI objectDataResponseAPI = null;
            List<ObjectAPI> attributeObjects = null;

            // Populate the list of available attributes
            attributeObjects = new List<ObjectAPI>();
            attributeObjects.Add(DescribeUtils.CreateAttributeObject("Colleagues", SERVICE_VALUE_COLLEAGUES));
            attributeObjects.Add(DescribeUtils.CreateAttributeObject("Delegates", SERVICE_VALUE_DELEGATES));
            attributeObjects.Add(DescribeUtils.CreateAttributeObject("Directs", SERVICE_VALUE_DIRECTS));
            attributeObjects.Add(DescribeUtils.CreateAttributeObject("Followers", SERVICE_VALUE_FOLLOWERS));
            attributeObjects.Add(DescribeUtils.CreateAttributeObject("Following", SERVICE_VALUE_FOLLOWING));
            attributeObjects.Add(DescribeUtils.CreateAttributeObject("Managers", SERVICE_VALUE_MANAGERS));
            attributeObjects.Add(DescribeUtils.CreateAttributeObject("User", SERVICE_VALUE_USER));

            // Send the attributes back in the object data
            objectDataResponseAPI = new ObjectDataResponseAPI();
            objectDataResponseAPI.objectData = attributeObjects;

            return objectDataResponseAPI;
        }

        /// <summary>
        /// This method is used to load the attributes that are available for group authentication queries.
        /// </summary>
        public ObjectDataResponseAPI LoadGroupAttributes(ObjectDataRequestAPI objectDataRequestAPI)
        {
            ObjectDataResponseAPI objectDataResponseAPI = null;
            List<ObjectAPI> attributeObjects = null;

            // Populate the list of available attributes
            attributeObjects = new List<ObjectAPI>();
            attributeObjects.Add(DescribeUtils.CreateAttributeObject("Members", SERVICE_VALUE_MEMBERS));
            attributeObjects.Add(DescribeUtils.CreateAttributeObject("Owners", SERVICE_VALUE_OWNERS));

            // Send the attributes back in the object data
            objectDataResponseAPI = new ObjectDataResponseAPI();
            objectDataResponseAPI.objectData = attributeObjects;

            return objectDataResponseAPI;
        }

        /// <summary>
        /// This method is used to load the list of users that are available to select from.
        /// </summary>
        public ObjectDataResponseAPI LoadUsers(IAuthenticatedWho authenticatedWho, ObjectDataRequestAPI objectDataRequestAPI)
        {
            List<ObjectDataTypePropertyAPI> typePropertyAPIs = null;
            ObjectDataResponseAPI objectDataResponseAPI = null;
            ListFilterWhereAPI listFilterWhereAPI = null;
            ListFilterAPI listFilterAPI = null;
            String authenticationUrl = null;
            String username = null;
            String password = null;
            String securityToken = null;

            if (objectDataRequestAPI == null)
            {
                throw new ArgumentNullException("ObjectDataRequest", "ObjectDataRequest object cannot be null.");
            }

            if (objectDataRequestAPI.configurationValues == null ||
                objectDataRequestAPI.configurationValues.Count == 0)
            {
                throw new ArgumentNullException("ObjectDataRequest.ConfigurationValues", "ObjectDataRequest.ConfigurationValues cannot be null or empty.");
            }

            // Get the configuration values out that are needed to load the list of users
            authenticationUrl = ValueUtils.GetContentValue(SERVICE_VALUE_AUTHENTICATION_URL, objectDataRequestAPI.configurationValues, true);
            username = ValueUtils.GetContentValue(SERVICE_VALUE_USERNAME, objectDataRequestAPI.configurationValues, true);
            password = ValueUtils.GetContentValue(SERVICE_VALUE_PASSWORD, objectDataRequestAPI.configurationValues, true);
            securityToken = ValueUtils.GetContentValue(SERVICE_VALUE_SECURITY_TOKEN, objectDataRequestAPI.configurationValues, false);

            // Create an object data response object
            objectDataResponseAPI = new ObjectDataResponseAPI();

            // TODO: Should really get the culture that the authenticated user is running under
            objectDataResponseAPI.culture = objectDataRequestAPI.culture;

            // If the user has provided object data, we want to filter our request by the provided object data
            listFilterAPI = SalesforceAuthenticationSingleton.GetInstance().CreateFilterFromProvidedObjectData(objectDataRequestAPI.objectData, objectDataRequestAPI.listFilter);

            // Check to see if we should continue with the lookup - if we have no object and we're filtering by objects - then we don't have anything to filter by!
            if (objectDataRequestAPI.listFilter != null &&
                objectDataRequestAPI.listFilter.filterByProvidedObjects == true &&
                (objectDataRequestAPI.objectData == null ||
                 objectDataRequestAPI.objectData.Count == 0))
            {
                objectDataResponseAPI.objectData = null;
            }
            else
            {
                // We construct the object data type for the salesforce user implementation - we then reassign the property names to the supported ManyWho property names
                // for Group Authorization User
                typePropertyAPIs = new List<ObjectDataTypePropertyAPI>();
                typePropertyAPIs.Add(new ObjectDataTypePropertyAPI() { developerName = "Id" });
                typePropertyAPIs.Add(new ObjectDataTypePropertyAPI() { developerName = "Name" });
                typePropertyAPIs.Add(new ObjectDataTypePropertyAPI() { developerName = "Email" });
                typePropertyAPIs.Add(new ObjectDataTypePropertyAPI() { developerName = "Title" });

                // We need to add a little more the list filter to make sure we're only loading active users
                if (listFilterAPI == null)
                {
                    listFilterAPI = new ListFilterAPI();
                }

                if (listFilterAPI.where == null)
                {
                    listFilterAPI.where = new List<ListFilterWhereAPI>();
                }

                // Now we need to add the "Active" piece
                listFilterWhereAPI = new ListFilterWhereAPI();
                listFilterWhereAPI.columnName = "IsActive";
                listFilterWhereAPI.criteriaType = ManyWhoConstants.CONTENT_VALUE_IMPLEMENTATION_CRITERIA_TYPE_EQUAL;
                listFilterWhereAPI.value = "true";

                // Add the extra filter for the user lookup
                listFilterAPI.where.Add(listFilterWhereAPI);

                // Do the actual selection to populate one or many of these object types
                // TODO: For now, we pass in a null for the filter
                objectDataResponseAPI.objectData = SalesforceDataSingleton.GetInstance().Select(authenticatedWho, authenticationUrl, username, password, securityToken, "User", typePropertyAPIs, listFilterAPI);

                // Check to see if the query returned any data
                if (objectDataResponseAPI.objectData != null &&
                    objectDataResponseAPI.objectData.Count > 0)
                {
                    List<ObjectAPI> userObjects = new List<ObjectAPI>();

                    // Go through the list of returned objects and turn them into user objects that are compatible with ManyWho
                    foreach (ObjectAPI objectAPI in objectDataResponseAPI.objectData)
                    {
                        String email = null;
                        String title = null;
                        String summary = null;

                        ObjectAPI userObject = new ObjectAPI();

                        // Create a new list to hold the properties
                        userObject.properties = new List<PropertyAPI>();

                        // Tell ManyWho that this is a user object
                        userObject.developerName = ManyWhoConstants.AUTHENTICATION_GROUP_AUTHORIZATION_USER_OBJECT_DEVELOPER_NAME;

                        // Go through each of the properties in the salesforce object and translate them to the ManyWho equivalent
                        foreach (PropertyAPI propertyAPI in objectAPI.properties)
                        {
                            if (propertyAPI.developerName.Equals("Id", StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                propertyAPI.developerName = ManyWhoConstants.AUTHENTICATION_OBJECT_AUTHENTICATION_ID;

                                // Add the property to our user object
                                userObject.properties.Add(propertyAPI);

                                // Assign the external id for the object
                                userObject.externalId = propertyAPI.contentValue;
                            }
                            else if (propertyAPI.developerName.Equals("Name", StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                propertyAPI.developerName = ManyWhoConstants.AUTHENTICATION_OBJECT_FRIENDLY_NAME;

                                // Add the property to our user object
                                userObject.properties.Add(propertyAPI);
                            }
                            else if (propertyAPI.developerName.Equals("Email", StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                email = propertyAPI.contentValue;
                            }
                            else if (propertyAPI.developerName.Equals("Title", StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                title = propertyAPI.contentValue;
                            }
                        }

                        // The title isn't always included
                        if (title != null &&
                            title.Trim().Length > 0)
                        {
                            summary = title + ", " + email;
                        }
                        else
                        {
                            summary = email;
                        }

                        // Add the developer summary as a combination of the title and email
                        userObject.properties.Add(new PropertyAPI() { contentValue = summary, developerName = ManyWhoConstants.AUTHENTICATION_OBJECT_DEVELOPER_SUMMARY });

                        // Add the user object to our list
                        userObjects.Add(userObject);
                    }

                    // Overwrite our existing object data
                    objectDataResponseAPI.objectData = userObjects;
                }
            }

            return objectDataResponseAPI;
        }

        /// <summary>
        /// This method is used to load the list of groups that are available to select from.
        /// </summary>
        public ObjectDataResponseAPI LoadGroups(IAuthenticatedWho authenticatedWho, ObjectDataRequestAPI objectDataRequestAPI)
        {
            List<ObjectDataTypePropertyAPI> typePropertyAPIs = null;
            ObjectDataResponseAPI objectDataResponseAPI = null;
            ListFilterAPI listFilterAPI = null;
            String authenticationUrl = null;
            String username = null;
            String password = null;
            String securityToken = null;

            if (objectDataRequestAPI == null)
            {
                throw new ArgumentNullException("ObjectDataRequest", "ObjectDataRequest object cannot be null.");
            }

            if (objectDataRequestAPI.configurationValues == null ||
                objectDataRequestAPI.configurationValues.Count == 0)
            {
                throw new ArgumentNullException("ObjectDataRequest.ConfigurationValues", "ObjectDataRequest.ConfigurationValues cannot be null or empty.");
            }

            // Get the configuration values out that are needed to load the list of groups
            authenticationUrl = ValueUtils.GetContentValue(SERVICE_VALUE_AUTHENTICATION_URL, objectDataRequestAPI.configurationValues, true);
            username = ValueUtils.GetContentValue(SERVICE_VALUE_USERNAME, objectDataRequestAPI.configurationValues, true);
            password = ValueUtils.GetContentValue(SERVICE_VALUE_PASSWORD, objectDataRequestAPI.configurationValues, true);
            securityToken = ValueUtils.GetContentValue(SERVICE_VALUE_SECURITY_TOKEN, objectDataRequestAPI.configurationValues, false);

            // Create an object data response object
            objectDataResponseAPI = new ObjectDataResponseAPI();

            // TODO: Should really get the culture that the authenticated user is running under
            objectDataResponseAPI.culture = objectDataRequestAPI.culture;

            // If the user has provided object data, we want to filter our request by the provided object data
            listFilterAPI = SalesforceAuthenticationSingleton.GetInstance().CreateFilterFromProvidedObjectData(objectDataRequestAPI.objectData, objectDataRequestAPI.listFilter);

            // Check to see if we should continue with the lookup - if we have no object and we're filtering by objects - then we don't have anything to filter by!
            if (objectDataRequestAPI.listFilter != null &&
                objectDataRequestAPI.listFilter.filterByProvidedObjects == true &&
                (objectDataRequestAPI.objectData == null ||
                 objectDataRequestAPI.objectData.Count == 0))
            {
                objectDataResponseAPI.objectData = null;
            }
            else
            {
                // We construct the object data type for the salesforce user implementation - we then reassign the property names to the supported ManyWho property names
                // for Group Authorization User
                typePropertyAPIs = new List<ObjectDataTypePropertyAPI>();
                typePropertyAPIs.Add(new ObjectDataTypePropertyAPI() { developerName = "Id" });
                typePropertyAPIs.Add(new ObjectDataTypePropertyAPI() { developerName = "Name" });
                typePropertyAPIs.Add(new ObjectDataTypePropertyAPI() { developerName = "Description" });

                // Do the actual selection to populate one or many of these object types
                // TODO: For now, we pass in a null for the filter
                objectDataResponseAPI.objectData = SalesforceDataSingleton.GetInstance().Select(authenticatedWho, authenticationUrl, username, password, securityToken, "CollaborationGroup", typePropertyAPIs, listFilterAPI);

                // Check to see if the query returned any data
                if (objectDataResponseAPI.objectData != null &&
                    objectDataResponseAPI.objectData.Count > 0)
                {
                    // Go through the list of returned objects and turn them into group objects that are compatible with ManyWho
                    foreach (ObjectAPI objectAPI in objectDataResponseAPI.objectData)
                    {
                        // Tell ManyWho that this is a group object
                        objectAPI.developerName = ManyWhoConstants.AUTHENTICATION_GROUP_AUTHORIZATION_GROUP_OBJECT_DEVELOPER_NAME;

                        // Go through each of the properties in the salesforce object and translate them to the ManyWho equivalent
                        foreach (PropertyAPI propertyAPI in objectAPI.properties)
                        {
                            if (propertyAPI.developerName.Equals("Id", StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                propertyAPI.developerName = ManyWhoConstants.AUTHENTICATION_OBJECT_AUTHENTICATION_ID;
                            }
                            else if (propertyAPI.developerName.Equals("Name", StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                propertyAPI.developerName = ManyWhoConstants.AUTHENTICATION_OBJECT_FRIENDLY_NAME;
                            }
                            else if (propertyAPI.developerName.Equals("Description", StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                propertyAPI.developerName = ManyWhoConstants.AUTHENTICATION_OBJECT_DEVELOPER_SUMMARY;
                            }
                        }
                    }
                }
            }

            return objectDataResponseAPI;
        }

        public ObjectDataResponseAPI Delete(ObjectDataRequestAPI objectDataRequestAPI)
        {
            // Ixnay on the implementay
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is used to log the user into salesforce based on the provided credentials.
        /// </summary>
        public AuthenticatedWhoResultAPI Login(AuthenticationCredentialsAPI authenticationCredentialsAPI)
        {
            AuthenticatedWhoResultAPI authenticatedUser = null;
            GetUserInfoResult userInfoResult = null;
            SforceService sforceService = null;
            Boolean dataAccessUsingStoredCredentials = false;
            String chatterBaseUrl = null;
            String loginUrl = null;
            String consumerSecret = null;
            String consumerKey = null;

            if (authenticationCredentialsAPI == null)
            {
                throw new ArgumentNullException("AuthenticationCredentials", "AuthenticationCredentials object cannot be null.");
            }

            if (String.IsNullOrWhiteSpace(authenticationCredentialsAPI.code) == false ||
                String.IsNullOrWhiteSpace(authenticationCredentialsAPI.token) == false)
            {
                // Do nothing on the validation - we have a token or code, that's all we need
            }
            else if (String.IsNullOrWhiteSpace(authenticationCredentialsAPI.sessionToken) == false)
            {
                if (String.IsNullOrWhiteSpace(authenticationCredentialsAPI.sessionUrl) == true)
                {
                    throw new ArgumentNullException("AuthenticationCredentials.SessionUrl", "AuthenticationCredentials.SessionUrl cannot be null or blank.");
                }
            }
            else
            {
                if (String.IsNullOrWhiteSpace(authenticationCredentialsAPI.username) == true)
                {
                    throw new ArgumentNullException("AuthenticationCredentials.Username", "AuthenticationCredentials.Username cannot be null or blank.");
                }

                if (String.IsNullOrWhiteSpace(authenticationCredentialsAPI.password) == true)
                {
                    throw new ArgumentNullException("AuthenticationCredentials.Password", "AuthenticationCredentials.Password cannot be null or blank.");
                }
            }

            // Assuming everything went ok so far
            authenticatedUser = new AuthenticatedWhoResultAPI();

            if (authenticationCredentialsAPI.configurationValues != null &&
                authenticationCredentialsAPI.configurationValues.Count > 0)
            {
                // If we have configuration values, we use those to determine the login url
                loginUrl = ValueUtils.GetContentValue(SERVICE_VALUE_AUTHENTICATION_URL, authenticationCredentialsAPI.configurationValues, true);
                chatterBaseUrl = ValueUtils.GetContentValue(SERVICE_VALUE_CHATTER_BASE_URL, authenticationCredentialsAPI.configurationValues, true);
                consumerSecret = ValueUtils.GetContentValue(SERVICE_VALUE_CONSUMER_SECRET, authenticationCredentialsAPI.configurationValues, false);
                consumerKey = ValueUtils.GetContentValue(SERVICE_VALUE_CONSUMER_KEY, authenticationCredentialsAPI.configurationValues, false);

                if (String.IsNullOrWhiteSpace(ValueUtils.GetContentValue(SERVICE_VALUE_DATA_ACCESS_USING_STORED_CREDENTIALS, authenticationCredentialsAPI.configurationValues, false)) == false)
                {
                    dataAccessUsingStoredCredentials = Boolean.Parse(ValueUtils.GetContentValue(SERVICE_VALUE_DATA_ACCESS_USING_STORED_CREDENTIALS, authenticationCredentialsAPI.configurationValues, false));
                }

                authenticatedUser.identityProvider = loginUrl;
            }
            else
            {
                // Assume we're dealing with production
                loginUrl = "https://login.salesforce.com";
                authenticatedUser.identityProvider = "https://login.salesforce.com";
            }

            try
            {
                if (String.IsNullOrWhiteSpace(authenticationCredentialsAPI.token) == false ||
                    String.IsNullOrWhiteSpace(authenticationCredentialsAPI.code) == false)
                {
                    if (String.IsNullOrWhiteSpace(authenticationCredentialsAPI.code) == false)
                    {
                        Task<HttpResponseMessage> message = null;
                        FormUrlEncodedContent form = null;
                        HttpClient client = null;
                        JObject jsonObject = null;
                        String serviceRefreshToken = null;
                        String serviceToken = null;
                        String result = null;
                        String identityUrl = null;

                        String endpoint = loginUrl + "/services/oauth2/token";
                        Dictionary<String, String> body = null;

                        body = new Dictionary<String, String>();
                        body.Add("client_id", consumerKey);
                        body.Add("redirect_uri", authenticationCredentialsAPI.redirectUri);
                        body.Add("client_secret", consumerSecret);
                        body.Add("grant_type", "authorization_code");
                        body.Add("code", authenticationCredentialsAPI.code);

                        client = new HttpClient();
                        form = new FormUrlEncodedContent(body);

                        // Send the request over
                        message = client.PostAsync(endpoint, form);
                        result = message.Result.Content.ReadAsStringAsync().Result;
                        jsonObject = JObject.Parse(result);

                        // Get the token information back
                        serviceToken = (String)jsonObject["access_token"];
                        serviceRefreshToken = (String)jsonObject["refresh_token"];
                        identityUrl = (String)jsonObject["id"];

                        // Now we have the identity URL, we do a GET to get the complete user info
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + serviceToken);
                        message = client.GetAsync(identityUrl);
                        result = message.Result.Content.ReadAsStringAsync().Result;
                        jsonObject = JObject.Parse(result);

                        // Assign the token to the authenticated user
                        authenticatedUser.token = serviceToken;

                        // Get the user info out so we can send it back
                        authenticatedUser.userId = (String)jsonObject["user_id"];
                        authenticatedUser.username = (String)jsonObject["username"];
                        authenticatedUser.tenantName = (String)jsonObject["organization_id"];
                        authenticatedUser.directoryId = (String)jsonObject["organization_id"];
                        authenticatedUser.directoryName = (String)jsonObject["organization_id"];
                        authenticatedUser.email = (String)jsonObject["email"];
                        authenticatedUser.firstName = (String)jsonObject["first_name"];
                        authenticatedUser.lastName = (String)jsonObject["last_name"];
                        authenticatedUser.status = ManyWhoConstants.AUTHENTICATED_USER_STATUS_AUTHENTICATED;
                        authenticatedUser.statusMessage = null;
                        authenticatedUser.token = this.CreateSalesforceAuthenticationToken(serviceToken, chatterBaseUrl + "/services/Soap/u/26.0");

                        // Check to make sure we're sending back a valid user as names can be empty
                        if (String.IsNullOrWhiteSpace(authenticatedUser.firstName) == true)
                        {
                            authenticatedUser.firstName = "(blank)";
                        }

                        if (String.IsNullOrWhiteSpace(authenticatedUser.lastName) == true)
                        {
                            authenticatedUser.lastName = "(blank)";
                        }
                    }
                }
                else
                {
                    if (String.IsNullOrWhiteSpace(authenticationCredentialsAPI.sessionToken) == false)
                    {
                        // The user has already logged into salesforce so we simply check them against the session
                        sforceService = SalesforceDataSingleton.GetInstance().Login(authenticationCredentialsAPI.sessionToken, authenticationCredentialsAPI.sessionUrl);
                    }
                    else
                    {
                        // Log the user into salesforce using the details provided
                        sforceService = SalesforceDataSingleton.GetInstance().Login(loginUrl, authenticationCredentialsAPI.username, authenticationCredentialsAPI.password, authenticationCredentialsAPI.token);
                    }

                    // Make sure the web service object is not null
                    if (sforceService == null)
                    {
                        throw new ArgumentNullException("SForceService", "SforceService is null.");
                    }

                    // Get the user info from the service
                    userInfoResult = sforceService.getUserInfo();

                    // Looks like the credentials are OK, so we create an authenticated user object for this user
                    authenticatedUser.userId = userInfoResult.userId;
                    authenticatedUser.username = userInfoResult.userName;
                    authenticatedUser.token = this.CreateSalesforceAuthenticationToken(sforceService.SessionHeaderValue.sessionId, sforceService.Url);
                    authenticatedUser.tenantName = userInfoResult.organizationName;
                    authenticatedUser.directoryId = userInfoResult.organizationId;
                    authenticatedUser.directoryName = userInfoResult.organizationName;
                    authenticatedUser.email = userInfoResult.userEmail;
                    authenticatedUser.firstName = userInfoResult.userFullName;
                    authenticatedUser.lastName = userInfoResult.userFullName;
                    authenticatedUser.status = ManyWhoConstants.AUTHENTICATED_USER_STATUS_AUTHENTICATED;
                    authenticatedUser.statusMessage = null;
                }
            }
            catch (Exception exception)
            {
                // If there's an error, we simply deny the user access
                authenticatedUser.status = ManyWhoConstants.AUTHENTICATED_USER_STATUS_ACCESS_DENIED;
                authenticatedUser.statusMessage = exception.Message;
            }

            return authenticatedUser;
        }

        /// <summary>
        /// This method is used to create a new activity stream in salesforce based on the provided configuration.
        /// </summary>
        public String CreateStream(INotifier notifier, IAuthenticatedWho authenticatedWho, SocialServiceRequestAPI socialServiceRequestAPI)
        {
            List<ObjectAPI> manywhoObjects = null;
            ObjectAPI manywhoObject = null;
            String authenticationUrl = null;
            String username = null;
            String password = null;
            String securityToken = null;
            String streamId = null;
            String adminEmail = null;

            if (authenticatedWho == null)
            {
                throw new ArgumentNullException("AuthenticatedWho", "AuthenticatedWho object cannot be null.");
            }

            if (socialServiceRequestAPI == null)
            {
                throw new ArgumentNullException("SocialServiceRequest", "SocialServiceRequest object cannot be null.");
            }

            if (socialServiceRequestAPI.configurationValues == null ||
                socialServiceRequestAPI.configurationValues.Count == 0)
            {
                throw new ArgumentNullException("SocialServiceRequest.ConfigurationValues", "SocialServiceRequest.ConfigurationValues cannot be null or empty.");
            }

            // Grab the required configuration values needed to create the stream
            authenticationUrl = ValueUtils.GetContentValue(SERVICE_VALUE_AUTHENTICATION_URL, socialServiceRequestAPI.configurationValues, true);
            username = ValueUtils.GetContentValue(SERVICE_VALUE_USERNAME, socialServiceRequestAPI.configurationValues, true);
            password = ValueUtils.GetContentValue(SERVICE_VALUE_PASSWORD, socialServiceRequestAPI.configurationValues, true);
            securityToken = ValueUtils.GetContentValue(SERVICE_VALUE_SECURITY_TOKEN, socialServiceRequestAPI.configurationValues, false);
            adminEmail = ValueUtils.GetContentValue(SERVICE_VALUE_ADMIN_EMAIL, socialServiceRequestAPI.configurationValues, true);

            // Create a ManyWho record to host the stream
            manywhoObject = new ObjectAPI();
            manywhoObject.developerName = "ManyWhoFlow__c";
            manywhoObject.properties = new List<PropertyAPI>();
            manywhoObject.properties.Add(new PropertyAPI() { developerName = "Name", contentValue = "ManyWho Flow Collaboration" });
            manywhoObject.properties.Add(new PropertyAPI() { developerName = "Id", contentValue = "" });

            // Add the object to the list of objects to save
            manywhoObjects = new List<ObjectAPI>();
            manywhoObjects.Add(manywhoObject);

            // Save the manywho object to salesforce
            manywhoObjects = SalesforceDataSingleton.GetInstance().Save(notifier, authenticatedWho, authenticationUrl, username, password, securityToken, manywhoObjects);

            // Check to see if anything came back as part of the save - it should unless there was a fault
            if (manywhoObjects != null &&
                manywhoObjects.Count > 0)
            {
                // Grab the first object from the returned manywho objects
                manywhoObject = manywhoObjects[0];

                // Grab the id from that object - this is our stream id
                streamId = manywhoObject.externalId;
            }
            else
            {
                // If we didn't get any objects back, we need to throw an error
                throw new ArgumentNullException("SocialNetworkStream", "Social network stream could not be created.");
            }

            return streamId;
        }

        /// <summary>
        /// This method is used to get the user info for the logged in user in salesforce.
        /// </summary>
        public WhoAPI GetCurrentUserInfo(INotifier notifier, IAuthenticatedWho authenticatedWho, String streamId, SocialServiceRequestAPI serviceRequest)
        {
            return SalesforceSocialSingleton.GetInstance().GetUserInfoById(notifier, authenticatedWho, streamId, CHATTER_ME, serviceRequest);
        }

        /// <summary>
        /// This method is used to get the user info for the provided user id in salesforce.
        /// </summary>
        public WhoAPI GetUserInfo(INotifier notifier, IAuthenticatedWho authenticatedWho, String streamId, String id, SocialServiceRequestAPI serviceRequest)
        {
            return SalesforceSocialSingleton.GetInstance().GetUserInfoById(notifier, authenticatedWho, streamId, id, serviceRequest);
        }

        /// <summary>
        /// This method is used to get the list of stream followers in salesforce.
        /// </summary>
        public List<WhoAPI> GetStreamFollowers(INotifier notifier, IAuthenticatedWho authenticatedWho, String streamId, SocialServiceRequestAPI socialServiceRequest)
        {
            WebException webException = null;
            ChatterGroupFollowingResponse chatterGroupFollowingResponse = null;
            HttpResponseMessage httpResponseMessage = null;
            HttpClient httpClient = null;
            List<WhoAPI> whos = null;
            WhoAPI who = null;
            String chatterBaseUrl = null;
            String endpointUrl = null;
            String adminEmail = null;

            if (authenticatedWho == null)
            {
                throw new ArgumentNullException("AuthenticatedWho", "Authenticated user is null");
            }

            if (streamId == null ||
                streamId.Trim().Length == 0)
            {
                throw new ArgumentNullException("StreamId", "Id for stream is null or blank");
            }

            if (socialServiceRequest == null)
            {
                throw new ArgumentNullException("SocialServiceRequest", "SocialServiceRequest is null");
            }

            // Grab the necessary configuration values to make the followers call
            chatterBaseUrl = ValueUtils.GetContentValue(SERVICE_VALUE_CHATTER_BASE_URL, socialServiceRequest.configurationValues, true);
            adminEmail = ValueUtils.GetContentValue(SERVICE_VALUE_ADMIN_EMAIL, socialServiceRequest.configurationValues, true);

            // We enclose the request in a for loop to handle http errors
            for (int i = 0; i < SalesforceHttpUtils.MAXIMUM_RETRIES; i++)
            {
                try
                {
                    // Create a new client object
                    httpClient = SalesforceHttpUtils.CreateHttpClient(SalesforceHttpUtils.GetAuthenticationDetails(authenticatedWho.Token).Token);

                    // Create the endpoint url
                    endpointUrl = chatterBaseUrl + CHATTER_URI_PART_API_VERSION + String.Format(CHATTER_URI_PART_STREAM_FOLLOWERS, streamId);

                    // TODO: Need to add paging support to this as it currently only sends back the first page of results
                    httpResponseMessage = httpClient.GetAsync(endpointUrl).Result;

                    // Check the status of the response and respond appropriately
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        // Read in the group following
                        chatterGroupFollowingResponse = httpResponseMessage.Content.ReadAsAsync<ChatterGroupFollowingResponse>().Result;

                        // Check to see if we do in fact have any followers
                        if (chatterGroupFollowingResponse.Followers != null &&
                            chatterGroupFollowingResponse.Followers.Count > 0)
                        {
                            whos = new List<WhoAPI>();

                            // Go through the followers and convert them into "whos"
                            foreach (ChatterGroupFollowing chatterGroupFollowing in chatterGroupFollowingResponse.Followers)
                            {
                                if (chatterGroupFollowing.Subscriber != null)
                                {
                                    who = SalesforceSocialSingleton.GetInstance().ChatterUserInfoToWhoAPI(chatterGroupFollowing.Subscriber);

                                    whos.Add(who);
                                }
                            }
                        }

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
                    HttpUtils.CleanUpHttp(httpClient, null, httpResponseMessage);
                }
            }

            return whos;
        }

        /// <summary>
        /// This method is used to get the list of stream messages in salesforce.
        /// </summary>
        public async Task<MessageListAPI> GetStreamMessages(INotifier notifier, IAuthenticatedWho authenticatedWho, String streamId, SocialServiceRequestAPI socialServiceRequest)
        {
            WebException webException = null;
            ChatterGetMessagesResponse chatterGetMessagesResponse = null;
            HttpResponseMessage httpResponseMessage = null;
            HttpClient httpClient = null;
            ChatterComents chatterComments = null;
            MessageListAPI messageList = null;
            String chatterBaseUrl = null;
            String endpointUrl = null;
            String adminEmail = null;
            String nextPage = null;

            if (authenticatedWho == null)
            {
                throw new ArgumentNullException("AuthenticatedWho", "AuthenticatedWho object cannot be null.");
            }

            if (streamId == null ||
                streamId.Trim().Length == 0)
            {
                throw new ArgumentNullException("StreamId", "StreamId cannot be null or blank.");
            }

            if (socialServiceRequest == null)
            {
                throw new ArgumentNullException("SocialServiceRequest", "SocialServiceRequest object cannot be null.");
            }

            // Grab the necessary configuration values to make the followers call
            chatterBaseUrl = ValueUtils.GetContentValue(SERVICE_VALUE_CHATTER_BASE_URL, socialServiceRequest.configurationValues, true);
            adminEmail = ValueUtils.GetContentValue(SERVICE_VALUE_ADMIN_EMAIL, socialServiceRequest.configurationValues, true);

            // If we have the page specified, we add it to the endpoint url
            if (socialServiceRequest.page != null &&
                socialServiceRequest.page.Trim().Length > 0)
            {
                endpointUrl += "&page=" + socialServiceRequest.page;
            }

            // We enclose the request in a for loop to handle http errors
            for (int i = 0; i < SalesforceHttpUtils.MAXIMUM_RETRIES; i++)
            {
                try
                {
                    // Create a new client object
                    httpClient = SalesforceHttpUtils.CreateHttpClient(SalesforceHttpUtils.GetAuthenticationDetails(authenticatedWho.Token).Token);

                    // Create the endpoint url
                    endpointUrl = chatterBaseUrl + CHATTER_URI_PART_API_VERSION + String.Format(CHATTER_URI_PART_POSTS, streamId) + "?pageSize=" + socialServiceRequest.pageSize;

                    // TODO: Need to add paging support to this as it currently only sends back the first page of results
                    httpResponseMessage = httpClient.GetAsync(endpointUrl).Result;

                    // Check the status of the response and respond appropriately
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        // Grab the chatter messages response
                        chatterGetMessagesResponse = await httpResponseMessage.Content.ReadAsAsync<ChatterGetMessagesResponse>();

                        // Grab the full next page
                        nextPage = chatterGetMessagesResponse.NextPageUrl;

                        // If we have a next page url, parse out the actual id
                        if (nextPage != null &&
                            nextPage.Trim().Length > 0)
                        {
                            Int32 endIndex = nextPage.IndexOf("&pageSize=");

                            if (endIndex > 0)
                            {
                                // Grab everything after the page= bit but befor the pageSize bit
                                nextPage = nextPage.Substring(nextPage.IndexOf("page=") + "page=".Length, endIndex);
                            }
                            else
                            {
                                // Grab everything after the page= bit
                                nextPage = nextPage.Substring(nextPage.IndexOf("page=") + "page=".Length);
                            }
                        }

                        // Create a new message list for the response
                        messageList = new MessageListAPI();

                        // Assign the next page
                        messageList.nextPage = nextPage;

                        // Convert all of the chatter messages to standard messages
                        messageList.messages = SalesforceSocialSingleton.GetInstance().ChatterMessageToMessageAPI(chatterBaseUrl, null, chatterGetMessagesResponse.Items);

                        // Now that we have the messages, we need to collect up the comments for all of the messages
                        if (messageList.messages != null &&
                            messageList.messages.Count > 0)
                        {
                            foreach (MessageAPI message in messageList.messages)
                            {
                                // Check to see if this message actually has any comments
                                if (message.commentsCount > 0)
                                {
                                    message.comments = new List<MessageAPI>();

                                    // Create the url for collecting the comments - we collect a maximum of 50
                                    endpointUrl = chatterBaseUrl + CHATTER_URI_PART_API_VERSION + String.Format(CHATTER_URI_PART_COMMENTS, message.id) + "?pageSize=50";

                                    // send response to get messages latest then older message from message array
                                    httpResponseMessage = await httpClient.GetAsync(endpointUrl);
                                    if (httpResponseMessage.IsSuccessStatusCode == true)
                                    {
                                        // Grab the comments from the response
                                        chatterComments = await httpResponseMessage.Content.ReadAsAsync<ChatterComents>();

                                        // Now convert all of the comments over to messages also
                                        message.comments = SalesforceSocialSingleton.GetInstance().ChatterMessageToMessageAPI(chatterBaseUrl, message.id, chatterComments.Comments);
                                    }
                                    else
                                    {
                                        throw new ArgumentNullException("HttpResponse", httpResponseMessage.ReasonPhrase);
                                    }
                                }
                            }
                        }

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
                    HttpUtils.CleanUpHttp(httpClient, null, httpResponseMessage);
                }
            }

            return messageList;
        }

        /// <summary>
        /// This method allows the user to share the flow app in salesforce with their friends.
        /// </summary>
        public async Task<MessageAPI> ShareMessage(IAuthenticatedWho authenticatedWho, String streamId, HttpContent httpContent)
        {
            WebException webException = null;
            MultipartFormDataStreamProvider multipartFormDataStreamProvider = null;
            HttpResponseMessage httpResponseMessage = null;
            HttpClient httpClient = null;
            MediaTypeFormatter jsonFormatter = null;
            MultipartFormDataContent multipartFormDataContent = null;
            SocialServiceRequestAPI socialServiceRequest = null;
            ChatterNewMessageBody chatterNewMessageBody = null;
            ChatterPostedMessage chatterPostedMessage = null;
            ChatterMessage chatterMessage = null;
            ChatterAttachmentLink chatterAttachmentLink = null;
            INotifier notifier = null;
            MessageAPI message = null;
            String chatterBaseUrl = null;
            String endpointUrl = null;
            String adminEmail = null;

            if (authenticatedWho == null)
            {
                throw new ArgumentNullException("AuthenticatedWho", "AuthenticatedWho object cannot be null.");
            }

            if (streamId == null ||
                streamId.Trim().Length == 0)
            {
                throw new ArgumentNullException("StreamId", "StreamId cannot be null or blank.");
            }

            if (httpContent == null)
            {
                throw new ArgumentNullException("HttpContent", "HttpContent object cannot be null.");
            }

            // Check if the request contains multipart/form-data.
            if (httpContent.IsMimeMultipartContent() == false)
            {
                throw new ArgumentNullException("UnsupportedMediaType", "The plugin is expecting a multipart/form-data post.");
            }

            // Stream the data to the local temp folder
            multipartFormDataStreamProvider = new MultipartFormDataStreamProvider(Path.GetTempPath());

            // Read the content in the request into the stream provider
            await httpContent.ReadAsMultipartAsync(multipartFormDataStreamProvider);

            // Now we can create the multipart form we're going to post over to salesforce
            multipartFormDataContent = new MultipartFormDataContent();

            // Create a new chatter posted message object which will manage the correct json format
            chatterPostedMessage = new ChatterPostedMessage();

            // Find the form part that's the service request, we need to this to create our chatter message, but also to get configuration information
            foreach (HttpContent content in multipartFormDataStreamProvider.Contents)
            {
                if (content.Headers.ContentDisposition.Name.Equals(ManyWhoConstants.SERVICE_REQUEST_FORM_POST_KEY, StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    socialServiceRequest = content.ReadAsAsync<SocialServiceRequestAPI>().Result;
                    break;
                }
            }

            if (socialServiceRequest == null)
            {
                throw new ArgumentNullException("SocialServiceRequest", "SocialServiceRequest object cannot be null.");
            }

            notifier = EmailNotifier.GetInstance(authenticatedWho, socialServiceRequest.configurationValues, "SalesforceServiceSingleton.ShareMessage");

            // Grab the values necessary to post the message over to chatter
            chatterBaseUrl = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_CHATTER_BASE_URL, socialServiceRequest.configurationValues, true);
            adminEmail = ValueUtils.GetContentValue(SERVICE_VALUE_ADMIN_EMAIL, socialServiceRequest.configurationValues, true);

            // Grab the new message from the service request and convert that over to a chatter message
            chatterNewMessageBody = SalesforceSocialSingleton.GetInstance().ConvertNewMessageAPIToChatterNewMessageBody(socialServiceRequest.newMessage);

            // Apply the chatter message to the posted message body
            chatterPostedMessage.Body = chatterNewMessageBody;

            // We also add the link to the app so the user has it
            chatterAttachmentLink = new ChatterAttachmentLink();
            chatterAttachmentLink.AttachmentType = "Link";
            chatterAttachmentLink.Url = socialServiceRequest.playerUri;
            chatterAttachmentLink.UrlName = "Link to ManyWho Flow";

            chatterPostedMessage.Attachment = chatterAttachmentLink;

            // We enclose the request in a for loop to handle http errors
            for (int i = 0; i < SalesforceHttpUtils.MAXIMUM_RETRIES; i++)
            {
                try
                {
                    // Create a new client object
                    httpClient = SalesforceHttpUtils.CreateHttpClient(SalesforceHttpUtils.GetAuthenticationDetails(authenticatedWho.Token).Token);

                    // Create the endpoint url
                    endpointUrl = chatterBaseUrl + CHATTER_URI_PART_API_VERSION + String.Format(CHATTER_URI_PART_MY_FEED, authenticatedWho.UserId);

                    // Create a new json formatter so the request will be in the right format
                    jsonFormatter = new JsonMediaTypeFormatter();

                    // Use the JSON formatter to create the content of the chatter post
                    multipartFormDataContent.Add(new ObjectContent<ChatterPostedMessage>(chatterPostedMessage, jsonFormatter), "json");

                    // Post the message over to chatter
                    httpResponseMessage = await httpClient.PostAsync(endpointUrl, multipartFormDataContent);

                    // Check the status of the response and respond appropriately
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        // Grab the chatter message from the post
                        chatterMessage = await httpResponseMessage.Content.ReadAsAsync<ChatterMessage>();

                        // Convert it over to a manywho message
                        message = SalesforceSocialSingleton.GetInstance().ChatterMessageToMessageAPI(chatterBaseUrl, null, chatterMessage);

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
                    HttpUtils.CleanUpHttp(httpClient, null, httpResponseMessage);
                }
            }

            return message;
        }

        /// <summary>
        /// This method allows the user to post a new message to the stream in chatter.
        /// </summary>
        public async Task<MessageAPI> PostNewMessage(IAuthenticatedWho authenticatedWho, String streamId, HttpContent httpContent)
        {
            WebException webException = null;
            MultipartFormDataStreamProvider multipartFormDataStreamProvider = null;
            HttpResponseMessage httpResponseMessage = null;
            HttpClient httpClient = null;
            MediaTypeFormatter jsonFormatter = null;
            MultipartFormDataContent multipartFormDataContent = null;
            SocialServiceRequestAPI socialServiceRequest = null;
            ChatterNewMessageBody chatterNewMessageBody = null;
            ChatterPostedMessage chatterPostedMessage = null;
            ChatterMessage chatterMessage = null;
            INotifier notifier = null;
            MessageAPI message = null;
            String chatterBaseUrl = null;
            String endpointUrl = null;
            String adminEmail = null;

            if (authenticatedWho == null)
            {
                throw new ArgumentNullException("AuthenticatedWho", "AuthenticatedWho object cannot be null.");
            }

            if (streamId == null ||
                streamId.Trim().Length == 0)
            {
                throw new ArgumentNullException("StreamId", "StreamId cannot be null or blank.");
            }

            if (httpContent == null)
            {
                throw new ArgumentNullException("HttpContent", "HttpContent object cannot be null");
            }

            // Check if the request contains multipart/form-data.
            if (httpContent.IsMimeMultipartContent() == false)
            {
                throw new ArgumentNullException("UnsupportedMediaType", "The plugin is expecting a multipart/form-data post.");
            }

            // We enclose the request in a for loop to handle http errors
            for (int i = 0; i < SalesforceHttpUtils.MAXIMUM_RETRIES; i++)
            {
                try
                {
                    // Stream the data to the local temp folder
                    multipartFormDataStreamProvider = new MultipartFormDataStreamProvider(Path.GetTempPath());

                    // Read the content in the request into the stream provider
                    await httpContent.ReadAsMultipartAsync(multipartFormDataStreamProvider);

                    // Now we can create the multipart form we're going to post over to salesforce
                    multipartFormDataContent = new MultipartFormDataContent();

                    // Create a new chatter posted message object which will manage the correct json format
                    chatterPostedMessage = new ChatterPostedMessage();

                    // Find the form part that's the service request, we need to this to create our chatter message, but also to get configuration information
                    foreach (HttpContent content in multipartFormDataStreamProvider.Contents)
                    {
                        if (content.Headers.ContentDisposition.Name.Equals(ManyWhoConstants.SERVICE_REQUEST_FORM_POST_KEY, StringComparison.InvariantCultureIgnoreCase) == true)
                        {
                            socialServiceRequest = content.ReadAsAsync<SocialServiceRequestAPI>().Result;
                            break;
                        }
                    }

                    if (socialServiceRequest == null)
                    {
                        throw new ArgumentNullException("SocialServiceRequest", "SocialServiceRequest object cannot be null.");
                    }

                    notifier = EmailNotifier.GetInstance(authenticatedWho, socialServiceRequest.configurationValues, "SalesforceServiceSingleton.PostNewMessage");

                    // Now we have the file data request, we grab out the data that needs to be uploaded to the service
                    if (multipartFormDataStreamProvider.FileData != null &&
                        multipartFormDataStreamProvider.FileData.Count > 0)
                    {
                        ChatterAttachmentFile chatterAttachmentFile = new ChatterAttachmentFile();

                        // Go through all of the file data in the stream provider
                        foreach (MultipartFileData fileData in multipartFormDataStreamProvider.FileData)
                        {
                            String fileName = null;
                            //MemoryStream memoryStream = null;
                            byte[] fileBinaryData = null;

                            // Grab the file name so we have it for storage
                            fileName = Path.GetFileName(fileData.Headers.ContentDisposition.FileName.Trim('"'));

                            chatterAttachmentFile.AttachmentType = "NewFile";
                            chatterAttachmentFile.Description = "";
                            chatterAttachmentFile.Title = fileName;

                            // Create the attachment part of the posted message
                            chatterPostedMessage.Attachment = chatterAttachmentFile;

                            // We don't store the file, we simply pass it up to the remote service to store
                            using (FileStream fileStream = new FileStream(fileData.LocalFileName, FileMode.Open))
                            {
                                // Create a new byte array  to store the binary data
                                fileBinaryData = new byte[fileStream.Length];

                                // Read the file stream into the byte array
                                fileStream.Read(fileBinaryData, 0, fileBinaryData.Length);
                                
                                ByteArrayContent streamContent = null;

                                streamContent = new ByteArrayContent(fileBinaryData);
                                //streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("file") { FileName = fileName };
                                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                                // We have a file, simply pass it forward but using the correct metadata for chatter
                                multipartFormDataContent.Add(streamContent, "feedItemFileUpload", fileName);
                                //multipartFormDataContent.Add(
                            }

                            // Delete the file in our system so we don't have the file locally and in blob storage
                            //File.Delete(fileData.LocalFileName);
                        }
                    }

                    // Grab the values necessary to post the message over to chatter
                    chatterBaseUrl = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_CHATTER_BASE_URL, socialServiceRequest.configurationValues, true);
                    adminEmail = ValueUtils.GetContentValue(SERVICE_VALUE_ADMIN_EMAIL, socialServiceRequest.configurationValues, true);

                    // Grab the new message from the service request and convert that over to a chatter message
                    chatterNewMessageBody = SalesforceSocialSingleton.GetInstance().ConvertNewMessageAPIToChatterNewMessageBody(socialServiceRequest.newMessage);

                    // Apply the chatter message to the posted message body
                    chatterPostedMessage.Body = chatterNewMessageBody;

                    // We also check if any users have been @mentioned - if so, and we don't have a file - we put the link to the flow as an attachment - but only
                    // if this is the top level message, we don't do it for comments against a main message for now as many social networks don't support attachments
                    // on comments (1 being chatter)
                    if (chatterPostedMessage.Attachment == null &&
                        socialServiceRequest.newMessage.repliedTo == null)
                    {
                        Boolean atMentionedUser = false;

                        // As we don't have an attachment, we can attach a link to the flow for any @mentioned users
                        if (chatterNewMessageBody.MessageSegments != null &&
                            chatterNewMessageBody.MessageSegments.Count > 0)
                        {
                            // As we have some segments, we check to see if any of these have a mentioned user
                            foreach (ChatterSegment chatterSegment in chatterNewMessageBody.MessageSegments)
                            {
                                if (chatterSegment is ChatterMentionsSegment)
                                {
                                    // We have an @mentioned user, so we want to attach a link to the flow
                                    atMentionedUser = true;
                                    break;
                                }
                            }
                        }

                        // We can go ahead and attach a link to the flow
                        if (atMentionedUser == true)
                        {
                            ChatterAttachmentLink chatterAttachmentLink = new ChatterAttachmentLink();

                            chatterAttachmentLink.AttachmentType = "Link";
                            chatterAttachmentLink.Url = socialServiceRequest.joinPlayerUri;
                            chatterAttachmentLink.UrlName = "Click To Join Flow";

                            chatterPostedMessage.Attachment = chatterAttachmentLink;
                        }
                    }

                    // Create a new client object
                    httpClient = SalesforceHttpUtils.CreateHttpClient(SalesforceHttpUtils.GetAuthenticationDetails(authenticatedWho.Token).Token);

                    // Create a new json formatter so the request will be in the right format
                    jsonFormatter = new JsonMediaTypeFormatter();

                    // Use the JSON formatter to create the content of the chatter post
                    multipartFormDataContent.Add(new ObjectContent<ChatterPostedMessage>(chatterPostedMessage, jsonFormatter), "json");

                    // Post the files over to chatter
                    if (socialServiceRequest.newMessage.repliedTo != null)
                    {
                        endpointUrl = chatterBaseUrl + SalesforceServiceSingleton.CHATTER_URI_PART_API_VERSION + String.Format(SalesforceServiceSingleton.CHATTER_URI_PART_COMMENTS, socialServiceRequest.newMessage.repliedTo);
                    }
                    else
                    {
                        endpointUrl = chatterBaseUrl + SalesforceServiceSingleton.CHATTER_URI_PART_API_VERSION + String.Format(SalesforceServiceSingleton.CHATTER_URI_PART_POSTS, streamId);
                    }

                    // Post the message over to chatter
                    httpResponseMessage = await httpClient.PostAsync(endpointUrl, multipartFormDataContent);

                    // Check the status of the response and respond appropriately
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        // Grab the chatter message from the post
                        chatterMessage = await httpResponseMessage.Content.ReadAsAsync<ChatterMessage>();

                        // Convert it over to a manywho message
                        message = SalesforceSocialSingleton.GetInstance().ChatterMessageToMessageAPI(chatterBaseUrl, null, chatterMessage);

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
                    HttpUtils.CleanUpHttp(httpClient, null, httpResponseMessage);
                }
            }

            return message;
        }

        /// <summary>
        /// This method allows the user to delete messages from the stream in chatter.
        /// </summary>
        public async Task<String> DeleteMessage(INotifier notifier, IAuthenticatedWho authenticatedWho, String streamId, String messageId, SocialServiceRequestAPI socialServiceRequest)
        {
            WebException webException = null;
            HttpResponseMessage httpResponseMessage = null;
            HttpClient httpClient = null;
            String chatterBaseUrl = null;
            String endpointUrl = null;
            String adminEmail = null;
            String response = null;

            if (authenticatedWho == null)
            {
                throw new ArgumentNullException("AuthenticatedWho", "AuthenticatedWho object cannot be null.");
            }

            if (streamId == null ||
                streamId.Trim().Length == 0)
            {
                throw new ArgumentNullException("StreamId", "StreamId cannot be null or blank.");
            }

            if (messageId == null ||
                messageId.Trim().Length == 0)
            {
                throw new ArgumentNullException("MessageId", "MessageId cannot be null or blank.");
            }

            if (socialServiceRequest == null)
            {
                throw new ArgumentNullException("SocialServiceRequest", "SocialServiceRequest object cannot be null.");
            }

            // Grab the values necessary to delete the message from chatter
            chatterBaseUrl = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_CHATTER_BASE_URL, socialServiceRequest.configurationValues, true);
            adminEmail = ValueUtils.GetContentValue(SERVICE_VALUE_ADMIN_EMAIL, socialServiceRequest.configurationValues, true);

            // We enclose the request in a for loop to handle http errors
            for (int i = 0; i < SalesforceHttpUtils.MAXIMUM_RETRIES; i++)
            {
                try
                {
                    // Create a new client object
                    httpClient = SalesforceHttpUtils.CreateHttpClient(SalesforceHttpUtils.GetAuthenticationDetails(authenticatedWho.Token).Token);

                    // Create the end point url for the delete
                    endpointUrl = chatterBaseUrl + CHATTER_URI_PART_API_VERSION + CHATTER_URI_PART_FEED_ITEMS + messageId;

                    // Send the message over to chatter
                    httpResponseMessage = await httpClient.DeleteAsync(endpointUrl);

                    // Check the status of the response and respond appropriately
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        // Tell the caller that everything was OK
                        response = HttpStatusCode.OK.ToString();

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
                    HttpUtils.CleanUpHttp(httpClient, null, httpResponseMessage);
                }
            }

            return response;
        }

        /// <summary>
        /// This method allows the user to like messages in the stream in chatter.
        /// </summary>
        public async Task<String> LikeMessage(INotifier notifier, IAuthenticatedWho authenticatedWho, String streamId, String messageId, Boolean like, SocialServiceRequestAPI socialServiceRequest)
        {
            WebException webException = null;
            HttpClient httpClient = null;
            HttpResponseMessage httpResponseMessage = null;
            ChatterMessage chatterMessage = null;
            String chatterBaseUrl = null;
            String endpointUrl = null;
            String adminEmail = null;
            String myLikeId = null;
            String response = null;

            if (authenticatedWho == null)
            {
                throw new ArgumentNullException("AuthenticatedWho", "AuthenticatedWho object cannot be null.");
            }

            if (streamId == null ||
                streamId.Trim().Length == 0)
            {
                throw new ArgumentNullException("StreamId", "StreamId cannot be null or blank.");
            }

            if (messageId == null ||
                messageId.Trim().Length == 0)
            {
                throw new ArgumentNullException("MessageId", "MessageId cannot be null or blank.");
            }

            if (socialServiceRequest == null)
            {
                throw new ArgumentNullException("SocialServiceRequest", "SocialServiceRequest object cannot be null.");
            }

            // Grab the values necessary to like the message in chatter
            chatterBaseUrl = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_CHATTER_BASE_URL, socialServiceRequest.configurationValues, true);
            adminEmail = ValueUtils.GetContentValue(SERVICE_VALUE_ADMIN_EMAIL, socialServiceRequest.configurationValues, true);

            // We enclose the request in a for loop to handle http errors
            for (int i = 0; i < SalesforceHttpUtils.MAXIMUM_RETRIES; i++)
            {
                try
                {
                    // Create a new client object
                    httpClient = SalesforceHttpUtils.CreateHttpClient(SalesforceHttpUtils.GetAuthenticationDetails(authenticatedWho.Token).Token);

                    if (like == true)
                    {
                        // Create the end point url for the like
                        endpointUrl = chatterBaseUrl + CHATTER_URI_PART_API_VERSION + String.Format(CHATTER_URI_PART_LIKE, messageId);

                        // Post over the like request
                        httpResponseMessage = await httpClient.PostAsync(endpointUrl, null);

                        if (httpResponseMessage.IsSuccessStatusCode == true)
                        {
                            response = HttpStatusCode.OK.ToString();

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
                    else
                    {
                        // Create the end point url for the post so we can get the like
                        endpointUrl = chatterBaseUrl + CHATTER_URI_PART_API_VERSION + CHATTER_URI_PART_FEED_ITEMS + messageId;

                        // Do a get request
                        httpResponseMessage = await httpClient.GetAsync(endpointUrl);

                        if (httpResponseMessage.IsSuccessStatusCode == true)
                        {
                            // Get the message back from the system
                            chatterMessage = httpResponseMessage.Content.ReadAsAsync<ChatterMessage>().Result;

                            // Get my like id from the message
                            myLikeId = SalesforceSocialSingleton.GetInstance().ChatterMessageToMessageAPI(chatterBaseUrl, null, chatterMessage).myLikeId;

                            // If we have the like id, we can now delete it from chatter
                            if (myLikeId != null &&
                                myLikeId.Trim().Length > 0)
                            {
                                // Create the end point url for deleting the like
                                endpointUrl = chatterBaseUrl + CHATTER_URI_PART_API_VERSION + CHATTER_URI_PART_DELETE_LIKE + myLikeId;

                                // Send the request to chatter to delete the like
                                httpResponseMessage = await httpClient.DeleteAsync(endpointUrl);
                                
                                if (httpResponseMessage.IsSuccessStatusCode == true)
                                {
                                    response = HttpStatusCode.OK.ToString();

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
                    HttpUtils.CleanUpHttp(httpClient, null, httpResponseMessage);
                }
            }

            return response;
        }

        /// <summary>
        /// This method allows the user to follow the stream in chatter.
        /// </summary>
        public async Task<String> FollowStream(INotifier notifier, IAuthenticatedWho authenticatedWho, String streamId, Boolean follow, SocialServiceRequestAPI socialServiceRequest)
        {
            WebException webException = null;
            HttpClient httpClient = null;
            HttpContent httpContent = null;
            MediaTypeFormatter jsonFormatter = null;
            HttpResponseMessage httpResponseMessage = null;
            ChatterFollowing chatterFollowing = null;
            ChatterFollowingResponse chatterFollowingResponse = null;
            String chatterBaseUrl = null;
            String endpointUrl = null;
            String adminEmail = null;
            String subscriptionId = null;
            String response = null;

            if (authenticatedWho == null)
            {
                throw new ArgumentNullException("AuthenticatedWho", "AuthenticatedWho object cannot be null.");
            }

            if (streamId == null ||
                streamId.Trim().Length == 0)
            {
                throw new ArgumentNullException("StreamId", "StreamId cannot be null or blank.");
            }

            if (socialServiceRequest == null)
            {
                throw new ArgumentNullException("SocialServiceRequest", "SocialServiceRequest object cannot be null.");
            }

            // Grab the values necessary to follow the stream in chatter
            chatterBaseUrl = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_CHATTER_BASE_URL, socialServiceRequest.configurationValues, true);
            adminEmail = ValueUtils.GetContentValue(SERVICE_VALUE_ADMIN_EMAIL, socialServiceRequest.configurationValues, true);

            // We enclose the request in a for loop to handle http errors
            for (int i = 0; i < SalesforceHttpUtils.MAXIMUM_RETRIES; i++)
            {
                try
                {
                    // Create a new client object
                    httpClient = SalesforceHttpUtils.CreateHttpClient(SalesforceHttpUtils.GetAuthenticationDetails(authenticatedWho.Token).Token);

                    if (follow == true)
                    {
                        // Create the end point url for the follow request
                        endpointUrl = chatterBaseUrl + CHATTER_URI_PART_API_VERSION + CHATTER_URI_PART_FOLLOWING_TO_ME;

                        // Create a new json formatter so the request will be in the right format
                        jsonFormatter = new JsonMediaTypeFormatter();

                        // Use the JSON formatter to create the content of the request body.
                        httpContent = new ObjectContent<ChatterCreateFollowing>(new ChatterCreateFollowing { SubjectId = streamId }, jsonFormatter);

                        // Post the follow request over to chatter
                        httpResponseMessage = await httpClient.PostAsync(endpointUrl, httpContent);

                        if (httpResponseMessage.IsSuccessStatusCode == true)
                        {
                            response = HttpStatusCode.OK.ToString();

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
                    else
                    {
                        // Create the end point url for the message to unfollow request
                        endpointUrl = chatterBaseUrl + CHATTER_URI_PART_API_VERSION + CHATTER_URI_PART_FOLLOWING_ME;

                        // Send the request over to salesforce
                        httpResponseMessage = await httpClient.GetAsync(endpointUrl);
                        
                        if (httpResponseMessage.IsSuccessStatusCode == true)
                        {
                            chatterFollowingResponse = httpResponseMessage.Content.ReadAsAsync<ChatterFollowingResponse>().Result;

                            // Grab the first following where the id is the same as our stream
                            // TODO: This does need to include paging
                            chatterFollowing = chatterFollowingResponse.Following.FirstOrDefault(x => x.Subject.Id == streamId);

                            if (chatterFollowing != null)
                            {
                                subscriptionId = chatterFollowing.Id;
                            }

                            // Construct the end point url for deleting the follow
                            endpointUrl = chatterBaseUrl + CHATTER_URI_PART_API_VERSION + CHATTER_URI_PART_SUBSCRIPTIONS + subscriptionId;

                            httpResponseMessage = await httpClient.DeleteAsync(endpointUrl);

                            if (httpResponseMessage.IsSuccessStatusCode == true)
                            {
                                response = HttpStatusCode.OK.ToString();
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
                    HttpUtils.CleanUpHttp(httpClient, null, httpResponseMessage);
                }
            }

            return response;
        }

        /// <summary>
        /// This method allows the user to search for users by name in chatter.
        /// </summary>
        public async Task<List<MentionedWhoAPI>> SearchUsersByName(INotifier notifier, IAuthenticatedWho authenticatedWho, String streamId, String name, SocialServiceRequestAPI socialServiceRequest)
        {
            WebException webException = null;
            List<MentionedWhoAPI> mentionedUsers = null;
            HttpClient httpClient = null;
            HttpResponseMessage httpResponseMessage = null;
            ChatterAutoCompleteResponse chatterAutoCompleteResponse = null;
            String chatterBaseUrl = null;
            String endpointUrl = null;
            String adminEmail = null;

            if (authenticatedWho == null)
            {
                throw new ArgumentNullException("AuthenticatedWho", "AuthenticatedWho object cannot be null.");
            }

            if (streamId == null ||
                streamId.Trim().Length == 0)
            {
                throw new ArgumentNullException("StreamId", "StreamId cannot be null or blank.");
            }

            if (name == null ||
                name.Trim().Length == 0)
            {
                throw new ArgumentNullException("Name", "Name cannot be null or blank.");
            }

            if (socialServiceRequest == null)
            {
                throw new ArgumentNullException("SocialServiceRequest", "SocialServiceRequest object cannot be null.");
            }

            // Grab the values necessary to search for names in chatter
            chatterBaseUrl = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_CHATTER_BASE_URL, socialServiceRequest.configurationValues, true);
            adminEmail = ValueUtils.GetContentValue(SERVICE_VALUE_ADMIN_EMAIL, socialServiceRequest.configurationValues, true);

            // We enclose the request in a for loop to handle http errors
            for (int i = 0; i < SalesforceHttpUtils.MAXIMUM_RETRIES; i++)
            {
                try
                {
                    // Create a new client object
                    httpClient = SalesforceHttpUtils.CreateHttpClient(SalesforceHttpUtils.GetAuthenticationDetails(authenticatedWho.Token).Token);

                    // Create the end point url for the user search
                    endpointUrl = chatterBaseUrl + CHATTER_URI_PART_API_VERSION + String.Format(CHATTER_URI_PART_AUTOCOMPLETE, name.ToLower());

                    // Send the request to chatter
                    httpResponseMessage = await httpClient.GetAsync(endpointUrl);

                    if (httpResponseMessage.IsSuccessStatusCode == true)
                    {
                        // Parse the response data
                        chatterAutoCompleteResponse = await httpResponseMessage.Content.ReadAsAsync<ChatterAutoCompleteResponse>();

                        // Grab the mentioned users from the auto complete response
                        mentionedUsers = SalesforceSocialSingleton.GetInstance().ChatterUserInfoToMentionedUserAPI(chatterAutoCompleteResponse.Users);
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
                    HttpUtils.CleanUpHttp(httpClient, null, httpResponseMessage);
                }
            }

            return mentionedUsers;
        }

        private String CreateSalesforceAuthenticationToken(String sessionId, String sessionUrl)
        {
            return "Salesforce:" + sessionId + "||" + sessionUrl;
        }
    }
}