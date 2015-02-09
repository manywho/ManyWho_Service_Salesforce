using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Runtime.Remoting.Messaging;
using System.Configuration;
using ManyWho.Flow.SDK.Utils;
using ManyWho.Flow.SDK.Security;
using ManyWho.Flow.SDK.Run;

namespace ManyWho.Service.Salesforce.Utils
{
    public class EmailNotifier : INotifier
    {
        private delegate void AsyncMethodCaller(EmailNotifier emailNotifier);

        public EmailNotifier(String alertEmail)
        {
            this.Email = alertEmail;
        }

        public EmailNotifier(IAuthenticatedWho receivingAuthenticatedWho)
        {
            this.ReceivingAuthenticatedWho = receivingAuthenticatedWho;
            this.NotificationMessages = new List<INotificationMessage>();
            this.LogEntries = new List<String>();
        }

        public List<INotificationMessage> NotificationMessages
        {
            get;
            set;
        }

        private String Email
        {
            get;
            set;
        }

        public IAuthenticatedWho ReceivingAuthenticatedWho
        {
            get;
            set;
        }

        public String Description
        {
            get;
            set;
        }

        public String Reason
        {
            get;
            set;
        }

        private List<String> LogEntries
        {
            get;
            set;
        }

        public void AddLogEntry(String logEntry)
        {
            if (this.LogEntries == null)
            {
                this.LogEntries = new List<String>();
            }

            this.LogEntries.Add(logEntry);
        }

        public void AddNotificationMessage(String mediaType, String message)
        {
            if (String.IsNullOrWhiteSpace(mediaType) == true)
            {
                throw new ArgumentNullException("MediaType", "The MediaType cannot be null or blank.");
            }

            if (String.IsNullOrWhiteSpace(message) == true)
            {
                throw new ArgumentNullException("Message", "The Message cannot be null or blank.");
            }

            this.NotificationMessages.Add(new NotificationMessage() { MediaType = mediaType, Message = message });
        }

        public void SendNotification()
        {
            this.SendNotification(this.ReceivingAuthenticatedWho);
        }

        public void SendNotification(IAuthenticatedWho receivingAuthenticatedWho, String reason, String mediaType, String message)
        {
            this.Reason = reason;
            this.AddNotificationMessage(mediaType, message);
            this.SendNotification(receivingAuthenticatedWho);
        }

        public void SendNotification(IAuthenticatedWho receivingAuthenticatedWho)
        {
            //AsyncMethodCaller caller = null;
            //AsyncCallback callbackHandler = null;

            if (receivingAuthenticatedWho == null)
            {
                throw new ArgumentNullException("ReceivingAuthenticatedWho", "The ReceivingAuthenticatedWho cannot be null.");
            }

            if (String.IsNullOrWhiteSpace(receivingAuthenticatedWho.Email) == true)
            {
                throw new ArgumentNullException("ReceivingAuthenticatedWho.Email", "The ReceivingAuthenticatedWho.Email property value cannot be null or blank.");
            }

            // Set the receiving authenticated who to this user
            this.ReceivingAuthenticatedWho = receivingAuthenticatedWho;

            // Check to make sure we do in fact have messages to send
            if (this.NotificationMessages != null &&
                this.NotificationMessages.Count > 0)
            {
                //// Set the callback handler
                //callbackHandler = new AsyncCallback(AsyncCallback);

                //// Invoke the message asynchronously
                //caller = new AsyncMethodCaller(SendNotificationsInSeparateThread);
                //caller.BeginInvoke(this, callbackHandler, null);
                SendNotificationsInSeparateThread(this);
            }
        }

        private void SendNotificationsInSeparateThread(EmailNotifier emailNotifier)
        {
            NetworkCredential networkCredentials = null;
            MailMessage mailMessage = null;
            SmtpClient smtpClient = null;

            try
            {
                // Create the main email message
                mailMessage = new MailMessage();
                mailMessage.To.Add(new MailAddress(
                    this.ReceivingAuthenticatedWho.Email,
                    this.ReceivingAuthenticatedWho.FirstName + " " + this.ReceivingAuthenticatedWho.LastName
                ));
                mailMessage.From = new MailAddress(
                    ConfigurationManager.AppSettings.Get("ManyWho.SendAlertFromEmail"),
                    ConfigurationManager.AppSettings.Get("ManyWho.SendAlertFromEmail")
                );

                // We apply the "reason" as the subject
                mailMessage.Subject = emailNotifier.Reason;

                // Go through each of the notifications and apply them to the mail alternative views
                foreach (NotificationMessage notificationMessage in this.NotificationMessages)
                {
                    // Check to see if we have any log entries to add to the notification
                    if (this.LogEntries != null &&
                        this.LogEntries.Count > 0)
                    {
                        foreach (String logEntry in this.LogEntries)
                        {
                            // Check the media type to see how best to render the log entries
                            if (notificationMessage.MediaType.Equals(NotificationUtils.MEDIA_TYPE_HTML, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                notificationMessage.Message += logEntry + "<br/>";
                            }
                            else
                            {
                                notificationMessage.Message += logEntry + Environment.NewLine;
                            }
                        }
                    }

                    // Create the message in our mail system
                    mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(notificationMessage.Message, null, notificationMessage.MediaType));
                }

                // Get the network credentials for the email account we're going to send the notification through
                networkCredentials = new NetworkCredential(
                    ConfigurationManager.AppSettings.Get("ManyWho.SendGrid.Username"),
                    ConfigurationManager.AppSettings.Get("ManyWho.SendGrid.Password")
                );

                // Create the smtp client using SSL only
                smtpClient = new SmtpClient(
                    ConfigurationManager.AppSettings.Get("ManyWho.SendGrid.SMTP"),
                    Convert.ToInt32(587)
                );
                smtpClient.EnableSsl = true;
                smtpClient.Credentials = networkCredentials;

                // Send ye olde message
                smtpClient.Send(mailMessage);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                // Clear the list of messages with a new list ready for the next notification to be sent
                this.NotificationMessages = new List<INotificationMessage>();
                this.LogEntries = new List<String>();
            }
        }

        private void AsyncCallback(IAsyncResult asyncResult)
        {
            AsyncMethodCaller caller = null;
            AsyncResult result = null;

            try
            {
                // Cast the interface to the implementation
                result = (AsyncResult)asyncResult;

                caller = (AsyncMethodCaller)result.AsyncDelegate;
                caller.EndInvoke(asyncResult);
            }
            catch (Exception)
            {
                // Do nothing as this is our notifier
            }
        }

        public static INotifier GetInstance(IAuthenticatedWho authenticatedWho, List<EngineValueAPI> configurationValues, String codeReference)
        {
            return GetInstance(null, authenticatedWho, configurationValues, codeReference);
        }

        public static INotifier GetInstance(String tenantId, IAuthenticatedWho authenticatedWho, List<EngineValueAPI> configurationValues, String codeReference)
        {
            INotifier notifier = null;
            String email = "";

            if (configurationValues != null &&
                configurationValues.Count > 0)
            {
                email = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_ADMIN_EMAIL, configurationValues, false);
            }

            if (String.IsNullOrEmpty(authenticatedWho.Email) == true)
            {
                // TODO: This is a hack for the notification as we only use the email
                authenticatedWho.Email = email;
            }

            // Create the notifier for the caller
            notifier = new EmailNotifier(authenticatedWho);
            notifier.Reason = codeReference;

            return notifier;
        }
    }
}