using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class CanvasUser
    {
        [DataMember]
        public Boolean accessibilityModeEnabled
        {
            get;
            set;
        }

        [DataMember]
        public String currencyISOCode
        {
            get;
            set;
        }

        [DataMember]
        public String email
        {
            get;
            set;
        }

        [DataMember]
        public String firstName
        {
            get;
            set;
        }

        [DataMember]
        public String fullName
        {
            get;
            set;
        }

        [DataMember]
        public Boolean isDefaultNetwork
        {
            get;
            set;
        }

        [DataMember]
        public String language
        {
            get;
            set;
        }

        [DataMember]
        public String lastName
        {
            get;
            set;
        }

        [DataMember]
        public String locale
        {
            get;
            set;
        }

        [DataMember]
        public String networkId
        {
            get;
            set;
        }

        [DataMember]
        public String profileId
        {
            get;
            set;
        }

        [DataMember]
        public String profilePhotoUrl
        {
            get;
            set;
        }

        [DataMember]
        public String profileThumbnailUrl
        {
            get;
            set;
        }

        [DataMember]
        public String roleId
        {
            get;
            set;
        }

        [DataMember]
        public String siteUrl
        {
            get;
            set;
        }

        [DataMember]
        public String siteUrlPrefix
        {
            get;
            set;
        }

        [DataMember]
        public String timeZone
        {
            get;
            set;
        }

        [DataMember]
        public String userId
        {
            get;
            set;
        }

        [DataMember]
        public String userName
        {
            get;
            set;
        }

        [DataMember]
        public String userType
        {
            get;
            set;
        }
    }
}
