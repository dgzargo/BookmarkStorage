using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using VcogBookmark.Shared.Interfaces;

namespace VcogBookmark.Shared.Services
{
    public class NetworkFolderChangeWatcher : IFolderChangeWatcher, IDisposable
    {
        private readonly Uri _serverUrl;
        private readonly IAccountTokenController _accountTokenController;
        private readonly string _relativePath;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private HttpClient? _httpClient;

        public NetworkFolderChangeWatcher(string serverUrl, IAccountTokenController accountTokenController, string? relativePath = null)
        {
            _serverUrl = new Uri(serverUrl);
            _accountTokenController = accountTokenController;
            if (string.IsNullOrEmpty(relativePath))
            {
                _relativePath = "/";
            }
            else
            {
                var rootPathFragments = relativePath!.Split('/', Path.DirectorySeparatorChar)
                    .Where(fragment => !string.IsNullOrWhiteSpace(fragment)).ToArray();
                _relativePath = '/' + string.Join("/", rootPathFragments) + '/';
            }

            _cancellationTokenSource = new CancellationTokenSource();
            FolderChanged = Observable.FromAsync(async token =>
            {
                using var mergedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, _cancellationTokenSource.Token);
                return await WatchOneChange(mergedCancellationTokenSource.Token).ConfigureAwait(false);
            }).Repeat();
        }

        private async Task<HttpClient> CreateHttpClient()
        {
            var token = await _accountTokenController.GetToken();
            var httpClient = new HttpClient
            {
                BaseAddress = _serverUrl,
                Timeout = Timeout.InfiniteTimeSpan,
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return httpClient;
        }

        private async Task<string> WatchOneChange(CancellationToken cancellationToken)
        {
            const string address = Endpoints.BookmarkControllerRoute + "/" + Endpoints.WatchChangesEndpoint;
            _httpClient ??= await CreateHttpClient();
            using var httpResponseMessage = await _httpClient.GetAsync($"{address}?root={_relativePath}", cancellationToken).ConfigureAwait(false);
            httpResponseMessage.EnsureSuccessStatusCode();
            return await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _httpClient?.Dispose();
        }

        public IObservable<string> FolderChanged { get; }
    }
}