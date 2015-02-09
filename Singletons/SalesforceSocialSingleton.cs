using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using System.Web.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ManyWho.Flow.SDK;
using ManyWho.Flow.SDK.Utils;
using ManyWho.Flow.SDK.Social;
using ManyWho.Flow.SDK.Security;
using ManyWho.Flow.SDK.Describe;
using ManyWho.Flow.SDK.Draw.Elements.UI;
using ManyWho.Flow.SDK.Draw.Elements.Type;
using ManyWho.Flow.SDK.Run.Elements.UI;
using ManyWho.Flow.SDK.Run.Elements.Config;
using ManyWho.Flow.SDK.Run.Elements.Type;
using ManyWho.Service.Salesforce.Utils;
using ManyWho.Service.Salesforce.Models.Rest;
using ManyWho.Service.Salesforce.Models.Rest.Enums;

namespace ManyWho.Service.Salesforce.Singletons
{
    public class SalesforceSocialSingleton
    {
        private static SalesforceSocialSingleton salesforceSocialSingleton;

        private SalesforceSocialSingleton()
        {

        }

        public static SalesforceSocialSingleton GetInstance()
        {
            if (salesforceSocialSingleton == null)
            {
                salesforceSocialSingleton = new SalesforceSocialSingleton();
            }

            return salesforceSocialSingleton;
        }

        public MessageAPI PostNotification(INotifier notifier, IAuthenticatedWho authenticatedWho, ServiceRequestAPI serviceRequest, String endpointUrl, String flowLink, ChatterPostedMessage chatterPostedMessage)
        {
            return PostNotification(notifier, authenticatedWho, SalesforceHttpUtils.GetAuthenticationDetails(authenticatedWho.Token).Token, serviceRequest, endpointUrl, flowLink, chatterPostedMessage);
        }

        /// <summary>
        /// This method allows the user to share the flow app in salesforce with their friends.
        /// </summary>
        public MessageAPI PostNotification(INotifier notifier, IAuthenticatedWho authenticatedWho, String oauthToken, ServiceRequestAPI serviceRequest, String endpointUrl, String flowLink, ChatterPostedMessage chatterPostedMessage)
        {
            HttpResponseMessage httpResponseMessage = null;
            HttpClient httpClient = null;
            MediaTypeFormatter jsonFormatter = null;
            MultipartFormDataContent multipartFormDataContent = null;
            ChatterMessage chatterMessage = null;
            ChatterAttachmentLink chatterAttachmentLink = null;
            MessageAPI message = null;
            String chatterBaseUrl = null;
            String adminEmail = null;

            if (oauthToken == null ||
                oauthToken.Trim().Length == 0)
            {
                throw new ArgumentNullException("BadRequest", "OAuthToken cannot be null or blank.");
            }

            if (endpointUrl == null ||
                endpointUrl.Trim().Length == 0)
            {
                throw new ArgumentNullException("BadRequest", "EndpointUrl cannot be null or blank.");
            }

            // Grab the values necessary to post the message over to chatter
            chatterBaseUrl = ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_CHATTER_BASE_URL, serviceRequest.configurationValues, true);
            adminEmail =  ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_ADMIN_EMAIL, serviceRequest.configurationValues, true);

            // Now we can create the multipart form we're going to post over to salesforce
            multipartFormDataContent = new MultipartFormDataContent();

            if (flowLink != null &&
                flowLink.Trim().Length > 0)
            {
                // We also add the link to the app so the user has it
                chatterAttachmentLink = new ChatterAttachmentLink();
                chatterAttachmentLink.AttachmentType = "Link";
                chatterAttachmentLink.Url = flowLink;
                chatterAttachmentLink.UrlName = "Link to ManyWho Flow";

                chatterPostedMessage.Attachment = chatterAttachmentLink;
            }

            // We enclose the request in a for loop to handle http errors
            for (int i = 0; i < SalesforceHttpUtils.MAXIMUM_RETRIES; i++)
            {
                try
                {
                    // Create a new client object
                    httpClient = SalesforceHttpUtils.CreateHttpClient(oauthToken);

                    // Create a new json formatter so the request will be in the right format
                    jsonFormatter = new JsonMediaTypeFormatter();

                    // Use the JSON formatter to create the content of the chatter post
                    multipartFormDataContent.Add(new ObjectContent<ChatterPostedMessage>(chatterPostedMessage, jsonFormatter), "json");

                    // Post the message over to chatter
                    httpResponseMessage = httpClient.PostAsync(endpointUrl, multipartFormDataContent).Result;

                    // Check the status of the response and respond appropriately
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        // Grab the chatter message from the post
                        chatterMessage = httpResponseMessage.Content.ReadAsAsync<ChatterMessage>().Result;

                        // Convert it over to a manywho message
                        message = SalesforceSocialSingleton.GetInstance().ChatterMessageToMessageAPI(chatterBaseUrl, null, chatterMessage);

                        // We successfully executed the request, we can break out of the retry loop
                        break;
                    }
                    else
                    {
                        // Make sure we handle the lack of success properly
                        BaseHttpUtils.HandleUnsuccessfulHttpResponseMessage(notifier, authenticatedWho, i, httpResponseMessage, endpointUrl);
                    }
                }
                catch (Exception exception)
                {
                    // Make sure we handle the exception properly
                    BaseHttpUtils.HandleHttpException(notifier, authenticatedWho, i, exception, endpointUrl);
                }
                finally
                {
                    // Clean up the objects from the request
                    HttpUtils.CleanUpHttp(httpClient, null, httpResponseMessage);
                }
            }

            return message;
        }

        /// <summary>
        /// This is a general purpose method for getting user information from chatter.
        /// </summary>
        public WhoAPI GetUserInfoById(INotifier notifier, IAuthenticatedWho authenticatedWho, String streamId, String id, SocialServiceRequestAPI socialServiceRequestAPI)
        {
            List<WhoAPI> stuffAuthenticatedUserIsFollowing = null;
            ChatterUserInfo chatterUserInfo = null;
            HttpResponseMessage httpResponseMessage = null;
            HttpClient httpClient = null;
            WhoAPI who = null;
            String chatterBaseUrl = null;
            String endpointUrl = null;
            String adminEmail = null;

            if (authenticatedWho == null)
            {
                throw new ArgumentNullException("BadRequest", "AuthenticatedWho is null.");
            }

            if (id == null ||
                id.Trim().Length == 0)
            {
                throw new ArgumentNullException("BadRequest", "Id for user is null or blank.");
            }

            if (streamId == null ||
                streamId.Trim().Length == 0)
            {
                throw new ArgumentNullException("BadRequest", "Stream identifier is null or blank.");
            }

            if (socialServiceRequestAPI == null)
            {
                throw new ArgumentNullException("BadRequest", "SocialServiceRequest is null.");
            }

            // We only need the chatter base url for this call
            chatterBaseUrl =  ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_CHATTER_BASE_URL, socialServiceRequestAPI.configurationValues, true);
            adminEmail =  ValueUtils.GetContentValue(SalesforceServiceSingleton.SERVICE_VALUE_ADMIN_EMAIL, socialServiceRequestAPI.configurationValues, true);

            // We enclose the request in a for loop to handle http errors
            for (int i = 0; i < SalesforceHttpUtils.MAXIMUM_RETRIES; i++)
            {
                try
                {
                    // Create a new client object
                    httpClient = SalesforceHttpUtils.CreateHttpClient(SalesforceHttpUtils.GetAuthenticationDetails(authenticatedWho.Token).Token);

                    // Create the endpoint url
                    endpointUrl = chatterBaseUrl + SalesforceServiceSingleton.CHATTER_URI_PART_API_VERSION + SalesforceServiceSingleton.CHATTER_URI_PART_USERS + "/" + id;

                    // Call the get method on the chatter API to grab the user information
                    httpResponseMessage = httpClient.GetAsync(endpointUrl).Result;

                    // Check the status of the response and respond appropriately
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        // Grab the chatter user info from the result
                        chatterUserInfo = httpResponseMessage.Content.ReadAsAsync<ChatterUserInfo>().Result;

                        // Convert the chatter user info over to a who
                        who = this.ChatterUserInfoToWhoAPI(chatterUserInfo);

                        // Get the stuff this user is following
                        stuffAuthenticatedUserIsFollowing = this.GetStuffAuthenticatedUserIsFollowing(httpClient, chatterBaseUrl);

                        // Check to see if the authenticated user is also the id
                        if (id.Equals(SalesforceServiceSingleton.CHATTER_ME, StringComparison.InvariantCultureIgnoreCase) == false)
                        {
                            // Check to see if the currently authenticated user is following the provided user id
                            if (stuffAuthenticatedUserIsFollowing != null &&
                                stuffAuthenticatedUserIsFollowing.Any(x => x.id == id) == true)
                            {
                                // The authenticated user is following the provided user id
                                who.isFollower = true;
                            }
                        }
                        else
                        {
                            // If the authenticated user is the same as the provided id, the "is following" refers to the stream
                            if (stuffAuthenticatedUserIsFollowing != null &&
                                stuffAuthenticatedUserIsFollowing.Any(x => x.id == streamId) == true)
                            {
                                // We are following this stream
                                who.isFollower = true;
                            }
                        }

                        // We successfully executed the request, we can break out of the retry loop
                        break;
                    }
                    else
                    {
                        // Make sure we handle the lack of success properly
                        BaseHttpUtils.HandleUnsuccessfulHttpResponseMessage(notifier, authenticatedWho, i, httpResponseMessage, endpointUrl);
                    }
                }
                catch (Exception exception)
                {
                    // Make sure we handle the exception properly
                    BaseHttpUtils.HandleHttpException(notifier, authenticatedWho, i, exception, endpointUrl);
                }
                finally
                {
                    // Clean up the objects from the request
                    HttpUtils.CleanUpHttp(httpClient, null, httpResponseMessage);
                }
            }

            return who;
        }

        /// <summary>
        /// Utility method for getting the records and users the user is following.
        /// </summary>
        public List<WhoAPI> GetStuffAuthenticatedUserIsFollowing(HttpClient httpClient, String chatterBaseUrl)
        {
            WhoAPI who = null;
            List<WhoAPI> whos = null;
            HttpResponseMessage httpResponseMessage = null;
            ChatterFollowingResponse followingUsersresponse = null;

            // TODO: Need to add paging support to this as it currently only sends back the first page of results
            httpResponseMessage = httpClient.GetAsync(chatterBaseUrl + SalesforceServiceSingleton.CHATTER_URI_PART_API_VERSION + SalesforceServiceSingleton.CHATTER_URI_PART_FOLLOWING_ME).Result;

            if (httpResponseMessage.IsSuccessStatusCode == true)
            {
                // Get the followers from the response as the request was successful
                followingUsersresponse = httpResponseMessage.Content.ReadAsAsync<ChatterFollowingResponse>().Result;

                // Check to see if the user is in fact following anything
                if (followingUsersresponse.Following != null &&
                    followingUsersresponse.Following.Count > 0)
                {
                    whos = new List<WhoAPI>();

                    // Go through the list of following things and convert them to "whos"
                    foreach (ChatterFollowing chatterFollowing in followingUsersresponse.Following)
                    {
                        // The followed thing is in the subject
                        if (chatterFollowing.Subject != null)
                        {
                            who = this.ChatterUserInfoToWhoAPI(chatterFollowing.Subject);

                            whos.Add(who);
                        }
                    }
                }
            }
            else
            {
                // Throw an exception which will cause the calling method to retry - we don't have that here
                throw new ArgumentNullException("BadRequest", httpResponseMessage.ReasonPhrase);
            }

            return whos;
        }

        /// <summary>
        /// Utility method for converting ManyWho messages into chatter messages.
        /// </summary>
        public ChatterNewMessageBody ConvertNewMessageAPIToChatterNewMessageBody(NewMessageAPI newMessage)
        {
            Dictionary<Int32, String> mentionsAndHashTags = null;
            List<MentionedWhoAPI> mentionedUsers = null;
            List<ChatterSegment> chatterSegments = null;
            List<Int32> mentionsAndHashTagKeys = null;
            List<String> messageFragments = null;
            String messageText = null;

            if (newMessage == null)
            {
                throw new ArgumentNullException("BadRequest", "The new message is null in the service request");
            }

            // Create a new list to hold our message segments
            chatterSegments = new List<ChatterSegment>();

            // Create a dictionary to hold all of the at mentions and hash tags
            mentionsAndHashTags = new Dictionary<Int32, String>();

            // Grab the list of at mentioned users and the text of the message
            mentionedUsers = newMessage.mentionedWhos;
            messageText = newMessage.messageText;

            if (messageText != null &&
                messageText.Trim().Length > 0)
            {
                // Break the message up into individual words so we can query the list for hash tags
                messageFragments = messageText.Split(' ').ToList();

                // Now query the message fragments and filter out anything that is not a hash tag
                messageFragments = messageFragments.Where(x => (x.Contains('#') && x.Length > 1)).ToList();

                // If we have some hash tags, we need to add those to our list - they're posted to chatter in separate segments
                if (messageFragments != null &&
                    messageFragments.Count > 0)
                {
                    // Go through each of the tags and grab out the information cleanly so we can post it over to chatter correctly
                    foreach (String tag in messageFragments)
                    {
                        Int32 index = Regex.Match(messageText, SalesforceServiceSingleton.CHATTER_HASH_TAG_OR_MENTION_START + tag + SalesforceServiceSingleton.CHATTER_HASH_TAG_OR_MENTION_FINISH).Index;

                        while (mentionsAndHashTags.Select(x => x.Key).Contains(index))
                        {
                            String tagEntry = messageText.Substring(index + mentionsAndHashTags[index].Length);
                            index = Regex.Match(tagEntry, SalesforceServiceSingleton.CHATTER_HASH_TAG_OR_MENTION_START + tag + SalesforceServiceSingleton.CHATTER_HASH_TAG_OR_MENTION_FINISH).Index + index + mentionsAndHashTags[index].Length;
                        }

                        mentionsAndHashTags.Add(index, tag);
                    }
                }
            }

            // Go through the mentioned users and get the data out for chatter
            if (mentionedUsers != null &&
                mentionedUsers.Count > 0)
            {
                foreach (MentionedWhoAPI mentionedUser in mentionedUsers)
                {
                    // Grab the user information out nice and cleanly so we have it for chatter
                    Int32 index = messageText.IndexOf(mentionedUser.fullName);

                    while (mentionsAndHashTags.Select(x => x.Key).Contains(index))
                    {
                        var mentionedUserEntry = messageText.Substring(index + mentionsAndHashTags[index].Length);
                        index = Regex.Match(mentionedUserEntry, SalesforceServiceSingleton.CHATTER_HASH_TAG_OR_MENTION_START + mentionedUser.fullName + SalesforceServiceSingleton.CHATTER_HASH_TAG_OR_MENTION_FINISH).Index + index + mentionsAndHashTags[index].Length;
                    }

                    mentionsAndHashTags.Add(index, mentionedUser.fullName);
                }
            }

            // Get the keys out of our complete list and sort them numerically
            mentionsAndHashTagKeys = mentionsAndHashTags.Keys.ToList();
            mentionsAndHashTagKeys.Sort();

            if (mentionsAndHashTagKeys.Count > 0)
            {
                // Loop through keys and create the chatter segments
                for (var i = 0; i < mentionsAndHashTagKeys.Count; i++)
                {
                    // If the item is the first in the text string then only add the item, otherwise add the text and the item
                    if (mentionsAndHashTagKeys[i] != 0)
                    {
                        Int32 startIndex = i != 0 ? (mentionsAndHashTagKeys[i - 1] + mentionsAndHashTags[mentionsAndHashTagKeys[i - 1]].Length) : 0;
                        Int32 endIndex = mentionsAndHashTagKeys[i] - startIndex;

                        chatterSegments.Add(new ChatterNewMessageSegment { Type = ChatterMessageSegmentType.Text.ToString(), Text = messageText.Substring(startIndex, endIndex) });
                    }

                    // Check to see if this entry is a hash tag
                    if (mentionsAndHashTags[mentionsAndHashTagKeys[i]].Contains('#'))
                    {
                        chatterSegments.Add(new ChatterHashTagSegment { Type = ChatterMessageSegmentType.Hashtag.ToString(), Tag = mentionsAndHashTags[mentionsAndHashTagKeys[i]].Remove(0, 1) });
                    }
                    else
                    {
                        // We have an at mentioned user, add that segment
                        MentionedWhoAPI clientMentionedUser = mentionedUsers.FirstOrDefault(x => x.fullName == mentionsAndHashTags[mentionsAndHashTagKeys[i]]);

                        if (clientMentionedUser != null)
                        {
                            chatterSegments.Add(new ChatterMentionsSegment { Type = ChatterMessageSegmentType.Mention.ToString(), Id = clientMentionedUser.id });
                        }
                    }

                    // Add the last bit of text as a final segment to the post
                    if (i == mentionsAndHashTagKeys.Count - 1)
                    {
                        Int32 startIndex = mentionsAndHashTagKeys[i] + mentionsAndHashTags[mentionsAndHashTagKeys[i]].Length;
                        String text = messageText.Substring(startIndex);

                        if (String.IsNullOrWhiteSpace(text) == false)
                        {
                            chatterSegments.Add(new ChatterNewMessageSegment { Type = ChatterMessageSegmentType.Text.ToString(), Text = text });
                        }
                    }
                }
            }
            else
            {
                // We don't have any segments, so simply create one segment for the entire message
                chatterSegments.Add(new ChatterNewMessageSegment { Type = ChatterMessageSegmentType.Text.ToString(), Text = messageText });
            }

            return new ChatterNewMessageBody { MessageSegments = chatterSegments };
        }

        /// <summary>
        /// This is a utility method for translating chatter messages into a compatible format for ManyWho.
        /// </summary>
        public List<MessageAPI> ChatterMessageToMessageAPI(String chatterBaseUrl, String parentId, IList<ChatterMessage> chatterMessages)
        {
            List<MessageAPI> messages = null;

            // Convert the chatter messages to manywho messages
            if (chatterMessages != null &&
                chatterMessages.Count > 0)
            {
                messages = new List<MessageAPI>();

                foreach (ChatterMessage chatterMessage in chatterMessages)
                {
                    // Add this message to the list
                    messages.Add(ChatterMessageToMessageAPI(chatterBaseUrl, parentId, chatterMessage));
                }
            }

            return messages;
        }

        /// <summary>
        /// This is a utility method for translating chatter messages into a compatible format for ManyWho.
        /// </summary>
        public MessageAPI ChatterMessageToMessageAPI(String chatterBaseUrl, String parentId, ChatterMessage chatterMessage)
        {
            MessageAPI message = null;
            AttachmentAPI attachment = null;

            message = new MessageAPI();

            if (chatterMessage.Attachment != null)
            {
                attachment = new AttachmentAPI();
                attachment.name = chatterMessage.Attachment.Title;
                attachment.iconUrl = string.Format("{0}{1}", 
                                                    SettingUtils.GetStringSetting("ManyWho.CDNBasePath"),
                                                    SalesforceServiceSingleton.CHATTER_DEFAULT_FILE_IMAGE_URL);
                attachment.downloadUrl = chatterMessage.Attachment.DownloadUrl;
                attachment.description = chatterMessage.Attachment.Description;
                attachment.type = chatterMessage.Attachment.FileType;
                attachment.size = chatterMessage.Attachment.FileSize;

                message.attachments = new List<AttachmentAPI>();
                message.attachments.Add(attachment);
            }

            message.id = chatterMessage.Id;
            message.repliedToId = null;

            if (chatterMessage.Comments != null)
            {
                message.commentsCount = chatterMessage.Comments.Total;

                if (chatterMessage.Comments.Comments != null &&
                    chatterMessage.Comments.Comments.Count > 0)
                {
                    // Convert the child messages over for this message
                    message.comments = this.ChatterMessageToMessageAPI(chatterBaseUrl, message.id, chatterMessage.Comments.Comments);
                }
            }

            if (chatterMessage.MyLike != null)
            {
                message.myLikeId = chatterMessage.MyLike.Id;
            }

            message.createdDate = DateTime.Parse(chatterMessage.CreatedDate);
            message.comments = null;

            if (chatterMessage.Likes != null &&
                chatterMessage.Likes.Likes != null &&
                chatterMessage.Likes.Likes.Count > 0)
            {
                message.likerIds = new List<String>();

                foreach (ChatterLikeItem chatterLikeItem in chatterMessage.Likes.Likes)
                {
                    if (chatterLikeItem.User != null)
                    {
                        message.likerIds.Add(chatterLikeItem.User.Id);
                    }
                }
            }

            // The actor will be non-null for root posts, but it's the user for comments
            if (chatterMessage.Actor != null)
            {
                message.sender = this.ChatterUserInfoToWhoAPI(chatterMessage.Actor);
            }
            else
            {
                message.sender = this.ChatterUserInfoToWhoAPI(chatterMessage.User);
            }

            message.text = this.GetMessageText(chatterMessage);

            return message;
        }

        /// <summary>
        /// This is a utility method to change the chatter segments into a ManyWho message format.
        /// </summary>
        public String GetMessageText(ChatterMessage chatterMessage)
        {
            String text = chatterMessage.Body.Text;
            String userAtMention = null;
            IList<ChatterMessageSegment> chatterMessageSegments = chatterMessage.Body.MessageSegments.Where(x => x.Type == ChatterMessageSegmentType.Mention.ToString()).ToList();

            foreach (ChatterMessageSegment chatterMessageSegment in chatterMessageSegments)
            {
                // This is the user at mention piece
                userAtMention = String.Format(SalesforceServiceSingleton.CHATTER_MENTIONED_USER_NAME_SPAN, Guid.NewGuid().ToString(), chatterMessageSegment.User.Id, chatterMessageSegment.User.Name);

                // Parse the at mention into the message text so we have it as one block as opposed to a complex set of objects
                text = text.Replace(chatterMessageSegment.Text, userAtMention);
            }

            return text;
        }

        /// <summary>
        /// This is a utility method for converting chatter user info into a Who.
        /// </summary>
        public WhoAPI ChatterUserInfoToWhoAPI(ChatterUserInfo chatterUserInfo)
        {
            WhoAPI who = null;

            // Convert the chatter user info over to a who
            who = new WhoAPI();

            // The chatter user info is not always a user, it can be an object (i.e. the user is following an object - that comes through as a follower)
            if (chatterUserInfo.Photo != null)
            {
                who.avatarUrl = chatterUserInfo.Photo.SmallPhotoUrl;
            }

            who.fullName = chatterUserInfo.Name;
            who.id = chatterUserInfo.Id;

            return who;
        }

        /// <summary>
        /// This is a utility method for translating chatter mentioned users into a compatible format for ManyWho.
        /// </summary>
        public List<MentionedWhoAPI> ChatterUserInfoToMentionedUserAPI(List<ChatterUserInfo> chatterUserInfos)
        {
            List<MentionedWhoAPI> mentionedUsers = null;
            MentionedWhoAPI mentionedUser = null;

            if (chatterUserInfos != null &&
                chatterUserInfos.Count > 0)
            {
                mentionedUsers = new List<MentionedWhoAPI>();

                foreach (ChatterUserInfo chatterUserInfo in chatterUserInfos)
                {
                    // Convert the chatter user info over to a mentioned user
                    mentionedUser = new MentionedWhoAPI();

                    // The chatter user info is not always a user, it can be an object (i.e. the user is following an object - that comes through as a follower)
                    if (chatterUserInfo.Photo != null)
                    {
                        mentionedUser.avatarUrl = chatterUserInfo.Photo.SmallPhotoUrl;
                    }

                    mentionedUser.fullName = chatterUserInfo.Name;
                    mentionedUser.id = chatterUserInfo.Id;
                    mentionedUser.jobTitle = chatterUserInfo.Title;
                    mentionedUser.name = chatterUserInfo.Name;

                    mentionedUsers.Add(mentionedUser);
                }
            }

            return mentionedUsers;
        }
    }
}
