using System;
using System.Runtime.Serialization;

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

namespace ManyWho.Service.Salesforce.Models.Canvas
{
    [DataContract]
    public class CanvasLinks
    {
        [DataMember]
        public String chatterFeedItemsUrl
        {
            get;
            set;
        }

        [DataMember]
        public String chatterFeedsUrl
        {
            get;
            set;
        }

        [DataMember]
        public String chatterGroupsUrl
        {
            get;
            set;
        }

        [DataMember]
        public String chatterUsersUrl
        {
            get;
            set;
        }

        [DataMember]
        public String enterpriseUrl
        {
            get;
            set;
        }

        [DataMember]
        public String loginUrl
        {
            get;
            set;
        }

        [DataMember]
        public String metadataUrl
        {
            get;
            set;
        }

        [DataMember]
        public String partnerUrl
        {
            get;
            set;
        }

        [DataMember]
        public String queryUrl
        {
            get;
            set;
        }

        [DataMember]
        public String recentItemsUrl
        {
            get;
            set;
        }

        [DataMember]
        public String restUrl
        {
            get;
            set;
        }

        [DataMember]
        public String searchUrl
        {
            get;
            set;
        }

        [DataMember]
        public String sobjectUrl
        {
            get;
            set;
        }

        [DataMember]
        public String userUrl
        {
            get;
            set;
        }
    }
}
