﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using ManyWho.Flow.SDK.Security;
using ManyWho.Flow.SDK.Run.Elements.Config;
using ManyWho.Service.Salesforce.Utils;

namespace ManyWho.Service.Salesforce.Singletons
{
    public class SalesforceListenerSingleton
    {
        private static SalesforceListenerSingleton salesforceListenerSingleton;

        private SalesforceListenerSingleton()
        {

        }

        public static SalesforceListenerSingleton GetInstance()
        {
            if (salesforceListenerSingleton == null)
            {
                salesforceListenerSingleton = new SalesforceListenerSingleton();
            }

            return salesforceListenerSingleton;
        }

        public Dictionary<String, ListenerServiceRequestAPI> GetListenerRequests(String tenantId, String objectId)
        {
            Dictionary<String, ListenerServiceRequestAPI> salesforceListenerEntries = null;
            String json = null;

            if (String.IsNullOrWhiteSpace(tenantId) == true)
            {
                throw new ArgumentNullException("TenantId", "The TenantId cannot be null or blank.");
            }

            if (String.IsNullOrWhiteSpace(objectId) == true)
            {
                throw new ArgumentNullException("ObjectId", "The ObjectId cannot be null or blank.");
            }

            // Get the stored JSON for the listener requests
            json = StorageUtils.GetStoredJson((tenantId + objectId).ToLower());

            if (string.IsNullOrWhiteSpace(json) == false)
            {
                // Convert the json back to actual objects
                salesforceListenerEntries = JsonConvert.DeserializeObject<Dictionary<String, ListenerServiceRequestAPI>>(json);
            }

            return salesforceListenerEntries;
        }

        public void UnregisterListener(String tenantId, String objectId, ListenerServiceRequestAPI listenerServiceRequest)
        {
            Dictionary<String, ListenerServiceRequestAPI> salesforceListenerEntries = null;

            if (String.IsNullOrWhiteSpace(tenantId) == true)
            {
                throw new ArgumentNullException("TenantId", "The TenantId cannot be null or blank.");
            }

            if (String.IsNullOrWhiteSpace(objectId) == true)
            {
                throw new ArgumentNullException("ObjectId", "The ObjectId cannot be null or blank.");
            }

            if (listenerServiceRequest == null)
            {
                throw new ArgumentNullException("ListenerServiceRequest", "The ListenerServiceRequest object cannot be null.");
            }

            if (String.IsNullOrWhiteSpace(listenerServiceRequest.token) == true)
            {
                throw new ArgumentNullException("ListenerServiceRequest.Token", "The ListenerServiceRequest.Token property cannot be null or blank.");
            }

            // Try to get the listener requests out for this object
            salesforceListenerEntries = this.GetListenerRequests(tenantId, objectId);

            // Check to make sure we found some listener service requests for this record
            if (salesforceListenerEntries != null)
            {
                // Check to see if the listener service request contains an entry for this token
                if (salesforceListenerEntries.ContainsKey(listenerServiceRequest.token) == true)
                {
                    // Remove this listener service request from the map
                    salesforceListenerEntries.Remove(listenerServiceRequest.token);
                }

                // If the listener service requests are now emptry, we remove the parent as well
                if (salesforceListenerEntries.Count == 0)
                {
                    StorageUtils.RemoveStoredJson((tenantId + objectId).ToLower());
                }
            }
        }

        public void RegisterListener(IAuthenticatedWho authenticatedWho, ListenerServiceRequestAPI listenerServiceRequest)
        {
            Dictionary<String, ListenerServiceRequestAPI> salesforceListenerEntries = null;
            String objectId = null;
            Guid externalGuid = Guid.Empty;

            if (authenticatedWho == null)
            {
                throw new ArgumentNullException("AuthenticatedWho", "The AuthenticatedWho object cannot be null.");
            }

            if (listenerServiceRequest == null)
            {
                throw new ArgumentNullException("ListenerServiceRequest", "The ListenerServiceRequest object cannot be null.");
            }

            if (String.IsNullOrWhiteSpace(listenerServiceRequest.listenType) == true)
            {
                throw new ArgumentNullException("ListenerServiceRequest.ListenType", "The ListenerServiceRequest.ListenType property cannot be null or blank.");
            }

            if (String.IsNullOrWhiteSpace(listenerServiceRequest.token) == true)
            {
                throw new ArgumentNullException("ListenerServiceRequest.Token", "The ListenerServiceRequest.Token property cannot be null or blank.");
            }

            if (listenerServiceRequest.valueForListening == null)
            {
                throw new ArgumentNullException("ListenerServiceRequest.ValueForListening", "The ListenerServiceRequest.ValueForListening property cannot be null.");
            }

            if (listenerServiceRequest.valueForListening.objectData == null ||
                listenerServiceRequest.valueForListening.objectData.Count == 0)
            {
                throw new ArgumentNullException("ListenerServiceRequest.ValueForListening.ObjectData", "The Salesforce Service cannot listen without any object records to listen to.");
            }

            if (listenerServiceRequest.valueForListening.objectData.Count > 1)
            {
                throw new ArgumentNullException("ListenerServiceRequest.ValueForListening.ObjectData", "The Salesforce Service cannot listen to more than one object record in a single request.");
            }

            if (String.IsNullOrWhiteSpace(listenerServiceRequest.valueForListening.objectData[0].externalId) == true ||
                Guid.TryParse(listenerServiceRequest.valueForListening.objectData[0].externalId, out externalGuid) == true)
            {
                throw new ArgumentNullException("ListenerServiceRequest.ValueForListening.ObjectData", "The Salesforce Service cannot listen to an object that has not been first saved or loaded from Salesforce.");
            }

            // Assign the object id from the external id
            objectId = listenerServiceRequest.valueForListening.objectData[0].externalId;

            // Try to get the listener requests out for this object
            salesforceListenerEntries = this.GetListenerRequests(listenerServiceRequest.tenantId, objectId);

            // Check to make sure we have something, or create a new list
            if (salesforceListenerEntries == null)
            {
                // This is the first time we're registering a listener for this object, so we create a new map
                salesforceListenerEntries = new Dictionary<String, ListenerServiceRequestAPI>();
            }

            // Now we have our list of listeners, we add this one based on the token
            salesforceListenerEntries[listenerServiceRequest.token.ToLower()] = listenerServiceRequest;

            // And we put the updated map, back into the data store
            StorageUtils.SetStoredJson((listenerServiceRequest.tenantId + objectId).ToLower(), JsonConvert.SerializeObject(salesforceListenerEntries));
        }
    }
}