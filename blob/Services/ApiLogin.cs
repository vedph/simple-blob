using System;
using System.IO;
using System.Net;
using System.Text.Json;

namespace SimpleBlob.Cli.Services
{
    /// <summary>
    /// API login component.
    /// </summary>
    public sealed class ApiLogin
    {
        /// <summary>
        /// Gets the access token.
        /// </summary>
        public string Token { get; private set; }

        private readonly string _apiRoot;
        private DateTime? _expiration;

        public bool IsLogged => Token != null && DateTime.UtcNow < _expiration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiLogin"/> class.
        /// </summary>
        /// <param name="apiRootUri">The BLOB service API root URI (e.g.
        /// <c>www.myserver.com/api/</c>).</param>
        /// <exception cref="ArgumentNullException">apiRoot</exception>
        public ApiLogin(string apiRootUri)
        {
            _apiRoot = apiRootUri ?? throw new ArgumentNullException(nameof(apiRootUri));
            if (!_apiRoot.EndsWith("/")) _apiRoot += "/";
        }

        /// <summary>
        /// Logs the currently logged user out.
        /// </summary>
        public void Logout()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                _apiRoot + "auth/logout");
            AddAuthHeader(request);
            request.ContentType = "application/json";
            request.Method = "GET";
            request.GetResponse();

            Token = null;
            _expiration = null;
        }

        /// <summary>
        /// Logs the specified user in.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <returns>True on success.</returns>
        /// <exception cref="ArgumentNullException">user or password</exception>
        public bool Login(string user, string password)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (password == null) throw new ArgumentNullException(nameof(password));

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                _apiRoot + "auth/login");
            request.ContentType = "application/json";
            request.Method = "POST";

            using (StreamWriter writer =
                new StreamWriter(request.GetRequestStream()))
            {
                writer.Write("{\"username\":\"" + user + "\"," +
                            "\"password\":\"" + password + "\"}");
                writer.Flush();
            }

            HttpWebResponse httpResponse = (HttpWebResponse)request.GetResponse();
            string response;
            using (StreamReader reader = new StreamReader(
                httpResponse.GetResponseStream()))
            {
                response = reader.ReadToEnd();
            }
            JsonDocument doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("token", out JsonElement tokenElem))
            {
                Token = tokenElem.GetString();
                _expiration = doc.RootElement.GetProperty("expiration").GetDateTime();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds the authentication header to <paramref name="request"/>.
        /// </summary>
        /// <param name="request">The request.</param>
        public void AddAuthHeader(HttpWebRequest request)
        {
            if (Token != null)
                request.Headers.Add("Authentication", "Bearer " + Token);
        }
    }
}
