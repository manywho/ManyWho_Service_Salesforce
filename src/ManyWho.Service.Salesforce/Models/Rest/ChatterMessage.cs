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
    /// Chatter Message
    /// </summary>
    [DataContract]
    public class ChatterMessage
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
        /// Gets or sets the created date.
        /// </summary>
        /// <value>
        /// The created date.
        /// </value>
        [DataMember(Name = "createdDate")]
        public string CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the actor.
        /// </summary>
        /// <value>
        /// The actor.
        /// </value>
        [DataMember(Name = "actor")]
        public ChatterUserInfo Actor { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>
        /// The user.
        /// </value>
        [DataMember(Name = "user")]
        public ChatterUserInfo User { get; set; }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        [DataMember(Name = "body")]
        public ChatterMessageBody Body { get; set; }

        /// <summary>
        /// Gets or sets the likes.
        /// </summary>
        /// <value>
        /// The likes.
        /// </value>
        [DataMember(Name = "likes")]
        public ChatterLikesItems Likes { get; set; }

        /// <summary>
        /// Gets or sets the attachment.
        /// </summary>
        /// <value>
        /// The attachment.
        /// </value>
        [DataMember(Name = "attachment")]
        public ChatterAttachedFileInfo Attachment { get; set; }

        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        /// <value>
        /// The comments.
        /// </value>
        [DataMember(Name = "comments")]
        public ChatterComents Comments { get; set; }

        /// <summary>
        /// Gets or sets my like.
        /// </summary>
        /// <value>
        /// My like.
        /// </value>
        [DataMember(Name = "myLike")]
        public ChatterMyLike MyLike { get; set; }
    }
}
