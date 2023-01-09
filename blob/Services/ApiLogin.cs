using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
            Token = "";
            if (!_apiRoot.EndsWith("/")) _apiRoot += "/";
        }

        /// <summary>
        /// Logs the currently logged user (if any) out.
        /// </summary>
        public async Task Logout()
        {
            if (Token == null) return;

            HttpClient client = new();
            AddAuthHeader(client);
            await client.GetAsync(_apiRoot + "auth/logout");

            Token = "";
            _expiration = null;
        }

        /// <summary>
        /// Logs the specified user in.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <returns>True on success.</returns>
        /// <exception cref="ArgumentNullException">user or password</exception>
        public async Task<bool> Login(string user, string password)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (password == null) throw new ArgumentNullException(nameof(password));

            HttpClient client = new();
            HttpResponseMessage response = await client.PostAsync(
                _apiRoot + "auth/login",
                new StringContent(JsonSerializer.Serialize(new
                {
                    username = user,
                    password = password
                }), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("token", out JsonElement tokenElem))
            {
                Token = tokenElem.GetString() ?? "";
                _expiration = doc.RootElement.GetProperty("expiration").GetDateTime();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds the authentication header to <paramref name="request"/>.
        /// </summary>
        /// <param name="client">The client.</param>
        public void AddAuthHeader(HttpClient client)
        {
            if (Token != null)
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", Token);
            }
        }
    }
}
