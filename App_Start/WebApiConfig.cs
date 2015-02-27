using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace ManyWho.Service.Salesforce
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "PluginSalesforceHealthCheck",
                routeTemplate: "plugins/api/salesforce/1/health",
                constraints: new { httpMethod = new System.Web.Http.Routing.HttpMethodConstraint(HttpMethod.Get) },
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "Health"
                }
            );

            // Email outcome click addition
            config.Routes.MapHttpRoute(
                name: "PluginSalesforceTaskEmailOutcomeResponse",
                routeTemplate: "api/email/outcomeresponse",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "TaskEmailOutcomeResponse",
                    token = RouteParameter.Optional,
                    selectedOutcomeId = RouteParameter.Optional
                }
            );

            // Salesforce Plugin
            config.Routes.MapHttpRoute(
                name: "PluginSalesforceWorkflowRuleListener",
                routeTemplate: "plugins/api/salesforce/1/workflowrules/listener/{tenantId}",
                constraints: null,
                defaults: new
                {
                    controller = "PluginSalesforce",
                    tenantId = RouteParameter.Optional,
                    mode = RouteParameter.Optional,
                    email = RouteParameter.Optional
                },
                handler: new WorkflowRuleListenerMessageHandler() // assign our handler to intercept incoming POST request
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceWorkflowRule",
                routeTemplate: "plugins/api/salesforce/1/workflowrules/{tenantId}/{flowId}/{player}",
                constraints: null,
                defaults: new
                {
                    controller = "PluginSalesforce",
                    tenantId = RouteParameter.Optional,
                    flowId = RouteParameter.Optional,
                    player = RouteParameter.Optional,
                    mode = RouteParameter.Optional,
                    email = RouteParameter.Optional
                },
                handler: new WorkflowRuleMessageHandler() // assign our handler to intercept incoming POST request
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceCanvas",
                routeTemplate: "plugins/api/salesforce/1/canvas/{tenantId}/{flowId}/{playerUrl}",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "Canvas",
                    tenantId = RouteParameter.Optional,
                    flowId = RouteParameter.Optional,
                    playerUrl = RouteParameter.Optional,
                    sessionId = RouteParameter.Optional,
                    sessionUrl = RouteParameter.Optional
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceSessionSignIn",
                routeTemplate: "plugins/api/salesforce/1/session/{tenantId}/{flowId}/{playerUrl}",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "SessionSignIn",
                    tenantId = RouteParameter.Optional,
                    flowId = RouteParameter.Optional,
                    playerUrl = RouteParameter.Optional,
                    sessionId = RouteParameter.Optional,
                    sessionUrl = RouteParameter.Optional
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceDescribe",
                routeTemplate: "plugins/api/salesforce/1/metadata",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "Describe"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceDescribeTables",
                routeTemplate: "plugins/api/salesforce/1/metadata/table",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "DescribeTables"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceDescribeFields",
                routeTemplate: "plugins/api/salesforce/1/metadata/field",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "DescribeFields"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceView",
                routeTemplate: "plugins/api/salesforce/1/view/{actionName}",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "View",
                    actionName = RouteParameter.Optional
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceDelete",
                routeTemplate: "plugins/api/salesforce/1/data/delete",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "Delete"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceGetUserInAuthorizationContext",
                routeTemplate: "plugins/api/salesforce/1/authorization",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "GetUserInAuthorizationContext"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceLoadUserAttributes",
                routeTemplate: "plugins/api/salesforce/1/authorization/user/attribute",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "LoadUserAttributes"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceLoadGroupAttributes",
                routeTemplate: "plugins/api/salesforce/1/authorization/group/attribute",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "LoadGroupAttributes"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceLoadUsers",
                routeTemplate: "plugins/api/salesforce/1/authorization/user",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "LoadUsers"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceLoadGroups",
                routeTemplate: "plugins/api/salesforce/1/authorization/group",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "LoadGroups"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceLogin",
                routeTemplate: "plugins/api/salesforce/1/authentication",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "Login"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceVote",
                constraints: new { httpMethod = new System.Web.Http.Routing.HttpMethodConstraint(HttpMethod.Post) },
                routeTemplate: "plugins/api/salesforce/1/vote",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "Vote"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceListener",
                constraints: new { httpMethod = new System.Web.Http.Routing.HttpMethodConstraint(HttpMethod.Post) },
                routeTemplate: "plugins/api/salesforce/1/listener",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "Listener"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceNotification",
                constraints: new { httpMethod = new System.Web.Http.Routing.HttpMethodConstraint(HttpMethod.Post) },
                routeTemplate: "plugins/api/salesforce/1/notification",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "Notification"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceLoad",
                constraints: new { httpMethod = new System.Web.Http.Routing.HttpMethodConstraint(HttpMethod.Post) },
                routeTemplate: "plugins/api/salesforce/1/data",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "Load"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceSave",
                constraints: new { httpMethod = new System.Web.Http.Routing.HttpMethodConstraint(HttpMethod.Put) },
                routeTemplate: "plugins/api/salesforce/1/data",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "Save"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceLoadFiles",
                constraints: new { httpMethod = new System.Web.Http.Routing.HttpMethodConstraint(HttpMethod.Post) },
                routeTemplate: "plugins/api/salesforce/1/file",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "LoadFiles"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceUploadFile",
                constraints: new { httpMethod = new System.Web.Http.Routing.HttpMethodConstraint(HttpMethod.Post) },
                routeTemplate: "plugins/api/salesforce/1/file/content",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "UploadFile",
                    fileId = RouteParameter.Optional
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceDeleteFile",
                constraints: new { httpMethod = new System.Web.Http.Routing.HttpMethodConstraint(HttpMethod.Post) },
                routeTemplate: "plugins/api/salesforce/1/file/delete",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "DeleteFile"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceGetCurrentUserInfo",
                constraints: new { httpMethod = new System.Web.Http.Routing.HttpMethodConstraint(HttpMethod.Post) },
                routeTemplate: "plugins/api/salesforce/1/social/stream/{streamId}/user/me",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "GetCurrentUserInfo"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceSearchUsersByName",
                routeTemplate: "plugins/api/salesforce/1/social/stream/{streamId}/user/name/{name}",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "SearchUsersByName",
                    streamId = RouteParameter.Optional,
                    name = RouteParameter.Optional
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceGetUserInfo",
                constraints: new { httpMethod = new System.Web.Http.Routing.HttpMethodConstraint(HttpMethod.Post) },
                routeTemplate: "plugins/api/salesforce/1/social/stream/{streamId}/user/{userId}",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "GetUserInfo"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceGetStreamFollowers",
                routeTemplate: "plugins/api/salesforce/1/social/stream/{streamId}/follower",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "GetStreamFollowers",
                    streamId = RouteParameter.Optional
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceShareMessage",
                routeTemplate: "plugins/api/salesforce/1/social/stream/{streamId}/share",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "ShareMessage",
                    streamId = RouteParameter.Optional
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforcePostNewMessage",
                routeTemplate: "plugins/api/salesforce/1/social/stream/{streamId}/message",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "PostNewMessage",
                    streamId = RouteParameter.Optional
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceLikeMessage",
                routeTemplate: "plugins/api/salesforce/1/social/stream/{streamId}/message/{messageId}/like/{like}",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "LikeMessage",
                    streamId = RouteParameter.Optional,
                    messageId = RouteParameter.Optional,
                    like = RouteParameter.Optional
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceDeleteMessage",
                routeTemplate: "plugins/api/salesforce/1/social/stream/{streamId}/message/{messageId}",
                defaults: new
                {
                    controller = "Social",
                    action = "PluginSalesforce",
                    streamId = RouteParameter.Optional,
                    messageId = RouteParameter.Optional
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceFollowStream",
                routeTemplate: "plugins/api/salesforce/1/social/stream/{streamId}/follow/{follow}",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "FollowStream",
                    streamId = RouteParameter.Optional,
                    follow = RouteParameter.Optional
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceCreateStream",
                constraints: new { httpMethod = new System.Web.Http.Routing.HttpMethodConstraint(HttpMethod.Post) },
                routeTemplate: "plugins/api/salesforce/1/social/stream",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "CreateStream"
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceGetStreamMessages",
                routeTemplate: "plugins/api/salesforce/1/social/stream/{streamId}",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "GetStreamMessages",
                    streamId = RouteParameter.Optional
                }
            );

            config.Routes.MapHttpRoute(
                name: "PluginSalesforceInvoke",
                routeTemplate: "plugins/api/salesforce/1/{actionName}",
                defaults: new
                {
                    controller = "PluginSalesforce",
                    action = "Invoke",
                    actionName = RouteParameter.Optional
                }
            );

            // Make JSON the default format for the service
            var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault(t => t.MediaType == "application/xml");
            config.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);

            // Uncomment the following line of code to enable query support for actions with an IQueryable or IQueryable<T> return type.
            // To avoid processing unexpected or malicious queries, use the validation settings on QueryableAttribute to validate incoming queries.
            // For more information, visit http://go.microsoft.com/fwlink/?LinkId=279712.
            //config.EnableQuerySupport();

            // To disable tracing in your application, please comment out or remove the following line of code
            // For more information, refer to: http://www.asp.net/web-api
            //config.EnableSystemDiagnosticsTracing();
        }
    }
}
