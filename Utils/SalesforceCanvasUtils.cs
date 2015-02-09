using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Runtime.Serialization.Json;
using ManyWho.Flow.SDK.Utils;
using ManyWho.Flow.SDK.Security;
using ManyWho.Service.Salesforce.Models.Canvas;

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

namespace ManyWho.Service.Salesforce.Utils
{
    public class SalesforceCanvasUtils
    {
        public static CanvasRequest VerifyAndDecode(IAuthenticatedWho authenticatedWho, String input, String secret)
        {
            CanvasRequest canvasRequest= null;
            DataContractJsonSerializer serializer = null;
            String[] split = null;
            String encodedSig = null;
            String encodedEnvelope = null;
            String decodedEnvelope = null;
            Type type = null;

            // Get the signature and envelope
            split = GetParts(authenticatedWho, input);
            encodedSig = split[0];
            encodedEnvelope = split[1];

            // Verify the encoding and envelope
            Verify(authenticatedWho, secret, encodedEnvelope, encodedSig);

            // Create a new type for our json serializer
            type = typeof(CanvasRequest);

            // Create a new serializer using the canvas request type
            serializer = new DataContractJsonSerializer(type);

            Byte[] b = Convert.FromBase64String(encodedEnvelope);
            decodedEnvelope = Encoding.Default.GetString(b);

            // Create the canvas request object from the encoded json body
            canvasRequest = serializer.ReadObject(new MemoryStream(Encoding.Unicode.GetBytes(decodedEnvelope))) as CanvasRequest;

            // Return the request as a Canvas Object
            return canvasRequest;
        }

        public static String VerifyAndDecodeAsJson(IAuthenticatedWho authenticatedWho, String input, String secret)
        {
            String[] split = GetParts(authenticatedWho, input);
            String encodedSig = split[0];
            String encodedEnvelope = split[1];

            // Get the signature and envelope from the input
            split = GetParts(authenticatedWho, input);
            encodedSig = split[0];
            encodedEnvelope = split[1];

            // Verify the signature and envelope
            Verify(authenticatedWho, secret, encodedEnvelope, encodedSig);

            // Return the encoded envelope containing the JSON
            return encodedEnvelope;
        }

        private static String[] GetParts(IAuthenticatedWho authenticatedWho, String input)
        {
            String[] split = null;

            if (input == null || 
                input.IndexOf(".") <= 0)
            {
                throw new ArgumentNullException("BadRequest", "Input [" + input + "] doesn't look like a signed request");
            }

            // Split the string on the period - the first part is the signature, the second part is the body
            split = input.Split('.');

            return split;
        }

        private static void Verify(IAuthenticatedWho authenticatedWho, String secret, String encodedEnvelope, String encodedSig)
        {
            //Boolean tampered = false;

            if (encodedEnvelope == null ||
                encodedEnvelope.Trim().Length == 0)
            {
                throw new ArgumentNullException("BadRequest", "Encoded envelope is null or blank.");
            }

            if (encodedSig == null ||
                encodedSig.Trim().Length == 0)
            {
                throw new ArgumentNullException("BadRequest", "Encoded signature is null or blank.");
            }

            if (secret == null || 
                secret.Trim().Length == 0)
            {
                throw new ArgumentNullException("BadRequest", "Secret is null, did you set your environment variable CANVAS_CONSUMER_SECRET?");
            }

            //using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            //{
            //    byte[] digest = hmac.ComputeHash(Encoding.UTF8.GetBytes(encodedEnvelope));
            //    byte[] decode_sig = Encoding.UTF8.GetBytes(encodedSig);

            //    // Now compare the digest with the signature
            //    for (int i = 0; i < decode_sig.Length; i++)
            //    {
            //        if (digest[i] != decode_sig[i])
            //        {
            //            tampered = true;
            //        }
            //    }
            //}

            //if (tampered)
            //{
            //    ExceptionFactory.GetWebException(authenticatedUser, HttpStatusCode.Forbidden, "Hash values differ! The message may have been tampered with!");
            //}
        }
    }
}
