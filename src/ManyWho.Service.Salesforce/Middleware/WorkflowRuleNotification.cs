using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ManyWho.Flow.SDK.Utils;
using ManyWho.Service.Salesforce.Utils;
using Microsoft.AspNet.Http;

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

namespace ManyWho.Service.Salesforce.Middleware
{
    public class WorkflowRuleNotification
    {
        public List<String> NotificationIDs { get; set; } //salesforce can send upto 100 notifications in a single soap request
        public List<String> ObjectIDs { get; set; } //salesforce can send upto 100 notifications, ergo 100 record ids can exist in a single soap request
        public string SessionID { get; set; }
        public string SessionURL { get; set; }
        public string ObjectName { get; set; }

        public WorkflowRuleNotification()
        {
            this.NotificationIDs = new List<String>();
            this.ObjectIDs = new List<String>();
            this.SessionID = String.Empty;
            this.ObjectName = String.Empty;
            this.SessionURL = String.Empty;
        }

        public void PrepareResponse(HttpContext context)
        {
            try
            {
                StringBuilder acknowledgement = new StringBuilder();

                acknowledgement.Append("<?xml version = \"1.0\" encoding = \"utf-8\"?>");
                acknowledgement.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
                acknowledgement.Append("<soapenv:Body>");
                acknowledgement.Append("<notifications xmlns=\"http://soap.sforce.com/2005/09/outbound\">");
                acknowledgement.Append("<Ack>true</Ack>");
                acknowledgement.Append("</notifications>");
                acknowledgement.Append("</soapenv:Body>");
                acknowledgement.Append("</soapenv:Envelope>");

                context.Response.Body = new MemoryStream(Encoding.UTF8.GetBytes(acknowledgement.ToString()));
                context.Response.ContentType = "text/xml";
                context.Response.StatusCode = 200;
            }
            catch (Exception)
            {
                //TODO: log exception
            }
        }

        public void ExtractData(INotifier notifier, String soapBody, String mode)
        {
            XmlTextReader xtr = null;
            XmlDocument doc = null;
            XmlNode node = null;

            if (soapBody != String.Empty)
            {
                if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Parsing notification SOAP: " + soapBody); }

                xtr = new XmlTextReader(new System.IO.StringReader(soapBody));
                doc = new XmlDocument();
                node = doc.ReadNode(xtr);

                while (xtr.Read())
                {
                    if (xtr.IsStartElement())
                    {
                        // Get element name
                        switch (xtr.Name)
                        {
                            //extract session id
                            case "SessionId":
                                if (xtr.Read())
                                {
                                    this.SessionID = xtr.Value.Trim();
                                    if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("SessionId: " + this.SessionID); }
                                }
                                break;
                            //extract session url
                            case "PartnerUrl":
                                if (xtr.Read())
                                {
                                    this.SessionURL = xtr.Value.Trim();
                                    if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("SessionURL: " + this.SessionURL); }
                                }
                                break;
                            //extract object's name
                            case "sObject":
                                if (xtr["xsi:type"] != null)
                                {
                                    string sObjectName = xtr["xsi:type"];
                                    if (sObjectName != null)
                                    {
                                        this.ObjectName = sObjectName.Split(new char[] { ':' })[1];
                                        if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("ObjectName: " + this.ObjectName); }
                                    }
                                }
                                break;
                            //extract notification id [note: salesforce can send a notification multiple times. it is, therefore, a good idea to keep track of this id.]
                            case "Id":
                                if (xtr.Read())
                                {
                                    this.NotificationIDs.Add(xtr.Value.Trim());
                                }
                                break;
                            //extract record id
                            case "sf:Id":
                                if (xtr.Read())
                                {
                                    this.ObjectIDs.Add(xtr.Value.Trim());
                                    if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("ObjectId: " + xtr.Value.Trim()); }
                                }
                                break;
                        }
                    }
                }
            }
            else
            {
                if (SettingUtils.IsDebugging(mode)) { notifier.AddLogEntry("Notification has no data to parse."); }
            }
        }
    }
}