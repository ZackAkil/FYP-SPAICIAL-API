using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;

namespace Spaicial_API.Models
{
    public static class ApiKeyAuthentication
    {
        /// <summary>
        /// Checks that API key is exists within the database,
        ///  throws appropriate HTTP request erros depending on failure condition. 
        /// </summary>
        /// <param name="apiKey">api key value stringe</param>
        /// <param name="db">refference to database object</param>
        public static void CheckApiKey(string apiKey, ref spaicial_dbEntities db)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                var response = new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("No API key"),
                    ReasonPhrase = "The API key was not supplied."
                });

                var challenge = new AuthenticationHeaderValue("valid_ApiKey_required");
                response.Response.Headers.WwwAuthenticate.Add(challenge);

                throw response;

            }
            else if (!db.ApiKey.Any(a => a.keyValue == apiKey))
            {
                var response = new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Invalid API key"),
                    ReasonPhrase = "The API key used does not exist in the system."
                });

                var challenge = new AuthenticationHeaderValue("valid_ApiKey_required");
                response.Response.Headers.WwwAuthenticate.Add(challenge);

                throw response;
            }

        }

        /// <summary>
        /// Checks that API key is exists within the database and is used for the station zone,
        ///  throws appropriate HTTP request erros depending on failure condition. 
        /// </summary>
        /// <param name="apiKey">api key value stringe</param>
        /// <param name="zone">zone object</param>
        /// <param name="db">refference to database object</param>
        public static void CheckApiKey(string apiKey,Zone zone,  ref spaicial_dbEntities db)
        {
            //key string is supplied
            if (string.IsNullOrEmpty(apiKey))
            {
                var response = new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("No API key"),
                    ReasonPhrase = "The API key was not supplied."
                });

                var challenge = new AuthenticationHeaderValue("valid_ApiKey_required");
                response.Response.Headers.WwwAuthenticate.Add(challenge);

                throw response;

            }
            //key existed in database
            else if (!db.ApiKey.Any(a => a.keyValue == apiKey))
            {
                var response = new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Invalid API key"),
                    ReasonPhrase = "The API key used does not exist in the system."
                });

                var challenge = new AuthenticationHeaderValue("valid_ApiKey_required");
                response.Response.Headers.WwwAuthenticate.Add(challenge);

                throw response;
            }
            //key is authorised to the zone
            else if(zone.ApiKey.keyValue != apiKey)
            {
                var response = new HttpResponseException(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Unauthorised API key"),
                    ReasonPhrase = "The API key used is not authorised for accessing this zone."
                });

                var challenge = new AuthenticationHeaderValue("valid_ApiKey_required");
                response.Response.Headers.WwwAuthenticate.Add(challenge);

                throw response;
            }

        }



    }
}