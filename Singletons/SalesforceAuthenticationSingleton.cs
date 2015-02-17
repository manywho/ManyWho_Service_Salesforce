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
using ManyWho.Service.Salesforce.Utils;
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

namespace ManyWho.Service.Salesforce.Singletons
{
    public class SalesforceAuthenticationSingleton
    {
        private static SalesforceAuthenticationSingleton salesforceAuthenticationSingleton;

        // Useful constants for sobject properties
        public const String SALESFORCE_SOBJECT_USER_ID = "sf:Id";
        public const String SALESFORCE_SOBJECT_MANAGER_ID = "sf:ManagerId";
        public const String SALESFORCE_SOBJECT_USERNAME = "sf:Username";
        public const String SALESFORCE_SOBJECT_EMAIL = "sf:Email";
        public const String SALESFORCE_SOBJECT_FIRST_NAME = "sf:FirstName";
        public const String SALESFORCE_SOBJECT_LAST_NAME = "sf:LastName";

        private SalesforceAuthenticationSingleton()
        {

        }

        public static SalesforceAuthenticationSingleton GetInstance()
        {
            if (salesforceAuthenticationSingleton == null)
            {
                salesforceAuthenticationSingleton = new SalesforceAuthenticationSingleton();
            }

            return salesforceAuthenticationSingleton;
        }

        public Int32 GetAuthorizationContextCount(INotifier notifier, IAuthenticatedWho authenticatedWho, String authenticationUrl, String username, String password, String securityToken, AuthorizationAPI authorization)
        {
            SforceService sforceService = null;
            Int32 authorizationContextCount = 0;

            if (authenticatedWho == null)
            {
                throw new ArgumentNullException("AuthenticatedWho", "The AuthenticatedWho object cannot be null.");
            }

            if (authorization == null)
            {
                throw new ArgumentNullException("Authorization", "The Authorization object cannot be null.");
            }

            if (authorization.users != null &&
                authorization.users.Count > 0)
            {
                throw new ArgumentNullException("Authorization.Users", "The Service does not currently support any authorization context other than Group for Voting.");
            }

            if (authorization.groups != null &&
                authorization.groups.Count > 1)
            {
                throw new ArgumentNullException("Authorization.Groups", "The Service does not currently support more than one Group in the authorization context for Voting.");
            }

            // Login to the service
            sforceService = SalesforceDataSingleton.GetInstance().Login(authenticationUrl, username, password, securityToken);

            if (authorization.groups != null &&
                authorization.groups.Count > 0)
            {
                // We use the utils response object as it makes it a little easier to manage conditions that fail
                AuthenticationUtilsResponse authenticationUtilsResponse = null;

                if (authorization.groups[0].attribute.Equals(SalesforceServiceSingleton.SERVICE_VALUE_MEMBERS, StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    // Check to see if the user is a member of the specified group
                    authenticationUtilsResponse = this.GroupMember(sforceService, authorization.groups[0].authenticationId, authenticatedWho.UserId, true);
                }
                else if (authorization.groups[0].attribute.Equals(SalesforceServiceSingleton.SERVICE_VALUE_OWNERS, StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    // Check to see if the user is an owner of the specified group
                    authenticationUtilsResponse = this.GroupOwner(sforceService, authorization.groups[0].authenticationId, authenticatedWho.UserId, true);
                }
                else
                {
                    // We don't support the attribute that's being provided
                    String errorMessage = "The Group attribute is not supported: " + authorization.groups[0].attribute;

                    ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_FAULT, errorMessage);

                    throw new ArgumentNullException("BadRequest", errorMessage);
                }

                // Get the count out from the result
                authorizationContextCount = authenticationUtilsResponse.Count;
            }
            else
            {
                // Get the count of all users in the org
                authorizationContextCount = this.OrgUserCount(sforceService).Count;
            }

            return authorizationContextCount;
        }

        public List<ObjectAPI> GetUserInAuthorizationContext(INotifier notifier, IAuthenticatedWho authenticatedWho, String alertEmail, String authenticationUrl, String chatterBaseUrl, String username, String password, String securityToken, String clientId, Boolean loginUsingOAuth2, ObjectDataRequestAPI objectDataRequest)
        {
            SforceService sforceService = null;
            ObjectAPI objectAPI = null;
            List<ObjectAPI> objectAPIs = null;

            // Login to the service
            sforceService = SalesforceDataSingleton.GetInstance().Login(authenticationUrl, username, password, securityToken);

            // We start by checking if the request is based on public users. Despite this seeming a little odd, it does give the plugin the opportunity
            // to assign information to the public user that may be helpful for other operations - e.g. anoymous collaboration.
            if (objectDataRequest.authorization.globalAuthenticationType.Equals(ManyWhoConstants.GROUP_AUTHORIZATION_GLOBAL_AUTHENTICATION_TYPE_PUBIC, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                // Create the standard user object
                objectAPI = CreateUserObject(sforceService);

                // Apply some default settings
                objectAPI.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_USER_ID, ManyWhoConstants.AUTHENTICATED_USER_PUBLIC_USER_ID));
                objectAPI.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_USERNAME, ManyWhoConstants.AUTHENTICATED_USER_PUBLIC_USER_ID));
                objectAPI.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_EMAIL, null));
                objectAPI.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_FIRST_NAME, ManyWhoConstants.AUTHENTICATED_USER_PUBLIC_USER_ID));
                objectAPI.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_LAST_NAME, ManyWhoConstants.AUTHENTICATED_USER_PUBLIC_USER_ID));

                // Tell ManyWho the user is authorized to proceed
                objectAPI.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_STATUS, ManyWhoConstants.AUTHORIZATION_STATUS_AUTHORIZED));
            }
            else if (objectDataRequest.authorization.globalAuthenticationType.Equals(ManyWhoConstants.GROUP_AUTHORIZATION_GLOBAL_AUTHENTICATION_TYPE_ALL_USERS, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                // Only bother doing the lookup if we have an actual user id that's valid for this type of operation (e.g. not public)
                if (authenticatedWho.UserId != null &&
                    authenticatedWho.UserId.Equals(ManyWhoConstants.AUTHENTICATED_USER_PUBLIC_USER_ID, StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    // Check to see if the user is in fact a user in the org. We do this by checking the authenticated who object as this is the user actually
                    // requesting access
                    objectAPI = this.User(sforceService, authenticatedWho.UserId).UserObject;
                }
            }
            else if (objectDataRequest.authorization.globalAuthenticationType.Equals(ManyWhoConstants.GROUP_AUTHORIZATION_GLOBAL_AUTHENTICATION_TYPE_SPECIFIED, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                Boolean doMoreWork = true;

                // Specified permissions is a bit more complicated as we need to do a little more analysis depending on the configuration. We assume
                // the user is authenticated if any of the specified criteria evaluate to true. First we check to see if the author of the flow has
                // specified permissions based on specific user references (which is not recommended - but is supported).
                if (objectDataRequest.authorization.users != null &&
                    objectDataRequest.authorization.users.Count > 0)
                {
                    // Go through each of the specified users and attempt to match the currently authenticated user with the criteria
                    foreach (UserAPI user in objectDataRequest.authorization.users)
                    {
                        // First - check to see if the author explicitly decided this user should have access. This is the default setting if the
                        // attribute is null.
                        if (user.attribute == null ||
                            user.attribute.Trim().Length == 0 ||
                            user.attribute.Equals(SalesforceServiceSingleton.SERVICE_VALUE_USER, StringComparison.InvariantCultureIgnoreCase) == true)
                        {
                            // This is a hard-coded user permission - so we simply check if this user matches the current user
                            if (user.authenticationId.Equals(authenticatedWho.UserId, StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                // Get the user object from salesforce (which may not exist)
                                objectAPI = this.User(sforceService, authenticatedWho.UserId).UserObject;

                                // This is our user - no need to do anything else as the lookup will now determine if they have access
                                doMoreWork = false;
                                break;
                            }
                        }
                        else
                        {
                            // We use the utils response object as it makes it a little easier to manage conditions that fail
                            AuthenticationUtilsResponse authenticationUtilsResponse = null;
                            String userAuthenticationId = null;

                            if (user.runningUser == true)
                            {
                                userAuthenticationId = objectDataRequest.authorization.runningAuthenticationId;
                            }
                            else
                            {
                                userAuthenticationId = user.authenticationId;
                            }

                            // We are looking at a particular attribute of the user and therefore need to query the system based on that attribute
                            if (user.attribute.Equals(SalesforceServiceSingleton.SERVICE_VALUE_COLLEAGUES, StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                // Check to see if the current user is a colleague of the specified user
                                authenticationUtilsResponse = this.Colleague(sforceService, userAuthenticationId, authenticatedWho.UserId);
                            }
                            else if (user.attribute.Equals(SalesforceServiceSingleton.SERVICE_VALUE_DELEGATES, StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                // Check to see if the current user is a delegate of the specified user
                                authenticationUtilsResponse = this.Delegate(sforceService, userAuthenticationId, authenticatedWho.UserId);
                            }
                            else if (user.attribute.Equals(SalesforceServiceSingleton.SERVICE_VALUE_DIRECTS, StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                // Check to see if the current user is a direct of the specified user
                                authenticationUtilsResponse = this.Direct(sforceService, userAuthenticationId, authenticatedWho.UserId);
                            }
                            else if (user.attribute.Equals(SalesforceServiceSingleton.SERVICE_VALUE_FOLLOWERS, StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                // Check to see if the current user is a follower of the specified user
                                authenticationUtilsResponse = this.Follower(sforceService, notifier, authenticatedWho, alertEmail, chatterBaseUrl, userAuthenticationId);
                            }
                            else if (user.attribute.Equals(SalesforceServiceSingleton.SERVICE_VALUE_FOLLOWING, StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                // Check to see if the current user is being followed by the specified user
                                authenticationUtilsResponse = this.Following(sforceService, notifier, authenticatedWho, alertEmail, chatterBaseUrl, userAuthenticationId);
                            }
                            else if (user.attribute.Equals(SalesforceServiceSingleton.SERVICE_VALUE_MANAGERS, StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                // Check to see if the current user is a direct of the specified user
                                authenticationUtilsResponse = this.Manager(sforceService, userAuthenticationId, authenticatedWho.UserId);
                            }
                            else
                            {
                                // We don't support the attribute that's being provided
                                String errorMessage = "The user attribute is not supported: " + user.attribute;

                                ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_FAULT, errorMessage);

                                throw new ArgumentNullException("BadRequest", errorMessage);
                            }

                            // If the user is in this context, then we don't need to do anything else
                            if (authenticationUtilsResponse.IsInContext == true)
                            {
                                // Grab the user object
                                objectAPI = authenticationUtilsResponse.UserObject;

                                // Break out of the user validation
                                doMoreWork = false;
                                break;
                            }
                        }
                    }
                }

                // No need to do this next bit if we already know we're authorized
                if (doMoreWork == true)
                {
                    // If the user has not been matched by the user configuration, we need to move into the groups to see if they're included
                    // in any of the specified groups - if any group configuration has been provided
                    if (objectDataRequest.authorization.groups != null &&
                        objectDataRequest.authorization.groups.Count > 0)
                    {
                        // Go through each group in turn
                        foreach (GroupAPI group in objectDataRequest.authorization.groups)
                        {
                            // We use the utils response object as it makes it a little easier to manage conditions that fail
                            AuthenticationUtilsResponse authenticationUtilsResponse = null;

                            if (group.attribute.Equals(SalesforceServiceSingleton.SERVICE_VALUE_MEMBERS, StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                // Check to see if the user is a member of the specified group
                                authenticationUtilsResponse = this.GroupMember(sforceService, group.authenticationId, authenticatedWho.UserId, false);
                            }
                            else if (group.attribute.Equals(SalesforceServiceSingleton.SERVICE_VALUE_OWNERS, StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                // Check to see if the user is an owner of the specified group
                                authenticationUtilsResponse = this.GroupOwner(sforceService, group.authenticationId, authenticatedWho.UserId, false);
                            }
                            else
                            {
                                // We don't support the attribute that's being provided
                                String errorMessage = "The group attribute is not supported: " + group.attribute;

                                ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_FAULT, errorMessage);

                                throw new ArgumentNullException("BadRequest", errorMessage);
                            }

                            // If the user is in this context, then we don't need to do anything else
                            if (authenticationUtilsResponse.IsInContext == true)
                            {
                                // Grab the user object
                                objectAPI = authenticationUtilsResponse.UserObject;

                                // Break out of the user validation
                                doMoreWork = false;
                                break;
                            }
                        }
                    }
                }
            }

            // If we're here and the user object is null, then they did not manage to authenticate
            if (objectAPI == null)
            {
                // Create the standard user object
                objectAPI = CreateUserObject(sforceService);

                // Apply some default settings
                objectAPI.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_USER_ID, ManyWhoConstants.AUTHENTICATED_USER_PUBLIC_USER_ID));
                objectAPI.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_USERNAME, ManyWhoConstants.AUTHENTICATED_USER_PUBLIC_USER_ID));
                objectAPI.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_EMAIL, null));
                objectAPI.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_FIRST_NAME, ManyWhoConstants.AUTHENTICATED_USER_PUBLIC_USER_ID));
                objectAPI.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_LAST_NAME, ManyWhoConstants.AUTHENTICATED_USER_PUBLIC_USER_ID));

                // Tell ManyWho the user is not authorized to proceed
                objectAPI.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_STATUS, ManyWhoConstants.AUTHORIZATION_STATUS_NOT_AUTHORIZED));
            }

            // Finally, decide on the authentication mode
            if (loginUsingOAuth2 == true)
            {
                String loginUrl = "";

                if (String.IsNullOrWhiteSpace(authenticationUrl) == true)
                {
                    loginUrl = "https://login.salesforce.com";
                }
                else
                {
                    loginUrl = authenticationUrl;
                }

                loginUrl = String.Format(loginUrl + "/services/oauth2/authorize?response_type=code&client_id={0}", clientId);

                objectAPI.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_AUTHENTICATION_TYPE, ManyWhoConstants.AUTHENTICATION_TYPE_OAUTH2));
                objectAPI.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_LOGIN_URL, loginUrl));
            }
            else
            {
                objectAPI.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_AUTHENTICATION_TYPE, ManyWhoConstants.AUTHENTICATION_TYPE_USERNAME_PASSWORD));
            }

            // Create the list of objects to return and add our object
            objectAPIs = new List<ObjectAPI>();
            objectAPIs.Add(objectAPI);

            // Return the user in an object list
            return objectAPIs;
        }

        /// <summary>
        /// For user and group loads, the user has the option to provide the list of users contained in the system. We then have the option to populate all of the
        /// latest user information so ManyWho isn't storing data that's likely to change in the user management system.
        /// </summary>
        public ListFilterAPI CreateFilterFromProvidedObjectData(List<ObjectAPI> objectData, ListFilterAPI inboundListFilterAPI)
        {
            ListFilterAPI listFilterAPI = null;

            // Check to see if the caller has passed in objects - if they have - we'll filter the response by that list - using the attribute id as the filter
            if (objectData != null &&
                objectData.Count > 0 &&
                inboundListFilterAPI != null &&
                inboundListFilterAPI.filterByProvidedObjects == true)
            {
                listFilterAPI = new ListFilterAPI();
                listFilterAPI.comparisonType = ManyWhoConstants.LIST_FILTER_CONFIG_COMPARISON_TYPE_OR;
                listFilterAPI.where = new List<ListFilterWhereAPI>();

                foreach (ObjectAPI objectDataEntry in objectData)
                {
                    ListFilterWhereAPI listFilterWhere = null;

                    listFilterWhere = new ListFilterWhereAPI();
                    listFilterWhere.columnName = "Id";
                    listFilterWhere.criteriaType = ManyWhoConstants.CONTENT_VALUE_IMPLEMENTATION_CRITERIA_TYPE_EQUAL;

                    // We now need to find the id property from the incoming object
                    if (objectDataEntry.properties != null &&
                        objectDataEntry.properties.Count > 0)
                    {
                        // Go through each of the properties in the object to find the identifier
                        foreach (PropertyAPI objectDataEntryProperty in objectDataEntry.properties)
                        {
                            if (objectDataEntryProperty.developerName.Equals(ManyWhoConstants.AUTHENTICATION_OBJECT_AUTHENTICATION_ID, StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                listFilterWhere.value = objectDataEntryProperty.contentValue;
                                break;
                            }
                        }
                    }
                    else
                    {
                        throw new ArgumentNullException("BadRequest", "The incoming user object does not contain any properties.");
                    }

                    if (listFilterWhere.value == null ||
                        listFilterWhere.value.Trim().Length == 0)
                    {
                        throw new ArgumentNullException("BadRequest", "An attribute id could not be found for the user, which means the plugin will not be able to find the correct user.");
                    }

                    // Add this filter to the list
                    listFilterAPI.where.Add(listFilterWhere);
                }
            }

            return listFilterAPI;
        }

        /// <summary>
        /// Based on the reference group identifier, grab the complete list of user emails - though restrict the list to 100 for now so we
        /// don't accidentally send out a huge amount of spam to users.
        /// </summary>
        public List<String> GetGroupMemberEmails(INotifier notifier, SforceService sforceService, ServiceRequestAPI serviceRequestAPI, String referenceGroupId)
        {
            List<String> groupMemberEmails = null;
            QueryResult queryResult = null;
            String soql = null;
            String where = String.Empty;

            if (notifier == null)
            {
                throw new ArgumentNullException("Notifier", "The Notifier object cannot be null.");
            }

            if (sforceService == null)
            {
                throw new ArgumentNullException("SforceService", "The SforceService object cannot be null.");
            }

            if (serviceRequestAPI == null)
            {
                throw new ArgumentNullException("ServiceRequestAPI", "The ServiceRequestAPI object cannot be null.");
            }

            if (String.IsNullOrWhiteSpace(referenceGroupId) == true)
            {
                throw new ArgumentNullException("ReferenceGroupId", "The ReferenceGroupId cannot be null or blank.");
            }

            // Select from the group members to see if this user exists in the set
            soql = "SELECT MemberId FROM CollaborationGroupMember WHERE CollaborationGroupId = '" + referenceGroupId + "'";

            // Query salesforce to see if anything comes back
            queryResult = sforceService.query(soql);

            // Check to see if the query returned any results
            if (queryResult != null &&
                queryResult.records != null &&
                queryResult.records.Length > 0)
            {
                for (int i = 0; i < queryResult.records.Length; i++)
                {
                    // Get the identifier out of the record, we'll need this to get our user list
                    where += "Id = '" + queryResult.records[i].Any[0].InnerText + "' OR ";

                    if (i >= 25)
                    {
                        notifier.AddLogEntry("Query has returned too many users (max 25) - sending to the first 25.");
                        break;
                    }
                }

                // Trim the where clause back
                where = where.Substring(0, (where.Length - " OR ".Length));

                // Query salesforce again with this new where clause
                queryResult = sforceService.query("SELECT Email FROM User WHERE " + where);

                if (queryResult != null &&
                    queryResult.records != null &&
                    queryResult.records.Length > 0)
                {
                    groupMemberEmails = new List<String>();

                    // Now that we have the user, we need to get the properties from the object so we can map them to a manywho user
                    for (int j = 0; j < queryResult.records.Length; j++)
                    {
                        groupMemberEmails.Add(queryResult.records[j].Any[0].InnerText);
                    }
                }
            }

            return groupMemberEmails;
        }

        /// <summary>
        /// Check to see if this user exists in the system for the provided user id.
        /// </summary>
        private AuthenticationUtilsResponse User(SforceService sforceService, String thisUserId)
        {
            AuthenticationUtilsResponse authenticationUtilsResponse = null;
            String where = null;

            // Get the user based on this identifier
            where = "Id = '" + thisUserId + "'";

            // Execute the query and grab the user response as this is also our user
            authenticationUtilsResponse = new AuthenticationUtilsResponse();
            authenticationUtilsResponse.UserObject = this.ExecuteUserQuery(sforceService, where).UserObject;

            // If we have a user object then we can assume the user is in context
            if (authenticationUtilsResponse.UserObject != null)
            {
                authenticationUtilsResponse.IsInContext = true;
            }

            return authenticationUtilsResponse;
        }

        /// <summary>
        /// Check to see if this user is a colleague of the reference user id.
        /// </summary>
        private AuthenticationUtilsResponse Colleague(SforceService sforceService, String referenceUserId, String thisUserId)
        {
            AuthenticationUtilsResponse authenticationUtilsResponse = null;
            String where = null;
            String managerId = null;

            // Get the manager for the reference user id
            where = "Id = '" + referenceUserId + "'";

            // Grab the manager id from our helper
            managerId = this.ExecuteUserQuery(sforceService, where).ManagerId;

            //  Check to see if we found a manager id for this user
            if (managerId == null ||
                managerId.Trim().Length == 0)
            {
                // We didn't so the user is not in the colleague context
                authenticationUtilsResponse = new AuthenticationUtilsResponse();
                authenticationUtilsResponse.IsInContext = false;
            }
            else
            {
                // We have a manager, so we now need to see if this user has the same manager - and hence is a colleague
                where = "Id = '" + thisUserId + "' AND ManagerId = '" + managerId + "'";

                // Execute the query and grab the user response as this is also our user
                authenticationUtilsResponse = new AuthenticationUtilsResponse();
                authenticationUtilsResponse.UserObject = this.ExecuteUserQuery(sforceService, where).UserObject;

                // If we have a user object then we can assume the user is in context
                if (authenticationUtilsResponse.UserObject != null)
                {
                    authenticationUtilsResponse.IsInContext = true;
                }
            }

            return authenticationUtilsResponse;
        }

        /// <summary>
        /// Check to see if this user is a direct of the reference user id.
        /// </summary>
        private AuthenticationUtilsResponse Direct(SforceService sforceService, String referenceUserId, String thisUserId)
        {
            AuthenticationUtilsResponse authenticationUtilsResponse = null;
            String where = null;

            // Select from the users table to see if we have a user for this id and with a manager of the reference id
            where = "Id = '" + thisUserId + "' AND ManagerId = '" + referenceUserId + "'";

            // Create a new authentication utils response object to house the results
            authenticationUtilsResponse = new AuthenticationUtilsResponse();

            // We assign the user object as this query will return the correct user also - so we can keep that without requerying salesforce
            authenticationUtilsResponse.UserObject = this.ExecuteUserQuery(sforceService, where).UserObject;

            // If we have a user object, then we can assume the user has authenticated
            if (authenticationUtilsResponse.UserObject != null)
            {
                authenticationUtilsResponse.IsInContext = true;
            }

            return authenticationUtilsResponse;
        }

        /// <summary>
        /// Check to see if this user is being followed of the reference user id.
        /// </summary>
        private AuthenticationUtilsResponse Following(SforceService sforceService, INotifier notifier, IAuthenticatedWho authenticatedWho, String alertEmail, String chatterBaseUrl, String referenceUserId)
        {
            AuthenticationUtilsResponse authenticationUtilsResponse = null;
            HttpClient httpClient = null;
            HttpResponseMessage httpResponseMessage = null;
            ChatterFollowingResponse followingUsersresponse = null;
            String endpointUrl = null;

            // Create a new authentication utils response object to house the results
            authenticationUtilsResponse = new AuthenticationUtilsResponse();

            // We enclose the request in a for loop to handle http errors
            for (int i = 0; i < SalesforceHttpUtils.MAXIMUM_RETRIES; i++)
            {
                try
                {
                    // Create a new client object
                    httpClient = SalesforceHttpUtils.CreateHttpClient(SalesforceHttpUtils.GetAuthenticationDetails(authenticatedWho.Token).Token);

                    // Create the endpoint url
                    endpointUrl = chatterBaseUrl + SalesforceServiceSingleton.CHATTER_URI_PART_API_VERSION + String.Format(SalesforceServiceSingleton.CHATTER_URI_PART_STREAM_FOLLOWERS, referenceUserId);

                    // TODO: Need to add paging support to this as it currently only sends back the first page of results
                    httpResponseMessage = httpClient.GetAsync(endpointUrl).Result;

                    // Check the status of the response and respond appropriately
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        followingUsersresponse = httpResponseMessage.Content.ReadAsAsync<ChatterFollowingResponse>().Result;

                        // Check to see if this user has any following
                        if (followingUsersresponse.Following != null &&
                            followingUsersresponse.Following.Count > 0)
                        {
                            // Go through the followers and see if any of them match the current user
                            foreach (ChatterFollowing chatterFollowing in followingUsersresponse.Following)
                            {
                                ChatterUserInfo chatterUserInfo = null;

                                // The following "thing" is in the subject
                                if (chatterFollowing.Subject != null)
                                {
                                    chatterUserInfo = chatterFollowing.Subject;

                                    // Check to see if this is our user
                                    if (chatterUserInfo.Id.Equals(authenticatedWho.UserId, StringComparison.InvariantCultureIgnoreCase) == true)
                                    {
                                        // This user is a follower, we need to get their full user object details
                                        authenticationUtilsResponse.UserObject = this.User(sforceService, authenticatedWho.UserId).UserObject;
                                        authenticationUtilsResponse.IsInContext = true;

                                        // We have our user, break out of the following list
                                        break;
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
                        BaseHttpUtils.HandleUnsuccessfulHttpResponseMessage(notifier, authenticatedWho, i, httpResponseMessage, endpointUrl);
                    }
                }
                catch (Exception exception)
                {
                    // Make sure we handle the exception properly
                    BaseHttpUtils.HandleHttpException(notifier, authenticatedWho, i, exception, endpointUrl);
                }
                finally
                {
                    // Clean up the objects from the request
                    HttpUtils.CleanUpHttp(httpClient, null, httpResponseMessage);
                }
            }

            // Finally, return the authentication response to the caller
            return authenticationUtilsResponse;
        }

        /// <summary>
        /// Check to see if this user is a follower of the reference user id.
        /// </summary>
        private AuthenticationUtilsResponse Follower(SforceService sforceService, INotifier notifier, IAuthenticatedWho authenticatedWho, String alertEmail, String chatterBaseUrl, String referenceUserId)
        {
            AuthenticationUtilsResponse authenticationUtilsResponse = null;
            HttpClient httpClient = null;
            HttpResponseMessage httpResponseMessage = null;
            ChatterFollowingResponse followingUsersresponse = null;
            String endpointUrl = null;

            // Create a new authentication utils response object to house the results
            authenticationUtilsResponse = new AuthenticationUtilsResponse();

            // We enclose the request in a for loop to handle http errors
            for (int i = 0; i < SalesforceHttpUtils.MAXIMUM_RETRIES; i++)
            {
                try
                {
                    // Create a new client object
                    httpClient = SalesforceHttpUtils.CreateHttpClient(SalesforceHttpUtils.GetAuthenticationDetails(authenticatedWho.Token).Token);

                    // Create the endpoint url
                    endpointUrl = chatterBaseUrl + SalesforceServiceSingleton.CHATTER_URI_PART_API_VERSION + String.Format(SalesforceServiceSingleton.CHATTER_URI_PART_STREAM_FOLLOWERS, referenceUserId);

                    // TODO: Need to add paging support to this as it currently only sends back the first page of results
                    httpResponseMessage = httpClient.GetAsync(endpointUrl).Result;

                    // Check the status of the response and respond appropriately
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        followingUsersresponse = httpResponseMessage.Content.ReadAsAsync<ChatterFollowingResponse>().Result;

                        // Check to see if this user has any following
                        if (followingUsersresponse.Following != null &&
                            followingUsersresponse.Following.Count > 0)
                        {
                            // Go through the followers and see if any of them match the current user
                            foreach (ChatterFollowing chatterFollowing in followingUsersresponse.Following)
                            {
                                ChatterUserInfo chatterUserInfo = null;

                                // The following "thing" is in the subject
                                if (chatterFollowing.Subject != null)
                                {
                                    chatterUserInfo = chatterFollowing.Subject;

                                    // Check to see if this is our user
                                    if (chatterUserInfo.Id.Equals(authenticatedWho.UserId, StringComparison.InvariantCultureIgnoreCase) == true)
                                    {
                                        // This user is a follower, we need to get their full user object details
                                        authenticationUtilsResponse.UserObject = this.User(sforceService, authenticatedWho.UserId).UserObject;
                                        authenticationUtilsResponse.IsInContext = true;

                                        // We have our user, break out of the following list
                                        break;
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
                        BaseHttpUtils.HandleUnsuccessfulHttpResponseMessage(notifier, authenticatedWho, i, httpResponseMessage, endpointUrl);
                    }
                }
                catch (Exception exception)
                {
                    // Make sure we handle the exception properly
                    BaseHttpUtils.HandleHttpException(notifier, authenticatedWho, i, exception, endpointUrl);
                }
                finally
                {
                    // Clean up the objects from the request
                    HttpUtils.CleanUpHttp(httpClient, null, httpResponseMessage);
                }
            }

            // Finally, return the authentication response to the caller
            return authenticationUtilsResponse;
        }

        /// <summary>
        /// Check to see if this user is a delegate of the reference user id.
        /// </summary>
        private AuthenticationUtilsResponse Delegate(SforceService sforceService, String referenceUserId, String thisUserId)
        {
            AuthenticationUtilsResponse authenticationUtilsResponse = null;
            String where = null;

            // Select from the users table to see if we have a user for the reference id and delegate approval authority for this user id
            where = "Id = '" + referenceUserId + "' AND DelegateApproverId = '" + thisUserId + "'";

            // Create a new authentication utils response object to house the results
            authenticationUtilsResponse = new AuthenticationUtilsResponse();

            // If the query returns results, we know the user is valid
            if (this.ExecuteUserQuery(sforceService, where) != null)
            {
                // Ths user is in the specified context
                authenticationUtilsResponse.IsInContext = true;

                // Now we query the system again, but this time with the query for the actual user
                where = "Id = '" + thisUserId + "'";

                // Grab the correct user object
                authenticationUtilsResponse.UserObject = this.ExecuteUserQuery(sforceService, where).UserObject;
            }

            return authenticationUtilsResponse;
        }

        /// <summary>
        /// Check to see if this user is a manager of the reference user id.
        /// </summary>
        private AuthenticationUtilsResponse Manager(SforceService sforceService, String referenceUserId, String thisUserId)
        {
            AuthenticationUtilsResponse authenticationUtilsResponse = null;
            String where = null;

            // Select from the users table to see if we have a user for this id and with a manager of the reference id
            where = "Id = '" + referenceUserId  + "' AND ManagerId = '" + thisUserId + "'";

            // Create a new authentication utils response object to house the results
            authenticationUtilsResponse = new AuthenticationUtilsResponse();

            // Check to see if the query returns and results - if it doesn then we know this user is a manager of the reference user
            if (this.ExecuteUserQuery(sforceService, where).UserObject != null)
            {
                // Set the flag to indicate that this user is in the context
                authenticationUtilsResponse.IsInContext = true;

                // Now we query the system again, but this time with the query for the actual user
                where = "Id = '" + thisUserId + "'";

                // Grab the correct user object
                authenticationUtilsResponse.UserObject = this.ExecuteUserQuery(sforceService, where).UserObject;
            }

            return authenticationUtilsResponse;
        }

        /// <summary>
        /// Check to see if this user is a member of the reference group id.
        /// </summary>
        private AuthenticationUtilsResponse GroupMember(SforceService sforceService, String referenceGroupId, String thisUserId, Boolean isUserCount)
        {
            AuthenticationUtilsResponse authenticationUtilsResponse = null;
            QueryResult queryResult = null;
            String soql = null;
            String where = null;

            // Check to see what type of group member lookup we're doing
            if (isUserCount == true)
            {
                // If we're counting the users, we don't want to filter by a specific user, we just want the count
                soql = "SELECT Count(CollaborationGroupId) FROM CollaborationGroupMember WHERE CollaborationGroupId = '" + referenceGroupId + "'";
            }
            else
            {
                // Select from the group members to see if this user exists in the set
                soql = "SELECT CollaborationGroupId FROM CollaborationGroupMember WHERE CollaborationGroupId = '" + referenceGroupId + "' AND MemberId = '" + thisUserId + "'";
            }

            // Create a new authentication utils response object to house the results
            authenticationUtilsResponse = new AuthenticationUtilsResponse();

            // Query salesforce to see if anything comes back
            queryResult = sforceService.query(soql);

            // Check to see if the query returned any results
            if (queryResult != null &&
                queryResult.records != null &&
                queryResult.records.Length > 0)
            {
                if (isUserCount == true)
                {
                    if (queryResult.records[0].Any != null &&
                        queryResult.records[0].Any.Length > 0)
                    {
                        // Just get the count out of the result
                        authenticationUtilsResponse.Count = Int32.Parse(queryResult.records[0].Any[0].InnerText);
                    }
                }
                else
                {
                    // If we have a result, then the user is in context
                    authenticationUtilsResponse.IsInContext = true;

                    // Now we query the system again, but this time with the query for the actual user
                    where = "Id = '" + thisUserId + "'";

                    // Grab the user object
                    authenticationUtilsResponse.UserObject = this.ExecuteUserQuery(sforceService, where).UserObject;
                }
            }

            return authenticationUtilsResponse;
        }

        /// <summary>
        /// Check to see if this user is the owner of the reference group id.
        /// </summary>
        private AuthenticationUtilsResponse GroupOwner(SforceService sforceService, String referenceGroupId, String thisUserId, Boolean isUserCount)
        {
            AuthenticationUtilsResponse authenticationUtilsResponse = null;
            QueryResult queryResult = null;
            String soql = null;
            String where = null;

            // Check to see what type of group member lookup we're doing
            if (isUserCount == true)
            {
                // If we're counting the users, we don't want to filter by a specific user, we just want the count
                soql = "SELECT Count(OwnerId) FROM CollaborationGroup WHERE Id = '" + referenceGroupId + "'";
            }
            else
            {
                // Select from the collaboration groups for the reference group id with this user as owner
                soql = "SELECT OwnerId FROM CollaborationGroup WHERE Id = '" + referenceGroupId + "' AND OwnerId = '" + thisUserId + "'";
            }

            // Create a new authentication utils response object to house the results
            authenticationUtilsResponse = new AuthenticationUtilsResponse();

            // Query salesforce to see if anything comes back
            queryResult = sforceService.query(soql);

            // Check to see if the query returned any results
            if (queryResult != null &&
                queryResult.records != null &&
                queryResult.records.Length > 0)
            {
                if (isUserCount == true)
                {
                    if (queryResult.records[0].Any != null &&
                        queryResult.records[0].Any.Length > 0)
                    {
                        // Just get the count out of the result
                        authenticationUtilsResponse.Count = Int32.Parse(queryResult.records[0].Any[0].InnerText);
                    }
                }
                else
                {
                    // If we have a result, then the user is in context
                    authenticationUtilsResponse.IsInContext = true;

                    // Now we query the system again, but this time with the query for the actual user
                    where = "Id = '" + thisUserId + "'";

                    // Grab the user object
                    authenticationUtilsResponse.UserObject = this.ExecuteUserQuery(sforceService, where).UserObject;
                }
            }

            return authenticationUtilsResponse;
        }

        /// <summary>
        /// Get the count of all users in the org.
        /// </summary>
        private AuthenticationUtilsResponse OrgUserCount(SforceService sforceService)
        {
            AuthenticationUtilsResponse authenticationUtilsResponse = null;
            QueryResult queryResult = null;
            String soql = null;

            // Get the count of all users in the org
            soql = "SELECT Count(Id) FROM User";

            // Create a new authentication utils response object to house the results
            authenticationUtilsResponse = new AuthenticationUtilsResponse();

            // Query salesforce to see if anything comes back
            queryResult = sforceService.query(soql);

            // Check to see if the query returned any results
            if (queryResult != null &&
                queryResult.records != null &&
                queryResult.records.Length > 0 &&
                queryResult.records[0].Any != null &&
                queryResult.records[0].Any.Length > 0)
            {
                // Just get the count out of the result
                authenticationUtilsResponse.Count = Int32.Parse(queryResult.records[0].Any[0].InnerText);
            }

            return authenticationUtilsResponse;
        }

        /// <summary>
        /// Utility method for executing user queries against salesforce.com
        /// </summary>
        private QueryResponseHelper ExecuteUserQuery(SforceService sforceService, String where)
        {
            QueryResponseHelper queryResponseHelper = null;
            ObjectAPI userObject = null;
            QueryResult queryResult = null;
            sObject queryObject = null;

            // Create a new instance of the query response helper
            queryResponseHelper = new QueryResponseHelper();

            // Query salesforce to see if anything comes back
            queryResult = sforceService.query("SELECT Id, Username, Email, FirstName, LastName, ManagerId FROM User WHERE " + where);

            // Check to see if the query returned any results
            if (queryResult != null &&
                queryResult.records != null &&
                queryResult.records.Length > 0)
            {
                // Check to make sure that only one result was returned
                if (queryResult.records.Length > 1)
                {
                    throw new ArgumentException("The user query returned more than one result. The WHERE clause is: " + where);
                }

                // Create a new user object as we have one
                userObject = this.CreateUserObject(sforceService);

                // Grab the sobject from the array - this is our user
                queryObject = queryResult.records[0];

                // Now that we have the user, we need to get the properties from the object so we can map them to a manywho user
                for (int y = 0; y < queryObject.Any.Length; y++)
                {
                    PropertyAPI userProperty = null;
                    XmlElement element = queryObject.Any[y];

                    // We opportunistically grab the manager id also as it's useful for subsequent querying
                    if (element.Name.Equals(SALESFORCE_SOBJECT_MANAGER_ID, StringComparison.InvariantCultureIgnoreCase) == true)
                    {
                        queryResponseHelper.ManagerId = element.InnerText;
                    }
                    else
                    {
                        // Remap the salesforce property to a manywho property and create the object
                        userProperty = this.CreateProperty(this.RemapName(element.Name), element.InnerText);

                        // If this is the ID property, we assign that as the external id so manywho can track the value properly
                        if (userProperty.developerName.Equals(ManyWhoConstants.MANYWHO_USER_PROPERTY_USER_ID, StringComparison.InvariantCultureIgnoreCase) == true)
                        {
                            userObject.externalId = userProperty.contentValue;
                        }

                        // Add the property to the new user object
                        userObject.properties.Add(userProperty);
                    }
                }

                // Set the status to OK for the user - this user has authenticated OK
                userObject.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_STATUS, ManyWhoConstants.AUTHORIZATION_STATUS_AUTHORIZED));
            }

            // Assign the user object to the helper
            queryResponseHelper.UserObject = userObject;

            // Return the user object - which will be null if the user could not be found
            return queryResponseHelper;
        }

        /// <summary>
        /// Utility method for creating the standard properties for the user object.
        /// </summary>
        private ObjectAPI CreateUserObject(SforceService sforceService)
        {
            ObjectAPI userObject = null;

            userObject = new ObjectAPI();
            userObject.developerName = ManyWhoConstants.MANYWHO_USER_DEVELOPER_NAME;
            userObject.properties = new List<PropertyAPI>();
            userObject.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_COUNTRY, null));
            userObject.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_LANGUAGE, null));
            userObject.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_LOCATION, null));
            userObject.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_DIRECTORY_ID, sforceService.getUserInfo().organizationId));
            userObject.properties.Add(CreateProperty(ManyWhoConstants.MANYWHO_USER_PROPERTY_DIRECTORY_NAME, sforceService.getUserInfo().organizationName));

            return userObject;
        }

        /// <summary>
        /// Utility method for creating new properties.
        /// </summary>
        private PropertyAPI CreateProperty(String developerName, String contentValue)
        {
            PropertyAPI propertyAPI = null;

            propertyAPI = new PropertyAPI();
            propertyAPI.developerName = developerName;
            propertyAPI.contentValue = contentValue;

            return propertyAPI;
        }

        /// <summary>
        /// Utility method for mapping salesforce field names to ManyWho property developer names.
        /// </summary>
        private String RemapName(String salesforceName)
        {
            String manywhoName = null;

            if (salesforceName.Equals(SALESFORCE_SOBJECT_USER_ID, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                manywhoName = ManyWhoConstants.MANYWHO_USER_PROPERTY_USER_ID;
            }
            else if (salesforceName.Equals(SALESFORCE_SOBJECT_USERNAME, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                manywhoName = ManyWhoConstants.MANYWHO_USER_PROPERTY_USERNAME;
            }
            else if (salesforceName.Equals(SALESFORCE_SOBJECT_EMAIL, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                manywhoName = ManyWhoConstants.MANYWHO_USER_PROPERTY_EMAIL;
            }
            else if (salesforceName.Equals(SALESFORCE_SOBJECT_FIRST_NAME, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                manywhoName = ManyWhoConstants.MANYWHO_USER_PROPERTY_FIRST_NAME;
            }
            else if (salesforceName.Equals(SALESFORCE_SOBJECT_LAST_NAME, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                manywhoName = ManyWhoConstants.MANYWHO_USER_PROPERTY_LAST_NAME;
            }
            else
            {
                throw new ArgumentException("The provided name could not be mapped: " + salesforceName);
            }

            return manywhoName;
        }
    }

    class AuthenticationUtilsResponse
    {
        public Boolean IsInContext
        {
            get;
            set;
        }

        public ObjectAPI UserObject
        {
            get;
            set;
        }

        public Int32 Count
        {
            get;
            set;
        }
    }

    class QueryResponseHelper
    {
        public String ManagerId
        {
            get;
            set;
        }

        public ObjectAPI UserObject
        {
            get;
            set;
        }
    }
}
