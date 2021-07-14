using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace SimpleBlob.Cli.Services
{
    // https://www.c-sharpcorner.com/UploadFile/dacca2/http-request-methods-get-post-put-and-delete/

    static public class ClientHelper
    {
        /// <summary>
        /// Gets an HTTP client configured for the specified API, using the
        /// specified bearer token.
        /// </summary>
        /// <param name="apiRootUri">The API root URI.</param>
        /// <param name="token">The bearer token.</param>
        /// <returns>Client.</returns>
        public static HttpClient GetClient(string apiRootUri, string token)
        {
            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(apiRootUri)
            };
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }
}
