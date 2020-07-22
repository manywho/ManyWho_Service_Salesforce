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
    /// Chatter Attached File Info
    /// </summary>
    [DataContract]
    public class ChatterAttachedFileInfo
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>
        /// The id.
        /// </value>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the size of the file.
        /// </summary>
        /// <value>
        /// The size of the file.
        /// </value>
        [DataMember(Name = "fileSize")]
        public string FileSize { get; set; }

        /// <summary>
        /// Gets or sets the download URL.
        /// </summary>
        /// <value>
        /// The download URL.
        /// </value>
        [DataMember(Name = "downloadUrl")]
        public string DownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        [DataMember(Name = "title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the file extension.
        /// </summary>
        /// <value>
        /// The file extension.
        /// </value>
        [DataMember(Name = "fileExtension")]
        public string FileExtension { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        [DataMember(Name = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the type of the file.
        /// </summary>
        /// <value>
        /// The type of the file.
        /// </value>
        [DataMember(Name = "fileType")]
        public string FileType { get; set; }
        
        /// <summary>
        /// Gets or sets the attachment type.
        /// </summary>
        /// <value>
        /// The type of the attachment.
        /// </value>
        [DataMember(Name = "type")]
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets the url of the link attachment.
        /// </summary>
        /// <value>
        /// The url of the link attachment.
        /// </value>
        [DataMember(Name = "url")]
        public string Url { get; set; }
    }
}
