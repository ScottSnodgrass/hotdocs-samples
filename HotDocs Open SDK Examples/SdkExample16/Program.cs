﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.IO;

namespace SdkExample16
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            var subscriberId = "example-subscriber-id";
            var signingKey = "example-signing-key";
            var timestamp =   DateTime.UtcNow;
            var packageId = "HelloWorld";

            // Generate HMAC using Cloud Services signing key
            var hmac = GetHMAC(signingKey, timestamp, subscriberId, packageId);

            // Create upload request            
            var request = CreateHttpRequestMessage(hmac, subscriberId, packageId, timestamp);

            //Send upload request to Cloud Service
            var client = new HttpClient();

            var response = client.SendAsync(request);

            Console.WriteLine("Get Component File:" + response.Result.StatusCode);
            Console.WriteLine("Get Component File:" + response.Result.ReasonPhrase);
            Console.WriteLine("Get Component File:" + response.Result.RequestMessage);
            Console.ReadKey();
        }

        private static string GetHMAC(string signingKey, DateTime timestamp, string subscriberId, string packageId)
        {
            // Calculate the HMAC
            var hmac = CalculateHMAC(signingKey, timestamp, subscriberId, packageId, "HelloWorld", false, "", true);

            try
            {
                ValidateHMAC(hmac, signingKey, timestamp, subscriberId, packageId, "HelloWorld", false, "", true);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return hmac;
        }

        private static HttpRequestMessage CreateHttpRequestMessage(string hmac, string subscriberId, string packageId, DateTime timestamp)
        {
            var billingRef = "";
            string includeDialogs = "False";
            string template = "HelloWorld";

            //https://localhost:444/RestfulSvc.svc/componentinfo/hdSolutions/HelloWorld/?includedialogs=False&billingref=

            var uploadUrl = string.Format("https://localhost:444/RestfulSvc.svc/componentinfo/{0}/{1}?includedialogs={3}&billingref={4}", subscriberId, packageId, template, includeDialogs.ToString(), billingRef, timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"), hmac);
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(uploadUrl),
                Method = HttpMethod.Get,                
            };

            // Add request headers            
            request.Headers.TryAddWithoutValidation("x-hd-date", timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"));            
            request.Headers.TryAddWithoutValidation("Authorization", hmac);

            return request;
        }

        public static string CalculateHMAC(string signingKey, params object[] paramList)
        {
            byte[] key = Encoding.UTF8.GetBytes(signingKey);
            string stringToSign = Canonicalize(paramList);
            byte[] bytesToSign = Encoding.UTF8.GetBytes(stringToSign);
            byte[] signature;

            using (var hmac = new System.Security.Cryptography.HMACSHA1(key))
            {
                signature = hmac.ComputeHash(bytesToSign);
            }

            return Convert.ToBase64String(signature);
        }

        public static string Canonicalize(params object[] paramList)
        {
            if (paramList == null)
            {
                throw new ArgumentNullException();
            }

            var strings = paramList.Select(param =>
            {
                if (param is string || param is int || param is Enum || param is bool)
                {
                    return param.ToString();
                }

                if (param is DateTime)
                {
                    DateTime utcTime = ((DateTime)param).ToUniversalTime();
                    return utcTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
                }

                if (param is Dictionary<string, string>)
                {
                    var sorted = ((Dictionary<string, string>)param).OrderBy(kv => kv.Key);
                    var stringified = sorted.Select(kv => kv.Key + "=" + kv.Value).ToArray();
                    return string.Join("\n", stringified);
                }
                return "";
            });

            return string.Join("\n", strings.ToArray());
        }

        public static void ValidateHMAC(string hmac, string signingKey, params object[] paramList)
        {
            string calculatedHMAC = CalculateHMAC(signingKey, paramList);

            if (hmac != calculatedHMAC)
            {
                throw new Exception("Invalid Request Parameters");
            }
        }
      
    }
}
