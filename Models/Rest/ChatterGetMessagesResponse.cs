using System.Collections.Generic;
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

namespace ManyWho.Service.Salesforce.Models.Rest
{
    /// <summary>
    /// Chatter Get Messages Response
    /// </summary>
    [DataContract]
    public class ChatterGetMessagesResponse
    {
        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        [DataMember(Name = "items")]
        public IList<ChatterMessage> Items { get; set; }

        /// <summary>
        /// Gets or sets the next page URL.
        /// </summary>
        /// <value>
        /// The next page URL.
        /// </value>
        [DataMember(Name = "nextPageUrl")]
        public string NextPageUrl { get; set; }
    }
}
