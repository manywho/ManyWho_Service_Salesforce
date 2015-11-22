using System;
using ManyWho.Service.Salesforce.Filters;
using ManyWho.Service.Salesforce.Middleware;
using ManyWho.Service.Salesforce.Utils;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ManyWho.Service.Salesforce
{
    public class Startup
    {
        public static IConfiguration Configuration { get; set; }

        public Startup(IHostingEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
                .AddCommandLine(Environment.GetCommandLineArgs())
                .AddIniFile($"config.{env.EnvironmentName.ToLower()}.ini")
                .Build();   
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<SettingUtils>();
            services.AddMvc(options =>
            {
                options.Filters.Add(new ExceptionFilter());
                options.OutputFormatters.RemoveType<StringOutputFormatter>();
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.Map("/plugins/api/salesforce/1/workflowrules/listener/{tenantId}", builder =>
            {
                builder.UseMiddleware<WorkflowRuleListenerMiddleware>();
            });

            app.Map("/plugins/api/salesforce/1/workflowrules/{tenantId}/{flowId}/{player}", builder =>
            {
                builder.UseMiddleware<WorkflowRuleMiddleware>();
            });

            app.UseIISPlatformHandler();
            app.UseMvc(routes =>
            {
                routes.MapWebApiRoute("PluginSalesforceHealthCheck", "plugins/api/salesforce/1/health", new
                {
                    controller = "PluginSalesforce",
                    action = "Health"
                }
                );

                // Email outcome click addition
                routes.MapWebApiRoute("PluginSalesforceTaskEmailOutcomeResponse", "api/email/outcomeresponse", new
                {
                    controller = "PluginSalesforce",
                    action = "TaskEmailOutcomeResponse"
                }
                );

                // Salesforce Plugin
                routes.MapWebApiRoute("PluginSalesforceWorkflowRuleListener", "plugins/api/salesforce/1/workflowrules/listener/{tenantId}",
                    constraints: null,
                    defaults: new
                    {
                        controller = "PluginSalesforce"
                    }
                );

                routes.MapWebApiRoute("PluginSalesforceWorkflowRule", "plugins/api/salesforce/1/workflowrules/{tenantId}/{flowId}/{player}",
                    constraints: null,
                    defaults: new
                    {
                        controller = "PluginSalesforce"
                    }
                );

                routes.MapWebApiRoute("PluginSalesforceCanvas", "plugins/api/salesforce/1/canvas/{tenantId}/{flowId}/{playerUrl}", new
                {
                    controller = "PluginSalesforce",
                    action = "Canvas"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceSessionSignIn", "plugins/api/salesforce/1/session/{tenantId}/{flowId}/{playerUrl}", new
                {
                    controller = "PluginSalesforce",
                    action = "SessionSignIn"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceDescribe", "plugins/api/salesforce/1/metadata", new
                {
                    controller = "PluginSalesforce",
                    action = "Describe"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceDescribeTables", "plugins/api/salesforce/1/metadata/table", new
                {
                    controller = "PluginSalesforce",
                    action = "DescribeTables"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceDescribeFields", "plugins/api/salesforce/1/metadata/field", new
                {
                    controller = "PluginSalesforce",
                    action = "DescribeFields"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceView", "plugins/api/salesforce/1/view/{actionName}", new
                {
                    controller = "PluginSalesforce",
                    action = "View"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceDelete", "plugins/api/salesforce/1/data/delete", new
                {
                    controller = "PluginSalesforce",
                    action = "Delete"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceGetUserInAuthorizationContext", "plugins/api/salesforce/1/authorization", new
                {
                    controller = "PluginSalesforce",
                    action = "GetUserInAuthorizationContext"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceLoadUserAttributes", "plugins/api/salesforce/1/authorization/user/attribute", new
                {
                    controller = "PluginSalesforce",
                    action = "LoadUserAttributes"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceLoadGroupAttributes", "plugins/api/salesforce/1/authorization/group/attribute", new
                {
                    controller = "PluginSalesforce",
                    action = "LoadGroupAttributes"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceLoadUsers", "plugins/api/salesforce/1/authorization/user", new
                {
                    controller = "PluginSalesforce",
                    action = "LoadUsers"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceLoadGroups", "plugins/api/salesforce/1/authorization/group", new
                {
                    controller = "PluginSalesforce",
                    action = "LoadGroups"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceLogin", "plugins/api/salesforce/1/authentication", new
                {
                    controller = "PluginSalesforce",
                    action = "Login"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceVote", "plugins/api/salesforce/1/vote", new
                    {
                        controller = "PluginSalesforce",
                        action = "Vote"
                    }
                );

                routes.MapWebApiRoute("PluginSalesforceListener", "plugins/api/salesforce/1/listener", new
                    {
                        controller = "PluginSalesforce",
                        action = "Listener"
                    }
                );

                routes.MapWebApiRoute("PluginSalesforceNotification", "plugins/api/salesforce/1/notification", new
                    {
                        controller = "PluginSalesforce",
                        action = "Notification"
                    }
                );

                routes.MapWebApiRoute("PluginSalesforceLoad", "plugins/api/salesforce/1/data", new
                    {
                        controller = "PluginSalesforce",
                        action = "Load"
                    }
                );

                routes.MapWebApiRoute("PluginSalesforceSave", "plugins/api/salesforce/1/data", new
                    {
                        controller = "PluginSalesforce",
                        action = "Save"
                    }
                );

                routes.MapWebApiRoute("PluginSalesforceLoadFiles", "plugins/api/salesforce/1/file", new
                    {
                        controller = "PluginSalesforce",
                        action = "LoadFiles"
                    }
                );

                routes.MapWebApiRoute("PluginSalesforceUploadFile", "plugins/api/salesforce/1/file/content", new
                    {
                        controller = "PluginSalesforce",
                        action = "UploadFile"
                    }
                );

                routes.MapWebApiRoute("PluginSalesforceDeleteFile", "plugins/api/salesforce/1/file/delete", new
                    {
                        controller = "PluginSalesforce",
                        action = "DeleteFile"
                    }
                );

                routes.MapWebApiRoute("PluginSalesforceGetCurrentUserInfo", "plugins/api/salesforce/1/social/stream/{streamId}/user/me", new
                    {
                        controller = "PluginSalesforce",
                        action = "GetCurrentUserInfo"
                    }
                );

                routes.MapWebApiRoute("PluginSalesforceSearchUsersByName", "plugins/api/salesforce/1/social/stream/{streamId}/user/name/{name}", new
                {
                    controller = "PluginSalesforce",
                    action = "SearchUsersByName"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceGetUserInfo", "plugins/api/salesforce/1/social/stream/{streamId}/user/{userId}", new
                    {
                        controller = "PluginSalesforce",
                        action = "GetUserInfo"
                    }
                );

                routes.MapWebApiRoute("PluginSalesforceGetStreamFollowers", "plugins/api/salesforce/1/social/stream/{streamId}/follower", new
                {
                    controller = "PluginSalesforce",
                    action = "GetStreamFollowers"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceShareMessage", "plugins/api/salesforce/1/social/stream/{streamId}/share", new
                {
                    controller = "PluginSalesforce",
                    action = "ShareMessage"
                }
                );

                routes.MapWebApiRoute("PluginSalesforcePostNewMessage", "plugins/api/salesforce/1/social/stream/{streamId}/message", new
                {
                    controller = "PluginSalesforce",
                    action = "PostNewMessage"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceLikeMessage", "plugins/api/salesforce/1/social/stream/{streamId}/message/{messageId}/like/{like}", new
                {
                    controller = "PluginSalesforce",
                    action = "LikeMessage"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceDeleteMessage", "plugins/api/salesforce/1/social/stream/{streamId}/message/{messageId}", new
                {
                    controller = "Social",
                    action = "PluginSalesforce"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceFollowStream", "plugins/api/salesforce/1/social/stream/{streamId}/follow/{follow}", new
                {
                    controller = "PluginSalesforce",
                    action = "FollowStream"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceCreateStream", "plugins/api/salesforce/1/social/stream", new
                    {
                        controller = "PluginSalesforce",
                        action = "CreateStream"
                    }
                );

                routes.MapWebApiRoute("PluginSalesforceGetStreamMessages", "plugins/api/salesforce/1/social/stream/{streamId}", new
                {
                    controller = "PluginSalesforce",
                    action = "GetStreamMessages"
                }
                );

                routes.MapWebApiRoute("PluginSalesforceInvoke", "plugins/api/salesforce/1/{actionName}", new
                {
                    controller = "PluginSalesforce",
                    action = "Invoke"
                }
                );
            });
        }

        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
