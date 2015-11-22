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
    /// Chatter Authorization Response
    /// </summary>
    [DataContract]
    public class ChatterAuthorizationResponse
    {
        /// <summary>
        /// Gets or sets the instance URL.
        /// </summary>
        /// <value>
        /// The instance URL.
        /// </value>
        [DataMember(Name = "instance_url")]
        public string InstanceUrl { get; set; }

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>
        /// The access token.
        /// </value>
        [DataMember(Name = "access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the refresh token.
        /// </summary>
        /// <value>
        /// The refresh token.
        /// </value>
        [DataMember(Name = "refresh_token")]
        public string RefreshToken { get; set; }
    }
}
