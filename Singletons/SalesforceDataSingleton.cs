using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml;
using System.Web;
using System.Web.Http;
using System.Net;
using System.Net.Http;
using System.Web.Services.Protocols;
using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Utils;
using ManyWho.Flow.SDK.Describe;
using ManyWho.Flow.SDK.Security;
using ManyWho.Flow.SDK.Draw.Elements.Type;
using ManyWho.Flow.SDK.Draw.Elements.Group;
using ManyWho.Flow.SDK.Run;
using ManyWho.Flow.SDK.Run.Elements.Config;
using ManyWho.Flow.SDK.Run.Elements.Type;
using ManyWho.Service.Salesforce.Utils;
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
    public class SalesforceDataSingleton
    {
        public const String SELECTABLE = "Selectable";
        public const String INSERTABLE = "Createable";
        public const String UPDATEABLE = "Updateable";
        public const String DELETABLE = "Deletable";

        public const String MANAGER_FIELD_NAME = "Manager";

        public const String KEY_AUTHENTICATION_URL = "AuthenticationUrl";
        public const String KEY_USERNAME = "Username";
        public const String KEY_PASSWORD = "Password";
        public const String KEY_SECURITY_TOKEN = "SecurityToken";

        private static SalesforceDataSingleton salesforceDataSingleton;
        private QueryConstructorUtil queryConstructorUtil;

        private SalesforceDataSingleton()
        {
            queryConstructorUtil = new QueryConstructorUtil();
        }

        public static SalesforceDataSingleton GetInstance()
        {
            if (salesforceDataSingleton == null)
            {
                salesforceDataSingleton = new SalesforceDataSingleton();
            }

            return salesforceDataSingleton;
        }

        public List<TypeElementBindingAPI> DescribeTables(IAuthenticatedWho authenticatedWho, List<EngineValueAPI> configurationValues)
        {
            TypeElementBindingAPI typeElementBinding = null;
            List<TypeElementBindingAPI> typeElementBindings = null;
            SforceService sforceService = null;
            DescribeGlobalResult describeGlobalResult = null;

            // Login to the service
            sforceService = this.Login(authenticatedWho, configurationValues, false, true);

            if (sforceService == null)
            {
                throw new ArgumentNullException("SalesforceService", "Unable to log into Salesforce.");
            }

            // Get all the objects available in the org
            describeGlobalResult = sforceService.describeGlobal();

            if (describeGlobalResult != null &&
                describeGlobalResult.sobjects != null &&
                describeGlobalResult.sobjects.Length > 0)
            {
                typeElementBindings = new List<TypeElementBindingAPI>();

                for (int x = 0; x < describeGlobalResult.sobjects.Length; x++)
                {
                    DescribeGlobalSObjectResult describeGlobalSObjectResult = describeGlobalResult.sobjects[x];

                    // Objects currently need to support all of these to be available through the API
                    if (describeGlobalSObjectResult.createable == true &&
                        describeGlobalSObjectResult.deletable == true &&
                        describeGlobalSObjectResult.updateable == true)
                    {
                        typeElementBinding = new TypeElementBindingAPI();
                        typeElementBinding.id = null;
                        typeElementBinding.serviceElementId = null;
                        typeElementBinding.databaseTableName = describeGlobalSObjectResult.name;
                        typeElementBinding.developerName = describeGlobalSObjectResult.name;
                        typeElementBinding.developerSummary = "The binding to save " + describeGlobalSObjectResult.name + " objects into salesforce.com";

                        typeElementBindings.Add(typeElementBinding);
                    }
                }
            }

            return typeElementBindings;
        }

        public List<TypeElementPropertyBindingAPI> DescribeFields(IAuthenticatedWho authenticatedWho, List<EngineValueAPI> configurationValues, String tableName)
        {
            DescribeSObjectResult describeSObjectResult = null;
            SforceService sforceService = null;

            // Login to the service
            sforceService = this.Login(authenticatedWho, configurationValues, false, true);

            if (sforceService == null)
            {
                throw new ArgumentNullException("SalesforceService", "Unable to log into Salesforce.");
            }

            // Grab the describe so we have it
            describeSObjectResult = sforceService.describeSObject(tableName);

            // Execute the internal method
            return this.DescribeFields(authenticatedWho, sforceService, describeSObjectResult, tableName);
        }

        private List<TypeElementPropertyBindingAPI> DescribeFields(IAuthenticatedWho authenticatedWho, SforceService sforceService, DescribeSObjectResult describeSObjectResult, String tableName)
        {
            TypeElementPropertyBindingAPI typeElementFieldBinding = null;
            List<TypeElementPropertyBindingAPI> typeElementFieldBindings = null;
            Field[] fields = null;

            // Grab the object description and pull out the fields
            fields = describeSObjectResult.fields;

            if (fields != null &&
                fields.Length > 0)
            {
                typeElementFieldBindings = new List<TypeElementPropertyBindingAPI>();

                for (int x = 0; x < fields.Length; x++)
                {
                    Field field = fields[x];

                    typeElementFieldBinding = new TypeElementPropertyBindingAPI();
                    typeElementFieldBinding.databaseFieldName = field.name;
                    typeElementFieldBinding.databaseContentType = field.type.ToString();

                    typeElementFieldBindings.Add(typeElementFieldBinding);

                    // Check to see if this field reference should be added
                    if (this.AddReferenceField(tableName, field) == true)
                    {
                        // Add the reference to the binding
                        typeElementFieldBinding = new TypeElementPropertyBindingAPI();
                        typeElementFieldBinding.databaseFieldName = field.relationshipName + ".Name";
                        typeElementFieldBinding.databaseContentType = field.type.ToString();

                        typeElementFieldBindings.Add(typeElementFieldBinding);
                    }
                }
            }

            return typeElementFieldBindings;
        }

        private Boolean AddReferenceField(String tableName, Field field)
        {
            Boolean addReferenceField = false;

            // If this is an id lookup field, we want to get the name reference also
            // We exclude the Connection fields as though they are reference fields, they do not comply
            // with the standard for having a "name"
            if (field.type.ToString().Equals("reference", StringComparison.OrdinalIgnoreCase) == true &&
                String.IsNullOrWhiteSpace(field.relationshipName) == false &&
                field.relationshipName.Equals("ConnectionReceived", StringComparison.OrdinalIgnoreCase) == false &&
                field.relationshipName.Equals("ConnectionSent", StringComparison.OrdinalIgnoreCase) == false)
            {
                // Add additional reference testing for tables that do not have name properties
                if (field.referenceTo != null &&
                    field.referenceTo.Length > 0)
                {
                    for (int i = 0; i < field.referenceTo.Length; i++)
                    {
                        if (String.IsNullOrWhiteSpace(field.referenceTo[i]) == false &&
                            (field.referenceTo[i].EndsWith("__c", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Account", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("AccountCleanInfo", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("AccountOwnerSharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("AccountTag", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("AccountTerritoryAssignmentRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("AccountTerritorySharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("AdditionalNumber", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("AgentWork", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ApexClass", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ApexComponent", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ApexPage", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ApexTrigger", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("AppMenuItem", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Asset", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("AssetOwnerSharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("AssetTag", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("AssignmentRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Attachment", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("BackgroundOperation", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("BrandTemplate", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("BusinessHours", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("BusinessProcess", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("CallCenter", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Campaign", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("CampaignMember", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("CampaignOwnerSharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("CampaignTag", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("CaseOwnerSharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("CaseTag", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("CaseTeamRole", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("CaseTeamTemplate", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ChatterAnswersReputationLevel", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("CollaborationGroup", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Community", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ConnectedApplication", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Contact", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ContactCleanInfo", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ContactOwnerSharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ContactTag", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ContentDistribution", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ContentFolder", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ContentHubItem", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ContentWorkspace", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ContractTag", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("CronJobDetail", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("DandBCompany", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("DashboardComponent", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("DashboardTag", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("DatacloudCompany", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("DatacloudDandBCompany", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("DatacloudOwnedEntity", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("DatacloudPurchaseUsage", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Division", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Document", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("DocumentTag", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("DuplicateRecordItem", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("DuplicateRecordSet", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("EmailTemplate", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Entitlement", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("EntitlementContact", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("EntitlementTemplate", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("EnvironmentHubMember", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("EventTag", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("FiscalYearSettings", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("FlowInterview", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Folder", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Goal", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("GoalLink", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("GoogleDoc", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Group", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("HashtagDefinition", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Holiday", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("IdeaReputationLevel", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("KnowledgeArticleViewStat", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Lead", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("LeadCleanInfo", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("LeadOwnerSharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("LeadTag", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ListView", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("LiveAgentSession", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("LiveAgentSessionOwnerSharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("LiveChatTranscript", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("LiveChatTranscriptEvent", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("LiveChatTranscriptOwnerSharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("LiveChatTranscriptSkill", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("LiveChatVisitor", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("MacroInstruction", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("MailmergeTemplate", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Metric", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("MetricDataLink", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("MilestoneType", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Name", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Network", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("NetworkActivityAudit", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("NoteTag", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Opportunity", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("OpportunityLineItem", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("OpportunityOwnerSharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("OpportunityTag", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("OpportunityTeamMember", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Order", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("OrderOwnerSharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Organization", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("PermissionSet", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Pricebook2", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("PricebookEntry", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ProcessDefinition", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ProcessNode", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Product2", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Profile", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ProfileSkill", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ProfileSkillEndorsement", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ProfileSkillUser", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("PushTopic", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("QuestionSubscription", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("QuickText", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("QuickTextOwnerSharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Quote", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("QuoteDocument", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("RecentlyViewed", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("RecordType", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Reply", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ReplyReportAbuse", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Report", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ReportTag", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Scontrol", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("SelfServiceUser", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ServiceContract", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("ServiceContractOwnerSharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Site", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("SlaProcess", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("SolutionTag", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("SOSSession", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("SOSSessionActivity", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("SOSSessionOwnerSharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("StaticResource", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("StreamingChannel", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("TagDefinition", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("TaskTag", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Territory", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Territory2", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Territory2Model", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("Topic", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("TopicLocalization—Beta", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("User", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("UserAppMenuItem", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("UserLicense", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("UserMembershipSharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("UserProfile", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("UserProvAccount", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("UserProvAccountStaging", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("UserProvMockTarget", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("UserProvisioningLog", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("UserProvisioningRequest", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("UserProvisioningRequestOwnerSharingRule", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("UserRole", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("UserServicePresence", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("UserTerritory2Association", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("WebLink", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("WorkBadgeDefinition", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("WorkCoaching", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("WorkFeedback", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("WorkFeedbackQuestion", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("WorkFeedbackQuestionSet", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("WorkFeedbackQuestionShare", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("WorkFeedbackRequest", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("WorkGoal", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("WorkGoalHistory", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("WorkGoalLink", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("WorkPerformanceCycle", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("WorkRewardFund", StringComparison.OrdinalIgnoreCase) == true ||
                             field.referenceTo[i].Equals("WorkRewardFundType", StringComparison.OrdinalIgnoreCase) == true))
                        {
                            // This reference field does have a name property
                            addReferenceField = true;
                            break;
                        }
                    }
                }
            }

            return addReferenceField;
        }

        public List<TypeElementRequestAPI> GetTypeElements(IAuthenticatedWho authenticatedWho, List<EngineValueAPI> configurationValues)
        {
            TypeElementRequestAPI typeElement = null;
            List<TypeElementRequestAPI> typeElements = null;
            TypeElementBindingAPI typeElementBinding = null;
            TypeElementPropertyAPI typeElementEntry = null;
            TypeElementPropertyBindingAPI typeElementFieldBinding = null;
            SforceService sforceService = null;
            DescribeGlobalResult describeGlobalResult = null;
            DescribeSObjectResult[] describeSObjectResults = null;
            Dictionary<String, Int32> objectNames = null;
            Dictionary<String, Int32> fieldNames = null;
            String[] tables = null;
            Int32 entryCounter = 0;

            // Login to the service
            sforceService = this.Login(authenticatedWho, configurationValues, false, true);

            if (sforceService == null)
            {
                throw new ArgumentNullException("SalesforceService", "Unable to log into Salesforce.");
            }

            // Get all the objects available in the org
            describeGlobalResult = sforceService.describeGlobal();

            // Create a new dictionary to store the object names so we can detect conflicts
            objectNames = new Dictionary<String, Int32>();

            // Get the names of all of the objects so we can then do a full object query
            if (describeGlobalResult != null &&
                describeGlobalResult.sobjects != null &&
                describeGlobalResult.sobjects.Length > 0)
            {
                // 100 is the maximum number of describes that can be done for salesforce
                tables = new String[100];

                for (int x = 0; x < describeGlobalResult.sobjects.Length; x++)
                {
                    tables[entryCounter] = describeGlobalResult.sobjects[x].name;

                    // We're at the 100th or last entry in the list and need to call the describe call with this chunk
                    if (entryCounter == 99 || x == (describeGlobalResult.sobjects.Length - 1))
                    {
                        // Now grab the full object descriptions
                        describeSObjectResults = sforceService.describeSObjects(tables);

                        // Now we go through the full object descriptions to create the types
                        if (describeSObjectResults != null &&
                            describeSObjectResults.Length > 0)
                        {
                            // Check to see if the list of type elements is null - it will be for the first chunk
                            if (typeElements == null)
                            {
                                typeElements = new List<TypeElementRequestAPI>();
                            }

                            for (int y = 0; y < describeSObjectResults.Length; y++)
                            {
                                DescribeSObjectResult describeSObjectResult = describeSObjectResults[y];
                                String typeDeveloperName = null;

                                typeDeveloperName = describeSObjectResult.label;

                                // Check to see if this object name already exists
                                if (objectNames.ContainsKey(describeSObjectResult.label.ToLower()) == true)
                                {
                                    Int32 counter = 0;

                                    // Get the object name counter out of the dictionary
                                    objectNames.TryGetValue(describeSObjectResult.label.ToLower(), out counter);

                                    // Increment the counter
                                    counter++;

                                    // Change the developer name
                                    typeDeveloperName += " " + counter;

                                    // Apply the counter back so we know what the next counter needs to be
                                    objectNames[describeSObjectResult.label.ToLower()] = counter;
                                }
                                else
                                {
                                    // Add this table to our list so we're tracking it for future rounds
                                    objectNames.Add(describeSObjectResult.label.ToLower(), 0);
                                }

                                typeElement = new TypeElementRequestAPI();
                                typeElement.developerName = typeDeveloperName;
                                typeElement.developerSummary = null;
                                typeElement.bindings = new List<TypeElementBindingAPI>();
                                typeElement.elementType = ManyWhoConstants.TYPE_ELEMENT_TYPE_IMPLEMENTATION_TYPE;

                                typeElementBinding = new TypeElementBindingAPI();
                                typeElementBinding.databaseTableName = describeSObjectResult.name;
                                typeElementBinding.developerName = "Salesforce.com " + describeSObjectResult.name + " Binding";
                                typeElementBinding.developerSummary = "The binding to save " + describeSObjectResult.name + " objects into salesforce.com";

                                typeElement.bindings.Add(typeElementBinding);

                                if (describeSObjectResult.fields != null &&
                                    describeSObjectResult.fields.Length > 0)
                                {
                                    // The dictionary to make we don't have fields with the same name
                                    fieldNames = new Dictionary<String, Int32>();

                                    typeElement.properties = new List<TypeElementPropertyAPI>();
                                    typeElementBinding.propertyBindings = new List<TypeElementPropertyBindingAPI>();

                                    for (int z = 0; z < describeSObjectResult.fields.Length; z++)
                                    {
                                        Field field = describeSObjectResult.fields[z];
                                        String fieldDeveloperName = null;

                                        fieldDeveloperName = field.label;

                                        // Check to see if this field name already exists
                                        if (fieldNames.ContainsKey(field.label.ToLower()) == true)
                                        {
                                            Int32 counter = 0;

                                            // Get the field name counter out of the dictionary
                                            fieldNames.TryGetValue(field.label.ToLower(), out counter);

                                            // Increment the counter
                                            counter++;

                                            // Change the developer name
                                            fieldDeveloperName += " " + counter;

                                            // Apply the counter back so we know what the next counter needs to be
                                            fieldNames[field.label.ToLower()] = counter;
                                        }
                                        else
                                        {
                                            // Add this field to our list so we're tracking it for future rounds
                                            fieldNames.Add(field.label.ToLower(), 0);
                                        }

                                        typeElementFieldBinding = new TypeElementPropertyBindingAPI();
                                        typeElementFieldBinding.databaseFieldName = field.name;
                                        typeElementFieldBinding.databaseContentType = field.type.ToString();
                                        typeElementFieldBinding.typeElementPropertyDeveloperName = fieldDeveloperName;

                                        typeElementBinding.propertyBindings.Add(typeElementFieldBinding);

                                        typeElementEntry = new TypeElementPropertyAPI();
                                        typeElementEntry.contentType = this.TranslateToManyWhoContentType(field.type.ToString());
                                        typeElementEntry.developerName = fieldDeveloperName;
                                        typeElementEntry.typeElementDeveloperName = describeSObjectResult.name;

                                        typeElement.properties.Add(typeElementEntry);

                                        // Check to see if this field reference should be added
                                        if (this.AddReferenceField(describeSObjectResult.name, field) == true)
                                        {
                                            // Add the reference to the binding
                                            typeElementFieldBinding = new TypeElementPropertyBindingAPI();
                                            typeElementFieldBinding.databaseFieldName = field.relationshipName + ".Name";
                                            typeElementFieldBinding.databaseContentType = field.type.ToString();
                                            typeElementFieldBinding.typeElementPropertyDeveloperName = fieldDeveloperName + " Name";

                                            typeElementBinding.propertyBindings.Add(typeElementFieldBinding);

                                            typeElementEntry = new TypeElementPropertyAPI();
                                            typeElementEntry.contentType = this.TranslateToManyWhoContentType(field.type.ToString());
                                            typeElementEntry.developerName = fieldDeveloperName + " Name";
                                            typeElementEntry.typeElementDeveloperName = describeSObjectResult.name;

                                            typeElement.properties.Add(typeElementEntry);
                                        }
                                    }
                                }

                                typeElements.Add(typeElement);
                            }
                        }

                        entryCounter = 0;
                        tables = new String[100];
                    }
                    else
                    {
                        entryCounter++;
                    }
                }
            }

            return typeElements;
        }

        public List<ObjectAPI> Save(INotifier notifier, IAuthenticatedWho authenticatedWho, List<EngineValueAPI> configurationValues, List<ObjectDataTypePropertyAPI> objectDataTypeProperties, List<ObjectAPI> objectAPIs)
        {
            CleanedObjectDataTypeProperties cleanedObjectDataTypeProperties = null;
            List<ObjectDataTypePropertyAPI> objectDataTypePropertiesToSelect = null;
            Dictionary<String, Boolean> currencyFields = null;
            DescribeSObjectResult describeSObjectResult = null;
            Field[] fields = null;
            String selectSoql = null;
            String idWhere = null;
            List<PropertyAPI> propertyAPIs = null;
            List<sObject> createObjects = null;
            List<sObject> updateObjects = null;
            SaveResult[] saveResults = null;
            SforceService sforceService = null;
            String objectName = null;
            Boolean includesId = false;

            // Check to make sure we have some objects to save
            if (objectAPIs != null &&
                objectAPIs.Count > 0)
            {
                // Step 1: Login to the service so we can do a bunch of things
                sforceService = this.Login(authenticatedWho, configurationValues, false, false);

                if (sforceService == null)
                {
                    throw new ArgumentNullException("SalesforceService", "Unable to log into Salesforce.");
                }

                // Step 2: in the save is to get the latest information about the object from salesforce
                // TODO: this should definitely be cached and operate under a rolling nightly refresh

                // We need to get the name of the table from the first object in the list
                objectName = objectAPIs[0].developerName;

                if (objectName == null ||
                    objectName.Trim().Length == 0)
                {
                    String errorMessage = "One or more of the objects being saved does not have a DeveloperName. We don't know where to save the object without one!";

                    ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_WARNING, errorMessage);

                    throw new ArgumentNullException("BadRequest", errorMessage);
                }

                describeSObjectResult = this.GetDescribeResult(notifier, authenticatedWho, sforceService, objectName);

                // Step 3: Now that we have the latest object information from salesforce.com, we can make sure the object coming in is correctly applied.
                // We go through the fields in the describe call and then find the same field in the in-coming object data so we can see what do do with
                // the value and check its validity
                fields = describeSObjectResult.fields;

                // First, we go through each object in the list - that's the outer most loop
                foreach (ObjectAPI objectAPI in objectAPIs)
                {
                    Boolean isUpdate = false;
                    Guid testGuid = Guid.Empty;
                    List<PropertyAPI> propertiesToRemove = null;

                    // Get the object type out of the first entry - all objects in the list must be from the same type
                    // We get the properties out here for the select - before any properties get removed for the update!
                    if (propertyAPIs == null)
                    {
                        // We'll use these later for the lookup
                        propertyAPIs = new List<PropertyAPI>();
                        propertyAPIs.AddRange(objectAPI.properties);
                    }

                    // Check to see if we have an external id and that it's not a guid (though the guid problem is now fixed)
                    if (objectAPI.externalId != null &&
                        objectAPI.externalId.Trim().Length > 0 &&
                        Guid.TryParse(objectAPI.externalId, out testGuid) == false)
                    {
                        isUpdate = true;
                    }

                    // Test the object is the same type as the first entry
                    if (objectAPI.developerName.Equals(objectName, StringComparison.InvariantCultureIgnoreCase) == false)
                    {
                        String errorMessage = "The object list contains objects of varying types - this is not supported in a single save call";

                        ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_WARNING, errorMessage);

                        throw new ArgumentNullException("BadRequest", errorMessage);
                    }

                    // Make sure we have some properties in the object
                    if (objectAPI.properties == null ||
                        objectAPI.properties.Count == 0)
                    {
                        String errorMessage = "The object being saved does not have any properties so there is nothing to save! The object id being saved is: " + objectAPI.externalId + " (" + objectAPI.internalId + ")";

                        ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_WARNING, errorMessage);

                        throw new ArgumentNullException("BadRequest", errorMessage);
                    }

                    // Instantiate the list of properties to remove
                    propertiesToRemove = new List<PropertyAPI>();

                    // Now we have the object, we want to go through the fields in the describe and match it with the property in the object
                    if (fields != null &&
                        fields.Length > 0)
                    {
                        // Go through each field one-by-one
                        foreach (Field field in describeSObjectResult.fields)
                        {
                            PropertyAPI referencedPropertyAPI = null;
                            String fieldDataType = null;
                            String contentType = null;

                            // Get the data type from the field
                            fieldDataType = field.type.ToString();

                            // Now go through each of the properties in the object one-by-one
                            foreach (PropertyAPI propertyAPI in objectAPI.properties)
                            {
                                if (propertyAPI.developerName == null)
                                {
                                    String errorMessage = "An object being saved has a blank DeveloperName for one of the properties.  All properties must have a valid DeveloperName matching a field in salesforce.com";

                                    ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_WARNING, errorMessage);

                                    throw new ArgumentNullException("BadRequest", errorMessage);
                                }

                                // Check to see if the developer name of this field matches with the property
                                if (propertyAPI.developerName.Equals(field.name, StringComparison.InvariantCultureIgnoreCase) == true)
                                {
                                    // We have our matching property, we can now check it against the field
                                    referencedPropertyAPI = propertyAPI;
                                    break;
                                }
                            }

                            // Grab the content type for the field - the content type as it should be from ManyWho
                            contentType = TranslateToManyWhoContentType(field.type.ToString());

                            // If the property does not have a value, we exclude it from the update. We assume that the user will be providing a value
                            // if they want to perform a save. The driver therefore does not currently support "nulling" or "blanking" field values. This will
                            // help prevent any data loss issues and blanking values feels like an edge case that's more likely to cause problems than solve them!
                            if (referencedPropertyAPI != null)
                            {
                                if (referencedPropertyAPI.contentValue == null ||
                                    referencedPropertyAPI.contentValue.Trim().Length == 0)
                                {
                                    propertiesToRemove.Add(referencedPropertyAPI);
                                    continue;
                                }
                                else if (contentType != null &&
                                         contentType.Equals(ManyWhoConstants.CONTENT_TYPE_DATETIME) == true)
                                {
                                    DateTime dateTimeCheck;

                                    if (DateTime.TryParse(referencedPropertyAPI.contentValue, out dateTimeCheck) == true &&
                                        dateTimeCheck == DateTime.MinValue)
                                    {
                                        // Don't insert min dates as salesforce will throw an error
                                        propertiesToRemove.Add(referencedPropertyAPI);
                                        continue;
                                    }
                                }
                            }

                            // If the field is not creatable and updateable, we remove it from the save. Normally we should not have fields of this kind
                            // in the type - however - as this stuff was implemented post salesforce service creation by a number of tenants, we need to check
                            // for it.  We don't bother warning as it would get highly annoying (given that we've basically done it to them!)
                            if (field.createable == false &&
                                field.updateable == false)
                            {
                                // Check to see if the object references this field
                                if (referencedPropertyAPI != null)
                                {
                                    // It does, so we need to remove that reference from the save
                                    propertiesToRemove.Add(referencedPropertyAPI);

                                    // No need to go any further - the field is not one we're going to pass through to SFDC
                                    continue;
                                }
                            }
                            else if (field.createable == true &&
                                     field.updateable == false &&
                                     isUpdate == true)
                            {
                                // We're performing an update on a field that is not updatable
                                // Check to see if the object references this field
                                if (referencedPropertyAPI != null)
                                {
                                    // It does, so we need to remove that reference from the save
                                    propertiesToRemove.Add(referencedPropertyAPI);

                                    // No need to go any further - the field is not one we're going to pass through to SFDC
                                    continue;
                                }
                            }
                            else if (field.createable == false &&
                                     field.updateable == true &&
                                     isUpdate == false)
                            {
                                // We're performing a create on a field that is not creatable
                                // Check to see if the object references this field
                                if (referencedPropertyAPI != null)
                                {
                                    // It does, so we need to remove that reference from the save
                                    propertiesToRemove.Add(referencedPropertyAPI);

                                    // No need to go any further - the field is not one we're going to pass through to SFDC
                                    continue;
                                }
                            }

                            // If the field is null and we're dealing with a boolean, we assign false as booleans can't be null really
                            if (contentType.Equals(ManyWhoConstants.CONTENT_TYPE_BOOLEAN, StringComparison.InvariantCultureIgnoreCase) == true &&
                                referencedPropertyAPI != null &&
                                (referencedPropertyAPI.contentValue == null ||
                                 referencedPropertyAPI.contentValue.Trim().Length == 0))
                            {
                                // Set null boolean values to false
                                referencedPropertyAPI.contentValue = "false";
                            }

                            // We don't currently support object data saving, so we only need to check against the content value for the validation stuff
                            if (field.nillable == false)
                            {
                                // The id field has special rules as we don't want to include it if we have nothing to assign - but equally, we don't want to gack
                                // as salesforce will assign an id if we don't include it in the request.
                                if (field.name.Equals("Id", StringComparison.InvariantCultureIgnoreCase) == true ||
                                    field.name.Equals("OwnerId", StringComparison.InvariantCultureIgnoreCase) == true ||
                                    field.name.Equals("IsDeleted", StringComparison.InvariantCultureIgnoreCase) == true ||
                                    field.name.Equals("CreatedById", StringComparison.InvariantCultureIgnoreCase) == true ||
                                    field.name.Equals("CreatedDate", StringComparison.InvariantCultureIgnoreCase) == true ||
                                    field.name.Equals("LastModifiedById", StringComparison.InvariantCultureIgnoreCase) == true ||
                                    field.name.Equals("LastModifiedDate", StringComparison.InvariantCultureIgnoreCase) == true ||
                                    field.name.Equals("SystemModstamp", StringComparison.InvariantCultureIgnoreCase) == true)
                                {
                                    if (referencedPropertyAPI == null ||
                                        referencedPropertyAPI.contentValue == null ||
                                        referencedPropertyAPI.contentValue.Trim().Length == 0 ||
                                        field.name.Equals("IsDeleted", StringComparison.InvariantCultureIgnoreCase) == true ||
                                        field.name.Equals("CreatedById", StringComparison.InvariantCultureIgnoreCase) == true ||
                                        field.name.Equals("CreatedDate", StringComparison.InvariantCultureIgnoreCase) == true ||
                                        field.name.Equals("LastModifiedById", StringComparison.InvariantCultureIgnoreCase) == true ||
                                        field.name.Equals("LastModifiedDate", StringComparison.InvariantCultureIgnoreCase) == true ||
                                        field.name.Equals("SystemModstamp", StringComparison.InvariantCultureIgnoreCase) == true)
                                    {
                                        if (referencedPropertyAPI != null)
                                        {
                                            // This is the id field and we have no value to assign to it - remove it from the list of values to set
                                            // Or the field is something that should not be changed - e.g. the created information or system defined stuff
                                            propertiesToRemove.Add(referencedPropertyAPI);
                                        }

                                        // We don't need to go any further on this loop - the property
                                        continue;
                                    }
                                }
                                // The field is not nillable, so we need to check we have a value if the user is passing one in - the previous logic
                                // will not include this field if the data coming in has not been included
                                else if (referencedPropertyAPI != null &&
                                         (referencedPropertyAPI.contentValue == null ||
                                          referencedPropertyAPI.contentValue.Trim().Length == 0))
                                {
                                    if ((field.updateable == false &&
                                         field.createable == false) ||
                                        (field.updateable == false &&
                                         isUpdate == true) ||
                                        (field.createable == false &&
                                         isUpdate == false))
                                    {
                                        if (referencedPropertyAPI != null)
                                        {
                                            // If the field is not updatable or creatable, then we need to leave it nill. If we're doing an update and it's not
                                            // updatable, the same. Same for creatable.
                                            // This is the id field and we have no value to assign to it - remove it from the list of values to set
                                            // Or the field is something that should not be changed - e.g. the created information or system defined stuff
                                            propertiesToRemove.Add(referencedPropertyAPI);
                                        }

                                        // We don't need to go any further on this loop - the property
                                        continue;
                                    }
                                    else
                                    {
                                        String errorMessage = "An attempt is being made to set a non-nillable field to a null value. The name of the field is: " + field.name;

                                        ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_WARNING, errorMessage);

                                        throw new ArgumentNullException("BadRequest", errorMessage);
                                    }
                                }
                            }

                            // We only need to do this bit if we actually have a referenced field
                            if (referencedPropertyAPI != null)
                            {
                                // Now we need to parse the value for the given type
                                if (contentType.Equals(ManyWhoConstants.CONTENT_TYPE_BOOLEAN, StringComparison.InvariantCultureIgnoreCase) == true)
                                {
                                    Boolean booleanValue = false;

                                    // Parse the content value to find out if it's true or false and to ensure it's a valid boolean
                                    Boolean.TryParse(referencedPropertyAPI.contentValue, out booleanValue);

                                    // Make sure the content value is in fact a valid boolean
                                    referencedPropertyAPI.contentValue = booleanValue.ToString().ToLower();
                                }
                                else if (contentType.Equals(ManyWhoConstants.CONTENT_TYPE_DATETIME, StringComparison.InvariantCultureIgnoreCase) == true)
                                {
                                    DateTime datetimeValue;

                                    // Parse the content value to find out if it's a valid date time
                                    DateTime.TryParse(referencedPropertyAPI.contentValue, out datetimeValue);

                                    if (datetimeValue == DateTime.MinValue)
                                    {
                                        referencedPropertyAPI.contentValue = null;
                                    }
                                    else
                                    {
                                        if ("date".Equals(fieldDataType, StringComparison.InvariantCultureIgnoreCase) == true)
                                        {
                                            referencedPropertyAPI.contentValue = datetimeValue.ToUniversalTime().ToString("yyyy-MM-dd");
                                        }
                                        else if ("datetime".Equals(fieldDataType, StringComparison.InvariantCultureIgnoreCase) == true)
                                        {
                                            referencedPropertyAPI.contentValue = datetimeValue.ToUniversalTime().ToString("s");
                                        }
                                        else if ("time".Equals(fieldDataType, StringComparison.InvariantCultureIgnoreCase) == true)
                                        {
                                            String errorMessage = "The salesforce.com plugin does not currently support \"time\" field types.";

                                            ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_WARNING, errorMessage);

                                            throw new ArgumentNullException("BadRequest", errorMessage);
                                        }
                                    }
                                }
                                else if (contentType.Equals(ManyWhoConstants.CONTENT_TYPE_NUMBER, StringComparison.InvariantCultureIgnoreCase) == true)
                                {
                                    Double numberValue = 0;

                                    // Parse the content value to find out if it's a true number and to ensure it's a valid number
                                    Double.TryParse(referencedPropertyAPI.contentValue, out numberValue);

                                    // Make sure the content value is in fact a valid number
                                    referencedPropertyAPI.contentValue = numberValue.ToString();
                                }
                                else if (contentType.Equals(ManyWhoConstants.CONTENT_TYPE_LIST, StringComparison.InvariantCultureIgnoreCase) == true ||
                                         contentType.Equals(ManyWhoConstants.CONTENT_TYPE_OBJECT, StringComparison.InvariantCultureIgnoreCase) == true)
                                {
                                    String errorMessage = "The salesforce.com plugin does not currently support objects that are of type Object or List.";

                                    ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_WARNING, errorMessage);

                                    throw new ArgumentNullException("BadRequest", errorMessage);
                                }
                            }
                        }
                    }

                    // We also need to go the other way to make sure an object property is not being assigned that doesn't exist in the fields
                    foreach (PropertyAPI propertyAPI in objectAPI.properties)
                    {
                        Boolean found = false;

                        // Go through each of the fields to check that it exists in the metadata
                        foreach (Field field in describeSObjectResult.fields)
                        {
                            if (field.name.Equals(propertyAPI.developerName, StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                // The field in the object data exists, so we'll have validated the content value in the above algorithm
                                found = true;
                                break;
                            }
                        }

                        if (found == false)
                        {
                            // We couldn't find the property, so we add it to the list of properties to remove
                            propertiesToRemove.Add(propertyAPI);
                        }
                    }

                    // If we have some properties to remove, we remove them here so we don't cause a fault in the save
                    if (propertiesToRemove.Count > 0)
                    {
                        foreach (PropertyAPI propertyAPI in propertiesToRemove)
                        {
                            // TODO: we should alert the user
                            // Remove the property from our list of properties that are going to be saved
                            objectAPI.properties.Remove(propertyAPI);
                        }
                    }
                }

                // Step 4: Now we've done all of the object manipulation, we can save the objects back to salesforce.com - but the type of save
                // will depend on the presence of an external identifier for salesforce.com
                createObjects = new List<sObject>();
                updateObjects = new List<sObject>();

                // Go through each of the objects we are wanting to save and transform them into sobjects
                foreach (ObjectAPI objectAPI in objectAPIs)
                {
                    Boolean isUpdate = false;
                    Guid testGuid = Guid.Empty;

                    // Check to see if we have an external id and that it's not a guid (though the guid problem is now fixed)
                    if (objectAPI.externalId != null &&
                        objectAPI.externalId.Trim().Length > 0 &&
                        Guid.TryParse(objectAPI.externalId, out testGuid) == false)
                    {
                        isUpdate = true;
                    }

                    if (isUpdate == true)
                    {
                        // Update the sobject and put it into our update list
                        updateObjects.Add(CreateSObjectFromObjectAPI(objectAPI));
                    }
                    else
                    {
                        // Create the sobject and put it into our insert list
                        createObjects.Add(CreateSObjectFromObjectAPI(objectAPI));
                    }
                }

                // We now need to build our "id where" soql so we can get the full object information back
                idWhere = "";
                selectSoql = "SELECT ";

                // Perform the update operation on any objects that need to be updated
                if (updateObjects != null &&
                    updateObjects.Count > 0)
                {
                    // Perform the update - this is where we can also save the inner objects - not mapped in yet
                    saveResults = sforceService.update(updateObjects.ToArray());

                    // Go through the save results and get the ids out of the saved objects or throw any errors
                    for (int x = 0; x < saveResults.Length; x++)
                    {
                        SaveResult upsertResult = saveResults[x];

                        if (upsertResult.success == true)
                        {
                            idWhere += "Id = '" + upsertResult.id + "' OR ";
                        }
                        else
                        {
                            for (int y = 0; y < upsertResult.errors.Length; y++)
                            {
                                throw new Exception(upsertResult.errors[y].message);
                            }
                        }
                    }
                }

                // Perform the insert operation on any objects that need to be inserted
                if (createObjects != null &&
                    createObjects.Count > 0)
                {
                    // Perform the insert - this is where we can also save the inner objects - not mapped in yet
                    saveResults = sforceService.create(createObjects.ToArray());

                    // Go through the save results and get the ids out of the saved objects or throw any errors
                    for (int x = 0; x < saveResults.Length; x++)
                    {
                        SaveResult upsertResult = saveResults[x];

                        if (upsertResult.success == true)
                        {
                            idWhere += "Id = '" + upsertResult.id + "' OR ";
                        }
                        else
                        {
                            for (int y = 0; y < upsertResult.errors.Length; y++)
                            {
                                throw new Exception(upsertResult.errors[y].message);
                            }
                        }
                    }
                }

                // Get rid of the trailing OR
                idWhere = idWhere.Substring(0, idWhere.Length - " OR ".Length);

                // Get the cleaned response out that includes currency information
                cleanedObjectDataTypeProperties = this.CleanObjectDataTypeProperties(null, authenticatedWho, sforceService, objectName, objectDataTypeProperties);

                // Clean the properties before using them for the select
                objectDataTypePropertiesToSelect = cleanedObjectDataTypeProperties.ObjectDataTypeProperties;
                currencyFields = cleanedObjectDataTypeProperties.CurrencyFields;

                // Now we need to check if we have the Id column
                foreach (ObjectDataTypePropertyAPI objectDataTypeProperty in objectDataTypePropertiesToSelect)
                {
                    // Add the field to the select soql
                    selectSoql += objectDataTypeProperty.developerName + ", ";

                    if (objectDataTypeProperty.developerName.Equals("Id", StringComparison.InvariantCultureIgnoreCase) == true)
                    {
                        includesId = true;
                    }
                }

                // If the query doesn't include the id field, we need to add that to the query
                if (includesId == false)
                {
                    selectSoql += "Id, ";
                }

                // Get rid of the trailing , and construct the full query
                selectSoql = selectSoql.Substring(0, selectSoql.Length - ", ".Length);
                selectSoql += " FROM " + objectName + " WHERE " + idWhere;

                // Dispatch the query and get the results.  We do this because salesforce may have done more
                // than simply complete the id field.  Workflow rules and other assignment features may have manipulated the
                // object values and all of this needs to be reflected in the result we send back to the user
                objectAPIs = CreateObjectAPIsFromQuerySObjects(sforceService, objectName, selectSoql, includesId, objectDataTypePropertiesToSelect, currencyFields);
            }

            return objectAPIs;
        }

        public List<ObjectAPI> Select(IAuthenticatedWho authenticatedWho, List<EngineValueAPI> configurationValues, String objectName, List<ObjectDataTypePropertyAPI> propertyAPIs, ListFilterAPI listFilterAPI, String soqlQuery, Dictionary<String, Boolean> currencyFields)
        {
            List<ObjectAPI> objectAPIs = null;
            SforceService sforceService = null;

            // Login to the service
            sforceService = this.Login(authenticatedWho, configurationValues, false, false);

            if (sforceService == null)
            {
                throw new ArgumentNullException("SalesforceService", "Unable to log into Salesforce.");
            }

            // If the request has a search query, we need to alter the SOQL to SOSL
            if (listFilterAPI != null &&
                listFilterAPI.search != null &&
                listFilterAPI.search.Trim().Length > 0)
            {
                throw new ArgumentNullException("BadRequest", "Search is not yet supported for command operations.");
            }
            else
            {
                if (listFilterAPI != null)
                {
                    if (listFilterAPI.id != null &&
                        listFilterAPI.id.Trim().Length == 0)
                    {
                        throw new ArgumentNullException("BadRequest", "ListFilter.Id is not yet supported for command operations.");
                    }

                    if (listFilterAPI.filterByProvidedObjects == true)
                    {
                        throw new ArgumentNullException("BadRequest", "ListFilter.FilterByProvidedObjects is not yet supported for command operations.");
                    }

                    if (listFilterAPI.where != null &&
                        listFilterAPI.where.Count > 0)
                    {
                        throw new ArgumentNullException("BadRequest", "ListFilter.Where is not yet supported for command operations.");
                    }
                }

                // Add the additional filtering to the command soql
                soqlQuery += this.queryConstructorUtil.ConstructQuery(listFilterAPI, null);

                // Dispatch the query and get the results
                objectAPIs = CreateObjectAPIsFromQuerySObjects(sforceService, objectName, soqlQuery, true, propertyAPIs, currencyFields);
            }

            return objectAPIs;
        }

        public List<ObjectAPI> Select(IAuthenticatedWho authenticatedWho, List<EngineValueAPI> configurationValues, String objectName, List<ObjectDataTypePropertyAPI> objectDataTypeProperties, ListFilterAPI listFilterAPI, Boolean isModelingOperation)
        {
            CleanedObjectDataTypeProperties cleanedObjectDataTypeProperties = null;
            Dictionary<String, Boolean> currencyFields = null;
            List<ObjectAPI> objectAPIs = null;
            SforceService sforceService = null;
            String soqlQuery = null;
            Boolean includesId = false;

            // Login to the service
            sforceService = this.Login(authenticatedWho, configurationValues, false, isModelingOperation);

            if (sforceService == null)
            {
                throw new ArgumentNullException("SalesforceService", "Unable to log into Salesforce.");
            }

            soqlQuery = "";

            // Get the cleaned properties and currency info
            cleanedObjectDataTypeProperties = this.CleanObjectDataTypeProperties(null, authenticatedWho, sforceService, objectName, objectDataTypeProperties);

            // Clean the properties before using them for the select
            objectDataTypeProperties = cleanedObjectDataTypeProperties.ObjectDataTypeProperties;
            currencyFields = cleanedObjectDataTypeProperties.CurrencyFields;

            // Create the columns for the query
            foreach (ObjectDataTypePropertyAPI objectDataTypeProperty in objectDataTypeProperties)
            {
                soqlQuery += objectDataTypeProperty.developerName + ", ";

                if (includesId == false &&
                    objectDataTypeProperty.developerName.ToLower() == "id")
                {
                    includesId = true;
                }
            }

            // If the user didn't include the id in the select, we need to add it
            if (includesId == false)
            {
                soqlQuery += "Id, ";
            }

            // If the request has a search query, we need to alter the SOQL to SOSL
            if (listFilterAPI != null &&
                listFilterAPI.search != null &&
                listFilterAPI.search.Trim().Length > 0 &&
                listFilterAPI.searchCriteria == null)
            {
                // Remove the final coma
                String fields = soqlQuery.Substring(0, soqlQuery.Length - 2);

                // Construct the sosl query - we don't need the columns
                soqlQuery = "FIND {" + listFilterAPI.search + "} IN ALL FIELDS RETURNING " + objectName + " (" + fields;
                soqlQuery += this.queryConstructorUtil.ConstructQuery(listFilterAPI, cleanedObjectDataTypeProperties) + ")";

                // Dispatch the search and get the results
                objectAPIs = CreateObjectAPIsFromSearchSObjects(null, sforceService, objectName, soqlQuery, includesId, objectDataTypeProperties, listFilterAPI, currencyFields);
            }
            else
            {
                soqlQuery = "SELECT " + soqlQuery.Substring(0, soqlQuery.Length - 2) + " ";
                soqlQuery += "FROM " + objectName;
                soqlQuery += this.queryConstructorUtil.ConstructQuery(listFilterAPI, cleanedObjectDataTypeProperties);

                // Dispatch the query and get the results
                objectAPIs = CreateObjectAPIsFromQuerySObjects(sforceService, objectName, soqlQuery, includesId, objectDataTypeProperties, currencyFields);
            }

            return objectAPIs;
        }

        public List<ObjectAPI> LoadSObjectByIdentifier(SforceService sforceService, String objectName, String objectId, Boolean translateToLabels)
        {
            List<ObjectDataTypePropertyAPI> properties = null;
            DescribeSObjectResult describeSObjectResult = null;
            List<ObjectAPI> objectAPIs = null;
            Field[] fields = null;
            String soql = String.Empty;

            // Get the full description of the sobject based on the API name
            describeSObjectResult = sforceService.describeSObject(objectName);

            // Get the list of fields for this object
            fields = describeSObjectResult.fields;

            // Create a new list of properties
            properties = new List<ObjectDataTypePropertyAPI>();

            for (int i = 0; i < fields.Length - 1; i++)
            {
                soql += fields[i].name + ", ";
                properties.Add(new ObjectDataTypePropertyAPI() { developerName = fields[i].name });
            }

            // Re-orient the select query so everything is in the right place
            soql = soql.Substring(0, soql.Length - 2);
            soql = "SELECT " + soql + " FROM " + objectName + " WHERE Id = '" + objectId + "'";

            // Execute the query on the remote system
            objectAPIs = CreateObjectAPIsFromQuerySObjects(sforceService, objectName, soql, true, properties, null);

            // Now we have the object APIs, we need to translate to the labels, not the db fields as this call is for services
            if (translateToLabels == true)
            {
                if (objectAPIs != null &&
                    objectAPIs.Count > 0)
                {
                    foreach (ObjectAPI objectAPI in objectAPIs)
                    {
                        // Go through the properties in the object
                        if (objectAPI.properties != null &
                            objectAPI.properties.Count > 0)
                        {
                            foreach (PropertyAPI propertyAPI in objectAPI.properties)
                            {
                                if (fields != null &&
                                    fields.Length > 0)
                                {
                                    for (int i = 0; i < fields.Length; i++)
                                    {
                                        if (propertyAPI.developerName.Equals(fields[i].name, StringComparison.InvariantCultureIgnoreCase) == true)
                                        {
                                            // Change the property developer name to the label
                                            propertyAPI.developerName = fields[i].label;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return objectAPIs;
        }

        private List<ObjectAPI> CreateObjectAPIsFromQuerySObjects(SforceService sforceService, String objectName, String soqlQuery, Boolean includesId, List<ObjectDataTypePropertyAPI> properties, Dictionary<String, Boolean> currencyFields)
        {
            QueryResult queryResult = null;
            sObject queryObject = null;
            ObjectAPI objectAPI = null;
            List<ObjectAPI> objectAPIs = null;
            Boolean isAggregate = false;
            Int32 order = 0;

            // Make the query call and get the query results
            queryResult = sforceService.query(soqlQuery);

            // Check to see if the soql query is contains any aggregate stuff - in which case we take the first expr as the unique identifier
            if (soqlQuery.IndexOf("sum(", StringComparison.InvariantCultureIgnoreCase) > 0 ||
                soqlQuery.IndexOf("max(", StringComparison.InvariantCultureIgnoreCase) > 0 ||
                soqlQuery.IndexOf("min(", StringComparison.InvariantCultureIgnoreCase) > 0 ||
                soqlQuery.IndexOf("avg(", StringComparison.InvariantCultureIgnoreCase) > 0 ||
                soqlQuery.IndexOf("count(", StringComparison.InvariantCultureIgnoreCase) > 0 ||
                soqlQuery.IndexOf("count_distinct(", StringComparison.InvariantCultureIgnoreCase) > 0)
            {
                isAggregate = true;
            }

            // Process the query results
            if (queryResult != null &&
                queryResult.records != null &&
                queryResult.records.Length > 0)
            {
                objectAPIs = new List<ObjectAPI>();

                for (int x = 0; x < queryResult.records.Length; x++)
                {
                    queryObject = queryResult.records[x];

                    String externalId = null;

                    objectAPI = new ObjectAPI();
                    objectAPI.developerName = objectName;
                    objectAPI.properties = new List<PropertyAPI>();
                    objectAPI.order = order++;

                    if (queryObject.Any.Length > properties.Count)
                    {
                        throw new ArgumentNullException("ObjectData.Properties", "The list of properties being requested does not match the number of properties being returned by Salesforce.");
                    }

                    for (int y = 0; y < queryObject.Any.Length; y++)
                    {
                        XmlElement element = queryObject.Any[y];
                        PropertyAPI propertyAPI = new PropertyAPI();

                        // Do not rely on the element name as this has proven to be inconsistent from Salesforce - the search gives different
                        // field names from a standard select which confuses the binding logic
                        //propertyAPI.developerName = element.LocalName;

                        // This only works because the SOQL columns will have been generated from the ordered list of properties
                        // If the user has a final column of Id that's been added, we just keep the local name. The purpose of this
                        // is to preserve the deep name references that are difficult to resolve as they can be blank and have no
                        // child fields to do detection. This is much more explicit.
                        //if (properties.Count >= y)
                        //{
                        // Always use the name as defined by the binding properties as this will be consistent in all situations
                        propertyAPI.developerName = properties[y].developerName;
                        //}

                        if (propertyAPI.developerName.EndsWith(".name", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            // If we're dealing with a compound field, we need to grab the text a little differently as it will sit inside                            
                            // additional XML
                            if (element.LastChild != null)
                            {
                                propertyAPI.contentValue = element.LastChild.InnerText;
                            }
                        }
                        else
                        {
                            propertyAPI.contentValue = element.InnerText;
                        }

                        // Check to see if this is a currency field, if so, we need to change the formatting slightly
                        if (currencyFields != null &&
                            String.IsNullOrWhiteSpace(propertyAPI.contentValue) == false &&
                            currencyFields.ContainsKey(propertyAPI.developerName) == true)
                        {
                            Double currencyValue = Double.MinValue;
                            // This is a currency field, so we need to improve the formatting so it's a proper number

                            // If the value parses OK, we convert it over, otherwise we leave it alone
                            if (Double.TryParse(propertyAPI.contentValue, out currencyValue) == true)
                            {
                                propertyAPI.contentValue = currencyValue.ToString("0.##");
                            }
                        }

                        if (element.LocalName.Equals("Id", StringComparison.InvariantCultureIgnoreCase) == true)
                        {
                            objectAPI.externalId = propertyAPI.contentValue;

                            // Don't add the property to the object if the user hasn't asked for it
                            if (includesId == true)
                            {
                                // Add the property to the new object
                                objectAPI.properties.Add(propertyAPI);
                            }
                        }
                        else if (isAggregate == true &&
                                 element.LocalName.Equals("expr0", StringComparison.InvariantCultureIgnoreCase) == true)
                        {
                            // Assign the external identifier to the first expression "expr0"
                            objectAPI.externalId = propertyAPI.contentValue;

                            // Make sure we add the property also
                            objectAPI.properties.Add(propertyAPI);
                        }
                        else if (element.LocalName.Equals("ExternalId", StringComparison.InvariantCultureIgnoreCase) == true)
                        {
                            // We also grab the external id as the id will be blank for the new data integration stuff
                            externalId = propertyAPI.contentValue;

                            // Make sure we add the property also
                            objectAPI.properties.Add(propertyAPI);
                        }
                        else
                        {
                            objectAPI.properties.Add(propertyAPI);
                        }
                    }

                    // If we're using an x object, we use the external identifier
                    if (objectName.EndsWith("__x", StringComparison.InvariantCultureIgnoreCase) == true)
                    {
                        // If the external id is also null, we then throw an exception
                        if (externalId == null ||
                            externalId.Trim().Length == 0)
                        {
                            throw new ArgumentNullException("BadRequest", "An object identifier could not be found.");
                        }

                        objectAPI.externalId = externalId;
                    }

                    // Add the object to the response list
                    objectAPIs.Add(objectAPI);
                }
            }

            return objectAPIs;
        }

        private List<ObjectAPI> CreateObjectAPIsFromSearchSObjects(IAuthenticatedWho authenticatedWho, SforceService sforceService, String objectName, String soslQuery, Boolean includesId, List<ObjectDataTypePropertyAPI> properties, ListFilterAPI listFilterAPI, Dictionary<String, Boolean> currencyFields)
        {
            SearchResult searchResult = null;
            SearchRecord searchRecord = null;
            ObjectAPI objectAPI = null;
            List<ObjectAPI> objectAPIs = null;
            Int32 order = 0;

            // Make the search call and get the search results
            searchResult = sforceService.search(soslQuery);

            // Process the query results
            if (searchResult != null &&
                searchResult.searchRecords != null &&
                searchResult.searchRecords.Length > 0)
            {
                objectAPIs = new List<ObjectAPI>();

                for (int x = 0; x < searchResult.searchRecords.Length; x++)
                {
                    // Check to see if we should start adding records
                    if (listFilterAPI.offset > x)
                    {
                        // Skip past the records
                        continue;
                    }

                    searchRecord = searchResult.searchRecords[x];

                    if (searchRecord.record != null)
                    {
                        String externalId = null;

                        objectAPI = new ObjectAPI();
                        objectAPI.developerName = objectName;
                        objectAPI.properties = new List<PropertyAPI>();
                        objectAPI.order = order++;

                        if (searchRecord.record.Any.Length > properties.Count)
                        {
                            throw new ArgumentNullException("ObjectData.Properties", "The list of properties being requested does not match the number of properties being returned by Salesforce.");
                        }

                        for (int y = 0; y < searchRecord.record.Any.Length; y++)
                        {
                            XmlElement element = searchRecord.record.Any[y];
                            PropertyAPI propertyAPI = new PropertyAPI();

                            // Do not rely on the element name as this has proven to be inconsistent from Salesforce - the search gives different
                            // field names from a standard select which confuses the binding logic
                            //propertyAPI.developerName = element.LocalName;

                            // This only works because the SOQL columns will have been generated from the ordered list of properties
                            // If the user has a final column of Id that's been added, we just keep the local name. The purpose of this
                            // is to preserve the deep name references that are difficult to resolve as they can be blank and have no
                            // child fields to do detection. This is much more explicit.
                            // Always use the name as defined by the binding properties as this will be consistent in all situations
                            propertyAPI.developerName = properties[y].developerName;

                            if (propertyAPI.developerName.EndsWith(".name", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                // If we're dealing with a compound field, we need to grab the text a little differently as it will sit inside                            
                                // additional XML
                                if (element.LastChild != null)
                                {
                                    propertyAPI.contentValue = element.LastChild.InnerText;
                                }
                            }
                            else
                            {
                                propertyAPI.contentValue = element.InnerText;
                            }

                            // Check to see if this is a currency field, if so, we need to change the formatting slightly
                            if (currencyFields != null &&
                                currencyFields.ContainsKey(propertyAPI.developerName) == true)
                            {
                                Double currencyValue = Double.MinValue;
                                // This is a currency field, so we need to improve the formatting so it's a proper number

                                // If the value parses OK, we convert it over, otherwise we leave it alone
                                if (Double.TryParse(propertyAPI.contentValue, out currencyValue) == true)
                                {
                                    propertyAPI.contentValue = currencyValue.ToString();
                                }
                            }

                            if (element.LocalName.Equals("Id", StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                objectAPI.externalId = propertyAPI.contentValue;

                                // Don't add the property to the object if the user hasn't asked for it
                                if (includesId == true)
                                {
                                    // Add the property to the new object
                                    objectAPI.properties.Add(propertyAPI);
                                }
                            }
                            else if (element.LocalName.Equals("ExternalId", StringComparison.InvariantCultureIgnoreCase) == true)
                            {
                                // We also grab the external id as the id will be blank for the new data integration stuff
                                externalId = propertyAPI.contentValue;

                                // Make sure we add the property also
                                objectAPI.properties.Add(propertyAPI);
                            }
                            else
                            {
                                objectAPI.properties.Add(propertyAPI);
                            }
                        }

                        // If we're using an x object, we use the external identifier
                        if (objectName.EndsWith("__x", StringComparison.InvariantCultureIgnoreCase) == true)
                        {
                            // If the external id is also null, we then throw an exception
                            if (externalId == null ||
                                externalId.Trim().Length == 0)
                            {
                                throw new ArgumentNullException("BadRequest", "An object identifier could not be found.");
                            }

                            objectAPI.externalId = externalId;
                        }

                        // Add the object to the response list
                        objectAPIs.Add(objectAPI);
                    }
                }
            }

            return objectAPIs;
        }

        private sObject CreateSObjectFromObjectAPI(ObjectAPI objectAPI)
        {
            XmlDocument fieldsXmlDoc = null;
            XmlElement[] fieldsElements = null;
            XmlElement fieldXmlElement = null;
            sObject saveObject = null;
            String idInObject = null;
            Int32 fieldCounter = 0;

            if (objectAPI != null &&
                objectAPI.properties != null &&
                objectAPI.properties.Count > 0)
            {
                saveObject = new sObject();
                saveObject.type = objectAPI.developerName;

                // Create the fields xml doc
                fieldsXmlDoc = new XmlDocument();

                fieldsElements = new XmlElement[objectAPI.properties.Count];

                foreach (var property in objectAPI.properties)
                {
                    // Create the xml element for this field
                    fieldXmlElement = fieldsXmlDoc.CreateElement(property.developerName);
                    fieldXmlElement.InnerText = property.contentValue;

                    // Add the field value to the object
                    fieldsElements[fieldCounter] = fieldXmlElement;

                    // Check to see if this property is an id, if so, grab it here and apply it to the root object
                    if (property.developerName.Equals("id", StringComparison.InvariantCultureIgnoreCase) == true)
                    {
                        idInObject = property.contentValue;
                    }

                    fieldCounter++;
                }

                saveObject.Any = fieldsElements;

                if (idInObject != null &&
                    idInObject.Trim().Length > 0 &&
                    (objectAPI.externalId == null ||
                     objectAPI.externalId.Trim().Length == 0))
                {
                    // If we have an id in the object properties and not object id assigned as the external id, then chances are, the user
                    // wants to update this particular object - so we assign the identifier for the save for them
                    saveObject.Id = idInObject;
                }
                else if (objectAPI.externalId != null &&
                         objectAPI.externalId.Trim().Length > 0)
                {
                    Guid test = Guid.Empty;

                    // If the id is a guid, it's not one of salesforce.com's!
                    if (Guid.TryParse(objectAPI.externalId, out test) == false)
                    {
                        saveObject.Id = objectAPI.externalId;
                    }

                    if (idInObject != null &&
                        idInObject.Trim().Length > 0 &&
                        idInObject.Equals(objectAPI.externalId, StringComparison.InvariantCultureIgnoreCase) == false)
                    {
                        // If the user has assigned the id property and that id does not match the external id reference for the same object
                        // then the user must be attempting to save an object to a different record, but has not done a clone operation.  We do
                        // not allow this currently, as it feels like it could introduce issues with debugging.  All the user needs to do is clone
                        // the object - which is an explicit action - and they can get the same result
                        throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError) { ReasonPhrase = "Id property does not match ExternalId for the object" });
                    }
                }
            }

            return saveObject;
        }

        public SforceService Login(IAuthenticatedWho authenticatedWho, List<EngineValueAPI> configurationValues, Boolean preferElevatedAccess, Boolean isModelingOperation)
        {
            SforceService sforceService = null;
            String authenticationStrategy = null;
            String authenticationUrl = null;
            String securityToken = null;
            String username = null;
            String password = null;

            if (authenticatedWho == null)
            {
                throw new ArgumentNullException("AuthenticatedWho", "The AuthenticatedWho object cannot be null.");
            }

            if (configurationValues == null ||
                configurationValues.Count == 0)
            {
                throw new ArgumentNullException("ConfigurationValues", "The ConfigurationValues cannot be null or empty.");
            }

            // Get the authentication strategy out
            authenticationStrategy = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_AUTHENTICATION_STRATEGY, configurationValues, false);

            // If we don't have an authentication strategy, we use a standard configuration
            if (String.IsNullOrWhiteSpace(authenticationStrategy) == true)
            {
                authenticationStrategy = SalesforceServiceSingleton.AUTHENTICATION_STRATEGY_STANDARD;
            }

            // This path should be used for all users when executing a workflow, unless this is a system operation that needs higher level
            // credentials
            if (isModelingOperation == true ||
                authenticationStrategy.Equals(SalesforceServiceSingleton.AUTHENTICATION_STRATEGY_SUPER_USER, StringComparison.OrdinalIgnoreCase) == true ||
                (preferElevatedAccess == true &&
                 authenticationStrategy.Equals(SalesforceServiceSingleton.AUTHENTICATION_STRATEGY_STANDARD, StringComparison.OrdinalIgnoreCase) == true))
            {
                // Grab the actual configuration values out
                authenticationUrl = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_AUTHENTICATION_URL, configurationValues, true);
                username = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_USERNAME, configurationValues, true);
                password = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_PASSWORD, configurationValues, true);
                securityToken = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_SECURITY_TOKEN, configurationValues, false);

                sforceService = this.LoginUsingCredentials(authenticationUrl, username, password, securityToken);
            }
            else if (authenticationStrategy.Equals(SalesforceServiceSingleton.AUTHENTICATION_STRATEGY_STANDARD, StringComparison.OrdinalIgnoreCase) == true ||
                     authenticationStrategy.Equals(SalesforceServiceSingleton.AUTHENTICATION_STRATEGY_ACTIVE_USER, StringComparison.OrdinalIgnoreCase) == true)
            {
                if (string.IsNullOrWhiteSpace(authenticatedWho.Token) == true ||
                    authenticatedWho.Token.Equals(ManyWhoConstants.AUTHENTICATED_USER_PUBLIC_TOKEN, StringComparison.OrdinalIgnoreCase) == true)
                {
                    if (authenticationStrategy.Equals(SalesforceServiceSingleton.AUTHENTICATION_STRATEGY_ACTIVE_USER, StringComparison.OrdinalIgnoreCase) == true &&
                        authenticatedWho.Token.IndexOf(SalesforceHttpUtils.TOKEN_PREFIX) < 0)
                    {
                        // Do nothing, we don't have their active session information to perform a login
                    }
                    else
                    {
                        throw new ArgumentNullException("SalesforceService", "The authentication token is null, empty, or incorrectly configured. If you are running using a PUBLIC authentication context, make sure you set your " + SalesforceServiceSingleton.SERVICE_VALUE_AUTHENTICATION_STRATEGY + " configuration value to 'SuperUser'.");
                    }
                }
                else
                {
                    // We should log the user in using their session information that's been provided via a previous explicit login
                    sforceService = this.LogUserInBasedOnSession(
                        SalesforceHttpUtils.GetAuthenticationDetails(authenticatedWho.Token).Token,
                        SalesforceHttpUtils.GetAuthenticationDetails(authenticatedWho.Token).PartnerUrl
                    );
                }
            }

            return sforceService;
        }

        public SforceService LogUserInBasedOnSession(String sessionToken, String sessionUrl)
        {
            SforceService sforceService = null;

            sforceService = new SforceService();
            sforceService.Timeout = 60000;
            sforceService.Url = sessionUrl;
            sforceService.SessionHeaderValue = new SessionHeader();
            sforceService.SessionHeaderValue.sessionId = sessionToken;

            return sforceService;
        }

        public SforceService LoginUsingCredentials(String authenticationUrl, String username, String password, String securityToken)
        {
            LoginResult loginResult = null;
            SforceService sforceService = null;

            sforceService = new SforceService();
            sforceService.Timeout = 60000;

            // The user is not required to provide a security token
            if (securityToken == null)
            {
                securityToken = "";
            }

            // Log the user into the main authentication endpoint
            sforceService.Url = authenticationUrl + "/services/Soap/u/35.0";
            loginResult = sforceService.login(username, password + securityToken);

            if (loginResult.passwordExpired)
            {
                throw new SoapException("The password for your salesforce account has expired", null);
            }

            // Grab the pod information back and give the service connection to the caller
            sforceService.Url = loginResult.serverUrl;
            sforceService.SessionHeaderValue = new SessionHeader();
            sforceService.SessionHeaderValue.sessionId = loginResult.sessionId;

            return sforceService;
        }

        private CleanedObjectDataTypeProperties CleanObjectDataTypeProperties(INotifier notifier, IAuthenticatedWho authenticatedWho, SforceService sforceService, String objectName, List<ObjectDataTypePropertyAPI> objectDataTypeProperties)
        {
            CleanedObjectDataTypeProperties cleanedObjectDataTypeProperties = null;
            Dictionary<String, Boolean> currencyFields = null;
            Dictionary<String, Boolean> dateTimeFields = null;
            Dictionary<String, Boolean> dateFields = null;
            Dictionary<String, Boolean> booleanFields = null;
            Dictionary<String, Boolean> numberFields = null;
            List<TypeElementPropertyBindingAPI> typeElementPropertyBindings = null;
            List<ObjectDataTypePropertyAPI> cleanObjectDataTypeProperties = null;
            DescribeSObjectResult describeSObjectResult = null;

            // Get the describe for the provided user and sforce service
            describeSObjectResult = this.GetDescribeResult(notifier, authenticatedWho, sforceService, objectName);

            if (describeSObjectResult == null)
            {
                throw new ArgumentNullException("DescribeSObjectResult", "The DescribeSObjectResult is null for: " + objectName);
            }

            // Get the type information for this user so we can reference that to ensure we're not looking up on a field that doesn't match
            typeElementPropertyBindings = this.DescribeFields(authenticatedWho, sforceService, describeSObjectResult, objectName);

            // Make sure we're actually getting some properties before doing anything more
            if (objectDataTypeProperties != null &&
                objectDataTypeProperties.Count > 0 &&
                typeElementPropertyBindings != null &&
                typeElementPropertyBindings.Count > 0)
            {
                cleanObjectDataTypeProperties = new List<ObjectDataTypePropertyAPI>();
                cleanedObjectDataTypeProperties = new CleanedObjectDataTypeProperties();
                currencyFields = new Dictionary<String, Boolean>();
                dateTimeFields = new Dictionary<String, Boolean>();
                booleanFields = new Dictionary<String, Boolean>();
                dateFields = new Dictionary<String, Boolean>();
                numberFields = new Dictionary<String, Boolean>();

                // First, go through the object data type properties one by one
                foreach (ObjectDataTypePropertyAPI objectDataTypeProperty in objectDataTypeProperties)
                {
                    // Now we go through the bindings that are actually available for this user based on their credentials
                    foreach (TypeElementPropertyBindingAPI typeElementPropertyBinding in typeElementPropertyBindings)
                    {
                        // If this property exists in the type information, then we add it to our list of cleaned properties
                        if (objectDataTypeProperty.developerName.Equals(typeElementPropertyBinding.databaseFieldName, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            cleanObjectDataTypeProperties.Add(objectDataTypeProperty);

                            // Now add the entry to our currency fields if this is one. For currency fields, we need to change the formatting
                            // as Salesforce sends them back with an "E" when using large numbers. We also check for other data types as this can
                            // affect how we perform SOQL queries
                            if (typeElementPropertyBinding.databaseContentType != null)
                            {
                                if (typeElementPropertyBinding.databaseContentType.Equals("currency", StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    // This is a currency field, so we add it to our table of currencies
                                    currencyFields.Add(typeElementPropertyBinding.databaseFieldName, true);
                                }
                                else if (typeElementPropertyBinding.databaseContentType.Equals("datetime", StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    // This is a date/time field, so we add it to our table of date times
                                    dateTimeFields.Add(typeElementPropertyBinding.databaseFieldName, true);
                                }
                                else if (typeElementPropertyBinding.databaseContentType.Equals("date", StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    // This is a date field, so we add it to our table of dates
                                    dateFields.Add(typeElementPropertyBinding.databaseFieldName, true);
                                }
                                else if (typeElementPropertyBinding.databaseContentType.Equals("boolean", StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    // This is a boolean field, so we add it to our table of booleans
                                    booleanFields.Add(typeElementPropertyBinding.databaseFieldName, true);
                                }
                                else if (typeElementPropertyBinding.databaseContentType.Equals("double", StringComparison.OrdinalIgnoreCase) == true ||
                                         typeElementPropertyBinding.databaseContentType.Equals("percent", StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    // This is a numeric field other than currency
                                    numberFields.Add(typeElementPropertyBinding.databaseFieldName, true);
                                }
                            }
                        }
                    }
                }
            }

            // Fill up the response object so we can properly return the data to manywho
            cleanedObjectDataTypeProperties.ObjectDataTypeProperties = cleanObjectDataTypeProperties;
            cleanedObjectDataTypeProperties.CurrencyFields = currencyFields;
            cleanedObjectDataTypeProperties.BooleanFields = booleanFields;
            cleanedObjectDataTypeProperties.DateTimeFields = dateTimeFields;
            cleanedObjectDataTypeProperties.DateFields = dateFields;
            cleanedObjectDataTypeProperties.NumberFields = numberFields;

            return cleanedObjectDataTypeProperties;
        }

        private DescribeSObjectResult GetDescribeResult(INotifier notifier, IAuthenticatedWho authenticatedWho, SforceService sforceService, String objectName)
        {
            DescribeSObjectResult describeSObjectResult = null;

            describeSObjectResult = sforceService.describeSObject(objectName);

            if (describeSObjectResult == null)
            {
                String errorMessage = "The object being referenced in the save does not exist! The name of the object in salesforce.com is: " + objectName;

                ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_WARNING, errorMessage);

                throw new ArgumentNullException("BadRequest", errorMessage);
            }

            if (describeSObjectResult.fields == null ||
                describeSObjectResult.fields.Length == 0)
            {
                String errorMessage = "The object being referenced does not have any fields! The name of the object in salesforce.com is: " + objectName;

                ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_WARNING, errorMessage);

                throw new ArgumentNullException("BadRequest", errorMessage);
            }

            return describeSObjectResult;
        }

        // string	String values.
        // boolean	Boolean (true / false) values.
        // int	Integer values.
        // double	Double values.
        // date	Date values.
        // datetime	Date and time values.
        // base64	Base64-encoded arbitrary binary data (of type base64Binary). Used for Attachment, Document, and Scontrol objects.
        // ID	Primary key field for the object. For information on IDs, see ID Field Type.
        // reference	Cross-references to a different object. Analogous to a foreign key field in SQL.
        // currency	Currency values.
        // textarea	String that is displayed as a multiline text field.
        // percent	Percentage values.
        // phone	Phone numbers. Values can include alphabetic characters. Client applications are responsible for phone number formatting.
        // url	URL values. Client applications should commonly display these as hyperlinks.
        // email	Email addresses.
        // combobox	Comboboxes, which provide a set of enumerated values and allow the user to specify a value not in the list.
        // picklist	Single-select picklists, which provide a set of enumerated values from which only one value can be selected.
        // multipicklist	multi-select picklists, which provide a set of enumerated values from which multiple values can be selected.
        // anyType	Values can be any of these types: string, picklist, boolean, int, double, percent, ID, date, dateTime, url, or email.
        // location	Location values, including latitude and longitude.
        private String TranslateToManyWhoContentType(String dataType)
        {
            String contentType = null;

            if ("string".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                "ID".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                "reference".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                "textarea".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                "phone".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                "url".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                "email".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                "combobox".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                "picklist".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                "multipicklist".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                "base64".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                "anyType".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                contentType = ManyWhoConstants.CONTENT_TYPE_STRING;
            }
            else if ("int".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                     "double".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                     "currency".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                     "percent".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                contentType = ManyWhoConstants.CONTENT_TYPE_NUMBER;
            }
            else if ("date".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                     "datetime".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true ||
                     "time".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                contentType = ManyWhoConstants.CONTENT_TYPE_DATETIME;
            }
            else if ("boolean".Equals(dataType, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                contentType = ManyWhoConstants.CONTENT_TYPE_BOOLEAN;
            }
            else
            {
                // TODO: there are a bunch of types we need to think about
                // binary
                // location
                // multiselect
                // date, datetime, time
                contentType = ManyWhoConstants.CONTENT_TYPE_STRING;
            }

            return contentType;
        }
    }

    public class CleanedObjectDataTypeProperties
    {
        public List<ObjectDataTypePropertyAPI> ObjectDataTypeProperties
        {
            get;
            set;
        }

        public Dictionary<String, Boolean> CurrencyFields
        {
            get;
            set;
        }

        public Dictionary<String, Boolean> DateTimeFields
        {
            get;
            set;
        }

        public Dictionary<String, Boolean> DateFields
        {
            get;
            set;
        }

        public Dictionary<String, Boolean> BooleanFields
        {
            get;
            set;
        }

        public Dictionary<String, Boolean> NumberFields
        {
            get;
            set;
        }
    }
}
