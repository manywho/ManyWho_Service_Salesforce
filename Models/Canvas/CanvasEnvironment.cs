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
    public class CanvasEnvironment
    {
        [DataMember]
        public CanvasEnvironmentDimensions dimensions
        {
            get;
            set;
        }

        [DataMember]
        public String displayLocation
        {
            get;
            set;
        }

        [DataMember]
        public String locationUrl
        {
            get;
            set;
        }

        [DataMember]
        public String uiTheme
        {
            get;
            set;
        }

        [DataMember]
        public CanvasEnvironmentVersion version
        {
            get;
            set;
        }
    }
}
