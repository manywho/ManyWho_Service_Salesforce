using System;
using System.Collections.Generic;
using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Utils;
using ManyWho.Flow.SDK.Security;
using ManyWho.Flow.SDK.Draw.Elements.Group;
using ManyWho.Flow.SDK.Run;
using ManyWho.Flow.SDK.Run.Elements.Config;
using ManyWho.Flow.SDK.Run.Elements.Type;
using ManyWho.Service.Salesforce.Utils;
using ManyWho.Service.Salesforce.Models.Rest;
using ManyWho.Service.Salesforce.Models.Rest.Enums;
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

namespace ManyWho.Service.Salesforce.Singletons
{
    public class SalesforceInvokeSingleton
    {
        private static SalesforceInvokeSingleton salesforceInvokeSingleton;

        private SalesforceInvokeSingleton()
        {

        }

        public static SalesforceInvokeSingleton GetInstance()
        {
            if (salesforceInvokeSingleton == null)
            {
                salesforceInvokeSingleton = new SalesforceInvokeSingleton();
            }

            return salesforceInvokeSingleton;
        }

        public ServiceResponseAPI InvokeNotifyUsers(INotifier notifier, IAuthenticatedWho authenticatedWho, ServiceRequestAPI serviceRequest)
        {
            ChatterPostedMessage chatterPostedMessage = null;
            ChatterNewMessageSegment chatterNewMessageSegment = null;
            ServiceResponseAPI serviceResponse = null;
            SforceService sforceService = null;
            String groupAuthenticationToken = null;
            String authenticationUrl = null;
            String username = null;
            String password = null;
            String securityToken = null;
            String chatterBaseUrl = null;
            String adminEmail = null;
            String endpointUrl = null;
            String message = null;

            // Grab the configuration values from the service request
            authenticationUrl = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_AUTHENTICATION_URL, serviceRequest.configurationValues, true);
            username = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_USERNAME, serviceRequest.configurationValues, true);
            password = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_PASSWORD, serviceRequest.configurationValues, true);
            securityToken = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_SECURITY_TOKEN, serviceRequest.configurationValues, false);
            chatterBaseUrl = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_CHATTER_BASE_URL, serviceRequest.configurationValues, true);
            adminEmail = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_ADMIN_EMAIL, serviceRequest.configurationValues, true);

            if (serviceRequest.authorization != null)
            {
                Boolean postMade = false;

                // Get the message from the inputs
                message = ValueUtils.GetContentValue("Post", serviceRequest.inputs, true);

                // Check to see if we have any group authorization - if so, we post to those groups
                if (serviceRequest.authorization.groups != null &&
                    serviceRequest.authorization.groups.Count > 0)
                {
                    foreach (GroupAuthorizationGroupAPI groupAuthorization in serviceRequest.authorization.groups)
                    {
                        // For group posts, we post as the admin, not as the user - as it's very likely the user does not have permissions to post
                        if (groupAuthenticationToken == null ||
                            groupAuthenticationToken.Trim().Length == 0)
                        {
                            // Login as the API user
                            sforceService = SalesforceDataSingleton.GetInstance().Login(authenticatedWho, serviceRequest.configurationValues, true, false);

                            // Get the session id out as we'll use that for the oauth login
                            groupAuthenticationToken = sforceService.SessionHeaderValue.sessionId;
                        }

                        // Create the endpoint url for the group
                        endpointUrl = chatterBaseUrl + SalesforceServiceSingleton.CHATTER_URI_PART_API_VERSION + String.Format(SalesforceServiceSingleton.CHATTER_URI_PART_POSTS, groupAuthorization.authenticationId);

                        // Create a new chatter post
                        chatterPostedMessage = new ChatterPostedMessage();
                        chatterPostedMessage.Body = new ChatterNewMessageBody();
                        chatterPostedMessage.Body.MessageSegments = new List<ChatterSegment>();

                        // Create a message segment for the actual post
                        chatterNewMessageSegment = new ChatterNewMessageSegment();
                        chatterNewMessageSegment.Type = ChatterMessageSegmentType.Text.ToString();
                        chatterNewMessageSegment.Text = message;

                        // Add the segment to the post
                        chatterPostedMessage.Body.MessageSegments.Add(chatterNewMessageSegment);

                        // Post the message synchronously
                        SalesforceSocialSingleton.GetInstance().PostNotification(notifier, authenticatedWho, groupAuthenticationToken, serviceRequest, endpointUrl, serviceRequest.joinPlayerUri, chatterPostedMessage);

                        // Set the flag that we did in fact make a post
                        postMade = true;
                    }
                }

                // Check to see if we have any user authorization - if so, we post to those users
                if (serviceRequest.authorization.users != null &&
                    serviceRequest.authorization.users.Count > 0)
                {
                    // Create the endpoint url
                    endpointUrl = chatterBaseUrl + SalesforceServiceSingleton.CHATTER_URI_PART_API_VERSION + String.Format(SalesforceServiceSingleton.CHATTER_URI_PART_MY_FEED, authenticatedWho.UserId);

                    // Create a new chatter post
                    chatterPostedMessage = new ChatterPostedMessage();
                    chatterPostedMessage.Body = new ChatterNewMessageBody();
                    chatterPostedMessage.Body.MessageSegments = new List<ChatterSegment>();

                    // Create a message segment for the actual post
                    chatterNewMessageSegment = new ChatterNewMessageSegment();
                    chatterNewMessageSegment.Type = ChatterMessageSegmentType.Text.ToString();
                    chatterNewMessageSegment.Text = message;

                    // Add the segment to the post
                    chatterPostedMessage.Body.MessageSegments.Add(chatterNewMessageSegment);

                    // Rather than posting to each user, we do a joint post to all of the users that need to be notified
                    foreach (GroupAuthorizationUserAPI userAuthorization in serviceRequest.authorization.users)
                    {
                        ChatterMentionsSegment chatterMentionsSegment = null;

                        chatterMentionsSegment = new ChatterMentionsSegment();
                        chatterMentionsSegment.Id = userAuthorization.authenticationId;
                        chatterMentionsSegment.Type = ChatterMessageSegmentType.Mention.ToString();

                        // Add the user to the @mention
                        chatterPostedMessage.Body.MessageSegments.Add(chatterMentionsSegment);
                    }

                    // Post the message synchronously
                    SalesforceSocialSingleton.GetInstance().PostNotification(notifier, authenticatedWho, serviceRequest, endpointUrl, serviceRequest.joinPlayerUri, chatterPostedMessage);

                    // Set the flag that we did in fact make a post
                    postMade = true;
                }

                if (postMade == false)
                {
                    // Alert the admin that no message was sent
                    ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_WARNING, "The service request does not have anything in the authorization context, so there's no one to notify.");
                }
            }
            else
            {
                // Alert the admin that no one is in the authorization context
                ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_WARNING, "The service request does not have an authorization context, so there's no one to notify.");
            }

            // Construct the service response
            serviceResponse = new ServiceResponseAPI();
            serviceResponse.invokeType = ManyWhoConstants.INVOKE_TYPE_FORWARD;
            serviceResponse.token = serviceRequest.token;
            serviceResponse.outputs = null;

            return serviceResponse;
        }

        public ServiceResponseAPI InvokeCreateTask(INotifier notifier, IAuthenticatedWho authenticatedWho, ServiceRequestAPI serviceRequest)
        {
            List<ObjectDataTypePropertyAPI> objectDataTypeProperties = null;
            ServiceResponseAPI serviceResponse = null;
            DateTime whenDate = DateTime.Now;
            List<ObjectAPI> taskObjects = null;
            ObjectAPI taskObject = null;
            String authenticationUrl = null;
            String username = null;
            String password = null;
            String securityToken = null;
            String adminEmail = null;
            String when = null;
            String description = null;
            String priority = null;
            String status = null;
            String subject = null;
            String taskId = null;

            // Grab the configuration values from the service request
            authenticationUrl = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_AUTHENTICATION_URL, serviceRequest.configurationValues, true);
            username = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_USERNAME, serviceRequest.configurationValues, true);
            password = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_PASSWORD, serviceRequest.configurationValues, true);
            securityToken = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_SECURITY_TOKEN, serviceRequest.configurationValues, false);
            adminEmail = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_ADMIN_EMAIL, serviceRequest.configurationValues, true);

            if (serviceRequest.authorization != null)
            {
                // Get the message from the inputs
                when = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_INPUT_WHEN, serviceRequest.inputs, true);
                description = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_INPUT_DESCRIPTION, serviceRequest.inputs, true);
                priority = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_INPUT_PRIORITY, serviceRequest.inputs, true);
                status = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_INPUT_STATUS, serviceRequest.inputs, true);
                subject = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_INPUT_SUBJECT, serviceRequest.inputs, true);

                // Get the when date for the provided command
                whenDate = DateUtils.CreateDateFromWhenCommand(notifier, authenticatedWho, when, adminEmail);

                // Add the link to the flow in the description
                description += "  Link to Flow: " + serviceRequest.joinPlayerUri;

                // Create a task object to save back to the system
                taskObject = new ObjectAPI();
                taskObject.developerName = "Task";
                taskObject.properties = new List<PropertyAPI>();
                taskObject.properties.Add(new PropertyAPI() { developerName = "ActivityDate", contentValue = whenDate.ToUniversalTime().ToString("yyyy'-'MM'-'dd") });
                taskObject.properties.Add(new PropertyAPI() { developerName = "Description", contentValue = description });
                taskObject.properties.Add(new PropertyAPI() { developerName = "Priority", contentValue = priority });
                taskObject.properties.Add(new PropertyAPI() { developerName = "Status", contentValue = status });
                taskObject.properties.Add(new PropertyAPI() { developerName = "Subject", contentValue = subject });
                taskObject.properties.Add(new PropertyAPI() { developerName = "IsClosed", contentValue = "false" });
                taskObject.properties.Add(new PropertyAPI() { developerName = "IsArchived", contentValue = "false" });
                taskObject.properties.Add(new PropertyAPI() { developerName = "IsRecurrence", contentValue = "false" });
                taskObject.properties.Add(new PropertyAPI() { developerName = "IsReminderSet", contentValue = "false" });
                taskObject.properties.Add(new PropertyAPI() { developerName = "IsVisibleInSelfService", contentValue = "false" });

                // Add the object to the list of objects to save
                taskObjects = new List<ObjectAPI>();
                taskObjects.Add(taskObject);

                // Create the object data type properties for this object so the system knows what we're selecting
                objectDataTypeProperties = new List<ObjectDataTypePropertyAPI>();
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "ActivityDateTime" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "Description" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "Priority" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "Status" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "Subject" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "IsClosed" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "IsArchived" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "IsRecurrence" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "IsReminderSet" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "IsVisibleInSelfService" });

                // Save the task object to salesforce
                taskObjects = SalesforceDataSingleton.GetInstance().Save(notifier, authenticatedWho, serviceRequest.configurationValues, objectDataTypeProperties, taskObjects);

                // Check to see if anything came back as part of the save - it should unless there was a fault
                if (taskObjects != null &&
                    taskObjects.Count > 0)
                {
                    // Grab the first object from the returned task objects
                    taskObject = taskObjects[0];

                    // Grab the id from that object - this needs to be returned in our outputs
                    taskId = taskObject.externalId;
                }
                else
                {
                    // If we didn't get any objects back, we need to throw an error
                    String errorMessage = "Task could not be created for an unknown reason.";

                    ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_FAULT, errorMessage);

                    throw new ArgumentNullException("BadRequest", errorMessage);
                }

            }
            else
            {
                // Alert the admin that no one is in the authorization context
                ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_WARNING, "The service request does not have an authorization context, so there's no one to notify.");
            }

            // Construct the service response
            serviceResponse = new ServiceResponseAPI();
            serviceResponse.invokeType = ManyWhoConstants.INVOKE_TYPE_FORWARD;
            serviceResponse.token = serviceRequest.token;
            serviceResponse.outputs = new List<EngineValueAPI>();
            serviceResponse.outputs.Add(new EngineValueAPI() { contentType = ManyWhoConstants.CONTENT_TYPE_STRING, contentValue = taskId, developerName = SalesforceServiceSingleton.SERVICE_OUTPUT_ID });

            return serviceResponse;
        }

        public ServiceResponseAPI InvokeCreateEvent(INotifier notifier, IAuthenticatedWho authenticatedWho, ServiceRequestAPI serviceRequest)
        {
            List<ObjectDataTypePropertyAPI> objectDataTypeProperties = null;
            ServiceResponseAPI serviceResponse = null;
            DateTime whenDate = DateTime.Now;
            List<ObjectAPI> eventObjects = null;
            ObjectAPI eventObject = null;
            String authenticationUrl = null;
            String username = null;
            String password = null;
            String securityToken = null;
            String adminEmail = null;
            String when = null;
            String duration = null;
            String description = null;
            String subject = null;
            String eventId = null;

            // Grab the configuration values from the service request
            authenticationUrl = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_AUTHENTICATION_URL, serviceRequest.configurationValues, true);
            username = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_USERNAME, serviceRequest.configurationValues, true);
            password = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_PASSWORD, serviceRequest.configurationValues, true);
            securityToken = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_SECURITY_TOKEN, serviceRequest.configurationValues, false);
            adminEmail = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_ADMIN_EMAIL, serviceRequest.configurationValues, true);

            if (serviceRequest.authorization != null)
            {
                // Get the message from the inputs
                when = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_INPUT_WHEN, serviceRequest.inputs, true);
                duration = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_INPUT_DURATION, serviceRequest.inputs, true);
                description = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_INPUT_DESCRIPTION, serviceRequest.inputs, true);
                subject = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_INPUT_SUBJECT, serviceRequest.inputs, true);

                // Get the when date for the provided command
                whenDate = DateUtils.CreateDateFromWhenCommand(notifier, authenticatedWho, when, adminEmail);
                // Set the calendar event for a day in the week at 10am
                whenDate = DateUtils.GetDayInWeek(whenDate, 10);

                // Add the link to the flow in the description
                description += "  Link to Flow: " + serviceRequest.joinPlayerUri;

                // Create a event object to save back to the system
                eventObject = new ObjectAPI();
                eventObject.developerName = "Event";
                eventObject.properties = new List<PropertyAPI>();
                eventObject.properties.Add(new PropertyAPI() { developerName = "ActivityDateTime", contentValue = whenDate.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffZ") });
                eventObject.properties.Add(new PropertyAPI() { developerName = "Description", contentValue = description });
                eventObject.properties.Add(new PropertyAPI() { developerName = "DurationInMinutes", contentValue = duration });
                eventObject.properties.Add(new PropertyAPI() { developerName = "Subject", contentValue = subject });
                eventObject.properties.Add(new PropertyAPI() { developerName = "IsAllDayEvent", contentValue = "false" });
                eventObject.properties.Add(new PropertyAPI() { developerName = "IsArchived", contentValue = "false" });
                eventObject.properties.Add(new PropertyAPI() { developerName = "IsChild", contentValue = "false" });
                eventObject.properties.Add(new PropertyAPI() { developerName = "IsGroupEvent", contentValue = "false" });
                eventObject.properties.Add(new PropertyAPI() { developerName = "IsPrivate", contentValue = "false" });
                eventObject.properties.Add(new PropertyAPI() { developerName = "IsRecurrence", contentValue = "false" });
                eventObject.properties.Add(new PropertyAPI() { developerName = "IsReminderSet", contentValue = "false" });
                eventObject.properties.Add(new PropertyAPI() { developerName = "IsVisibleInSelfService", contentValue = "false" });

                // Add the object to the list of objects to save
                eventObjects = new List<ObjectAPI>();
                eventObjects.Add(eventObject);

                // Create the object data type properties for this object so the system knows what we're selecting
                objectDataTypeProperties = new List<ObjectDataTypePropertyAPI>();
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "ActivityDateTime" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "Description" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "DurationInMinutes" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "Subject" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "IsAllDayEvent" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "IsArchived" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "IsChild" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "IsGroupEvent" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "IsPrivate" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "IsRecurrence" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "IsReminderSet" });
                objectDataTypeProperties.Add(new ObjectDataTypePropertyAPI() { developerName = "IsVisibleInSelfService" });

                // Save the event object to salesforce
                eventObjects = SalesforceDataSingleton.GetInstance().Save(notifier, authenticatedWho, serviceRequest.configurationValues, objectDataTypeProperties, eventObjects);

                // Check to see if anything came back as part of the save - it should unless there was a fault
                if (eventObjects != null &&
                    eventObjects.Count > 0)
                {
                    // Grab the first object from the returned event objects
                    eventObject = eventObjects[0];

                    // Grab the id from that object - this needs to be returned in our outputs
                    eventId = eventObject.externalId;
                }
                else
                {
                    // If we didn't get any objects back, we need to throw an error
                    String errorMessage = "Event could not be created for an unknown reason.";

                    ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_FAULT, errorMessage);

                    throw new ArgumentNullException("BadRequest", errorMessage);
                }
            }
            else
            {
                // Alert the admin that no one is in the authorization context
                ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_WARNING, "The service request does not have an authorization context, so there's no one to notify.");
            }

            // Construct the service response
            serviceResponse = new ServiceResponseAPI();
            serviceResponse.invokeType = ManyWhoConstants.INVOKE_TYPE_FORWARD;
            serviceResponse.token = serviceRequest.token;
            serviceResponse.outputs = new List<EngineValueAPI>();
            serviceResponse.outputs.Add(new EngineValueAPI() { contentType = ManyWhoConstants.CONTENT_TYPE_STRING, contentValue = eventId, developerName = SalesforceServiceSingleton.SERVICE_OUTPUT_ID });

            return serviceResponse;
        }

        //public ServiceResponseAPI InvokeCreateTask(ServiceRequestAPI serviceRequest)
        //{
        //    ServiceResponseAPI serviceResponse = null;
        //    //List<ValueAPI> outputs = null;
        //    //Dictionary<String, SalesforceType> outputFields;
        //    //String authenticationUrl = null;
        //    //String username = null;
        //    //String password = null;
        //    //String securityToken = null;
        //    //String id = null;

        //    //authenticationUrl = UtilsSingleton.GetInstance().GetConfigurationValue("AuthenticationUrl", serviceRequest.ConfigurationValues);
        //    //username = UtilsSingleton.GetInstance().GetConfigurationValue("Username", serviceRequest.ConfigurationValues);
        //    //password = UtilsSingleton.GetInstance().GetConfigurationValue("Password", serviceRequest.ConfigurationValues);
        //    //securityToken = UtilsSingleton.GetInstance().GetConfigurationValue("SecurityToken", serviceRequest.ConfigurationValues);

        //    //// TODO: this is a bit sloppy - we should not rely on the service request to get the right inputs
        //    //id = DataUtilsSingleton.GetInstance().Insert(authenticationUrl, username, password, securityToken, "TASK", serviceRequest.Inputs);

        //    //// Create the output fields
        //    //outputFields = new Dictionary<String, SalesforceType>();

        //    //// These are the output only fields
        //    ////outputFields.Add("ActivityDate", SalesforceType.Date);
        //    //outputFields.Add("IsClosed", SalesforceType.Boolean);
        //    //outputFields.Add("OwnerId", SalesforceType.String);
        //    //outputFields.Add("WhoId", SalesforceType.String);
        //    //outputFields.Add("WhatId", SalesforceType.String);

        //    //// These are the input fields that double as outputs
        //    //outputFields.Add("Description", SalesforceType.String);
        //    //outputFields.Add("Subject", SalesforceType.String);
        //    //outputFields.Add("Priority", SalesforceType.String);
        //    //outputFields.Add("Status", SalesforceType.String);

        //    //// Requiry the system to get the outputs from the insert
        //    //outputs = DataUtilsSingleton.GetInstance().GetOutputs(authenticationUrl, username, password, securityToken, "TASK", id, outputFields);

        //    //// Register this task with the timer check - this will tell the system to keep calling back
        //    //TimerUtilsSingleton.GetInstance().AddTimerCheckEntry(serviceRequest.Token, authenticationUrl, username, password, securityToken, "TASK", id, outputFields);

        //    //// Construct the service response
        //    //serviceResponse = new ServiceResponseAPI();
        //    //serviceResponse.EngineCommand = "WAIT";
        //    //serviceResponse.Token = serviceRequest.Token;
        //    //serviceResponse.Outputs = outputs;

        //    return serviceResponse;
        //}

        public String GetContentValueForDeveloperName(String developerName, List<EngineValueAPI> inputs)
        {
            String value = null;

            foreach (EngineValueAPI input in inputs)
            {
                if (input.developerName == developerName)
                {
                    value = input.contentValue;
                    break;
                }
            }

            return value;
        }
    }
}
