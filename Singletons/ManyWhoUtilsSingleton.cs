using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Net.Mail;
using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Utils;
using ManyWho.Flow.SDK.Security;
using ManyWho.Flow.SDK.Run;
using ManyWho.Flow.SDK.Run.Elements.Config;
using ManyWho.Flow.SDK.Run.Elements.Map;
using ManyWho.Service.Salesforce;
using ManyWho.Service.Salesforce.Singletons;
using ManyWho.Service.Salesforce.Utils;
using ManyWho.Service.Salesforce.Salesforce;

namespace ManyWho.Service.ManyWho.Utils.Singletons
{
    public class ManyWhoUtilsSingleton
    {
        public const String APP_SETTING_DEFAULT_FROM_EMAIL = "Default From Email";
        public const String APP_SETTING_EMAIL_ACCOUNT_USERNAME = "Email Username";
        public const String APP_SETTING_EMAIL_ACCOUNT_PASSWORD = "Email Password";
        public const String APP_SETTING_EMAIL_ACCOUNT_SMTP = "Email SMTP";

        public const String SERVICE_VALUE_TO_EMAIL = "To Email";
        public const String SERVICE_VALUE_FROM_EMAIL = "From Email";
        public const String SERVICE_VALUE_SUBJECT = "Subject";
        public const String SERVICE_VALUE_HTML_BODY = "Html Body";
        public const String SERVICE_VALUE_TEXT_BODY = "Text Body";
        public const String SERVICE_VALUE_ATTACHMENT_HTML = "Attachment Html";
        public const String SERVICE_VALUE_REDIRECT_URI = "Redirect Uri";
        public const String SERVICE_VALUE_INCLUDE_OUTCOMES_AS_BUTTONS = "Include Outcomes As Buttons";

        private static ManyWhoUtilsSingleton manyWhoUtilsSingleton = null;

        private ManyWhoUtilsSingleton()
        {

        }

        public static ManyWhoUtilsSingleton GetInstance()
        {
            if (manyWhoUtilsSingleton == null)
            {
                manyWhoUtilsSingleton = new ManyWhoUtilsSingleton();
            }

            return manyWhoUtilsSingleton;
        }

        public Boolean SendEmail(INotifier notifier, IAuthenticatedWho authenticatedWho, ServiceRequestAPI serviceRequest, Boolean includeTracking)
        {
            SforceService sforceService = null;
            List<String> toEmails = null;
            Boolean includeOutcomesAsButtons = true;
            String includeOutcomesAsButtonsString = null;
            String toEmail = null;
            String fromEmail = null;
            String subject = null;
            String textBody = null;
            String htmlBody = null;
            String authenticationUrl = null;
            String username = null;
            String password = null;
            String securityToken = null;
            String redirectUri = null;

            // Get the configuration information for salesforce
            authenticationUrl = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_AUTHENTICATION_URL, serviceRequest.configurationValues, true);
            username = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_USERNAME, serviceRequest.configurationValues, true);
            password = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_PASSWORD, serviceRequest.configurationValues, true);
            securityToken = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_SECURITY_TOKEN, serviceRequest.configurationValues, false);

            // Get the values from the incoming request
            toEmail = ValueUtils.GetContentValue(SERVICE_VALUE_TO_EMAIL, serviceRequest.inputs, false);
            fromEmail = ValueUtils.GetContentValue(SERVICE_VALUE_FROM_EMAIL, serviceRequest.inputs, false);
            subject = ValueUtils.GetContentValue(SERVICE_VALUE_SUBJECT, serviceRequest.inputs, true);
            textBody = ValueUtils.GetContentValue(SERVICE_VALUE_TEXT_BODY, serviceRequest.inputs, false);
            htmlBody = ValueUtils.GetContentValue(SERVICE_VALUE_HTML_BODY, serviceRequest.inputs, false);
            redirectUri = ValueUtils.GetContentValue(SERVICE_VALUE_REDIRECT_URI, serviceRequest.inputs, false);
            includeOutcomesAsButtonsString = ValueUtils.GetContentValue(SERVICE_VALUE_INCLUDE_OUTCOMES_AS_BUTTONS, serviceRequest.inputs, false);

            // Create the to emails list
            toEmails = new List<String>();

            // Check to see if we have a value for including outcome buttons
            if (String.IsNullOrWhiteSpace(includeOutcomesAsButtonsString) == true)
            {
                // Default is true
                includeOutcomesAsButtons = true;
            }
            else
            {
                includeOutcomesAsButtons = Boolean.Parse(includeOutcomesAsButtonsString);
            }

            if (String.IsNullOrWhiteSpace(toEmail) == true)
            {
                if (serviceRequest.authorization == null)
                {
                    throw new ArgumentNullException("ServiceRequest.Authorization", "The ServiceRequest.Authorization property cannot be null as we will not know who to send the email to.");
                }

                if (serviceRequest.authorization.groups == null ||
                    serviceRequest.authorization.groups.Count == 0)
                {
                    throw new ArgumentNullException("ServiceRequest.Authorization.Groups", "The ServiceRequest.Authorization.Groups property cannot be null or empty as we will not know who to send the email to.");
                }

                if (serviceRequest.authorization.groups.Count > 1)
                {
                    throw new ArgumentNullException("ServiceRequest.Authorization.Groups", "The ServiceRequest.Authorization.Groups property cannot contain more than one group currently.");
                }

                // We need to get the users from salesforce
                sforceService = SalesforceDataSingleton.GetInstance().Login(authenticatedWho, serviceRequest.configurationValues, true, false);

                if (sforceService == null)
                {
                    throw new ArgumentNullException("SalesforceService", "Unable to log into Salesforce.");
                }

                // Get the to emails from Salesforce
                toEmails = SalesforceAuthenticationSingleton.GetInstance().GetGroupMemberEmails(notifier, sforceService, serviceRequest, serviceRequest.authorization.groups[0].authenticationId);

                if (toEmails == null ||
                    toEmails.Count == 0)
                {
                    throw new ArgumentNullException("ServiceRequest.Authorization.Groups", "The ServiceRequest.Authorization.Groups configuration is not returning any users to send the email to.");
                }
            }
            else
            {
                // The user is explicitly setting the to email
                toEmails.Add(toEmail);
            }
            
            if (includeOutcomesAsButtons == false)
            {
                // Null out any outcomes so we don't send them through
                serviceRequest.outcomes = null;
            }

            // Send the actual email
            this.SendEmail(serviceRequest.configurationValues, fromEmail, toEmails.ToArray(), null, subject, textBody, htmlBody, serviceRequest.token, redirectUri, serviceRequest.outcomes);

            return includeOutcomesAsButtons;
        }

        public void SendEmail(List<EngineValueAPI> configurationValues, String fromEmail, String[] toEmails, String[] bccEmails, String subject, String textBody, String htmlBody, String token, String redirectUri, List<OutcomeAvailableAPI> outcomes)
        {
            NetworkCredential credentials = null;
            SmtpClient smtpClient = null;
            MailMessage mailMsg = null;
            String fullHtmlBody = null;
            String defaultEmail = null;
            String emailUsername = null;
            String emailPassword = null;
            String emailSmtp = null;

            // Get the configuration information for the email
            defaultEmail = ValueUtils.GetContentValue(ManyWhoUtilsSingleton.APP_SETTING_DEFAULT_FROM_EMAIL, configurationValues, true);
            emailUsername = ValueUtils.GetContentValue(ManyWhoUtilsSingleton.APP_SETTING_EMAIL_ACCOUNT_USERNAME, configurationValues, true);
            emailPassword = ValueUtils.GetContentValue(ManyWhoUtilsSingleton.APP_SETTING_EMAIL_ACCOUNT_PASSWORD, configurationValues, true);
            emailSmtp = ValueUtils.GetContentValue(ManyWhoUtilsSingleton.APP_SETTING_EMAIL_ACCOUNT_SMTP, configurationValues, true);

            // If we don't have a from email, use the "donotreply" address for manywho
            if (String.IsNullOrWhiteSpace(fromEmail) == true)
            {
                fromEmail = defaultEmail;
            }

            // If we don't have a redirection uri, we make it "blank"
            if (String.IsNullOrWhiteSpace(redirectUri) == true)
            {
                redirectUri = "";
            }

            // Construct the email using the provided values
            mailMsg = new MailMessage();

            if (toEmails != null && toEmails.Length > 0)
            {
                // Add each of the emails to the "TO" part
                foreach (String toEmailEntry in toEmails)
                {
                    mailMsg.To.Add(new MailAddress(toEmailEntry, toEmailEntry));
                }
            }

            if (bccEmails != null && bccEmails.Length > 0)
            {
                // Add each of the emails to the "TO" part
                foreach (String bccEmailEntry in bccEmails)
                {
                    mailMsg.Bcc.Add(new MailAddress(bccEmailEntry, bccEmailEntry));
                }
            }

            mailMsg.From = new MailAddress(fromEmail, fromEmail);
            mailMsg.Subject = subject;

            if (textBody != null &&
                textBody.Trim().Length > 0)
            {
                if (outcomes != null &&
                    outcomes.Count > 0)
                {
                    foreach (OutcomeAvailableAPI outcome in outcomes)
                    {
                        if (outcome.id.Equals(ManyWhoConstants.FAULT_GUID.ToString(), StringComparison.OrdinalIgnoreCase) == false &&
                            string.IsNullOrWhiteSpace(outcome.label) == false)
                        {
                            textBody += outcome.label + "(Click here: " + SettingUtils.GetStringSetting("Salesforce.ServerBasePath") + "/api/email/outcomeresponse?token=" + token + "&selectedOutcomeId=" + outcome.id + "&redirectUri=" + Uri.EscapeUriString(redirectUri) + ")" + Environment.NewLine;
                        }
                    }
                }

                mailMsg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, null, MediaTypeNames.Text.Plain));
            }

            if (htmlBody != null &&
                htmlBody.Trim().Length > 0)
            {
                fullHtmlBody = "";
                fullHtmlBody += "<html>";
                fullHtmlBody += "<head>";
                fullHtmlBody += "<script type=\"text/javascript\" src=\"https://assets.manywho.com/js/vendor/bootstrap-3.3.6.min.js\"></script>";
                fullHtmlBody += "<link rel=\"stylesheet\" href=\"https://assets.manywho.com/css/mw-bootstrap.ae817fbf.css\">";
                fullHtmlBody += "<link rel=\"stylesheet\" href=\"https://assets.manywho.com/css/themes/mw-sf1.css\">";
                fullHtmlBody += "</head>";
                fullHtmlBody += "<body class=\"mw-bs\">";
                fullHtmlBody += "<div class=\"container-fluid\">";
                fullHtmlBody += "<div class=\"row\">";
                fullHtmlBody += "<div class=\"col-md-12\">";
                fullHtmlBody += htmlBody;
                fullHtmlBody += "</div>";
                fullHtmlBody += "</div>";

                if (outcomes != null &&
                    outcomes.Count > 0)
                {
                    // The user has outcomes that should be used for actions - sort them before doing anything else
                    outcomes.Sort();

                    fullHtmlBody += "<div class=\"row\">";
                    fullHtmlBody += "<div class=\"col-md-12\">";

                    foreach (OutcomeAvailableAPI outcome in outcomes)
                    {
                        if (outcome.id.Equals(ManyWhoConstants.FAULT_GUID.ToString(), StringComparison.OrdinalIgnoreCase) == false &&
                            string.IsNullOrWhiteSpace(outcome.label) == false)
                        {
                            fullHtmlBody += "<a href=\"" + SettingUtils.GetStringSetting("Salesforce.ServerBasePath") + "/api/email/outcomeresponse?token=" + token + "&selectedOutcomeId=" + outcome.id + "&redirectUri=" + Uri.EscapeUriString(redirectUri) + "\" class=\"btn btn-primary\">" + outcome.label + "</a>&nbsp;";
                        }
                    }

                    fullHtmlBody += "</div>";
                    fullHtmlBody += "</div>";
                }

                fullHtmlBody += "</div>";
                fullHtmlBody += "</body>";
                fullHtmlBody += "</html>";

                mailMsg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(fullHtmlBody, null, MediaTypeNames.Text.Html));
            }

            credentials = new NetworkCredential(emailUsername, emailPassword);

            smtpClient = new SmtpClient(emailSmtp, Convert.ToInt32(587));
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = credentials;
            smtpClient.Send(mailMsg);
        }
    }
}