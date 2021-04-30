using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VcogBookmark.Shared.Services
{
    public interface IAccountTokenController
    {
        Task<string> GetToken();
    }
    
    public class AccountTokenController : IAccountTokenController
    {
        private readonly Uri _serverUri;
        private readonly string _username;
        private readonly string _password;
        private string _token;

        public AccountTokenController(string serverUrl, string username, string password)
            : this(new Uri(serverUrl), username, password)
        {
        }
        public AccountTokenController(Uri serverUri, string username, string password)
        {
            _serverUri = serverUri;
            _username = username;
            _password = password;
        }

        private async Task<string> ReceiveToken()
        {
            var bodyContent = new MultipartFormDataContent
            {
                {new StringContent(_username, Encoding.UTF8, "text/plain"), "username"},
                {new StringContent(_password, Encoding.UTF8, "text/plain"), "password"},
            };
            using (var client = new HttpClient {BaseAddress = _serverUri})
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, "account/get-token") {Content = bodyContent})
            using (var responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                responseMessage.EnsureSuccessStatusCode();
                return await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        public async Task<string> GetToken()
        {
            if (_token == null)
            {
                _token = await ReceiveToken().ConfigureAwait(false);
            }
            return _token;
        }
    }

    public class AccountCachedTokenController : IAccountTokenController
    {
        private readonly string _token;

        public AccountCachedTokenController(string token)
        {
            _token = token;
        }
        
        public Task<string> GetToken()
        {
            return Task.FromResult(_token);
        }
    }
}