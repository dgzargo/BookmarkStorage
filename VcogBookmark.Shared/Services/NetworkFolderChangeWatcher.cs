using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using VcogBookmark.Shared.Interfaces;

namespace VcogBookmark.Shared.Services
{
    public class NetworkFolderChangeWatcher : IFolderChangeWatcher
    {
        private readonly Uri _serverUrl;
        private readonly string _relativePath;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public NetworkFolderChangeWatcher(string serverUrl, string? relativePath = null)
        {
            _serverUrl = new Uri(serverUrl);
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

        private async Task<string> WatchOneChange(CancellationToken cancellationToken)
        {
            const string address = Endpoints.BookmarkControllerRoute + "/" + Endpoints.WatchChangesEndpoint;
            var httpClient = new HttpClient {BaseAddress = _serverUrl};
            var httpResponseMessage = await httpClient.GetAsync($"{address}?root={_relativePath}", cancellationToken).ConfigureAwait(false);
            httpResponseMessage.EnsureSuccessStatusCode();
            return await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        public IObservable<string> FolderChanged { get; }
    }
}