using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Interfaces;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.Shared.Services
{
    public class BookmarkNetworkService : AbstractStorageService, IFileDataProviderService
    {
        private readonly IAccountTokenController _accountTokenController;
        private readonly BookmarkHierarchyUtils _bookmarkHierarchyUtils;
        private readonly string _hierarchyRoot;
        private readonly HttpClient _httpClient;

        public BookmarkNetworkService(string baseAddress, IAccountTokenController accountTokenController, string? hierarchyRoot = null, BookmarkHierarchyUtils? bookmarkHierarchyUtils = null)
        {
            if (baseAddress.Last() != '/') throw new ArgumentException("URL should end with '/'!", nameof(baseAddress));
            _httpClient = new HttpClient {BaseAddress = new Uri($"{baseAddress}{Endpoints.BookmarkControllerRoute}/")};
            _accountTokenController = accountTokenController;
            _bookmarkHierarchyUtils = bookmarkHierarchyUtils ?? BookmarkHierarchyUtils.Instance;
            if (hierarchyRoot == null)
            {
                _hierarchyRoot = string.Empty;
            }
            else
            {
                var rootPathFragments = hierarchyRoot.Split('/', Path.DirectorySeparatorChar)
                    .Where(fragment => !string.IsNullOrWhiteSpace(fragment)).ToArray();
                _hierarchyRoot = string.Join("/", rootPathFragments);
            }
        }

        private async Task<HttpRequestMessage> CreateHttpRequestMessageWithAuthHeader(HttpMethod method, string requestUri, HttpContent? content = null)
        {
            var token = await _accountTokenController.GetToken();
            var httpRequestMessage = new HttpRequestMessage(method, requestUri) {Content = content};
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return httpRequestMessage;
        }

        public override async Task<bool> Save(FilesGroup filesGroup, FileWriteMode writeMode)
        {
            var endpointPath = writeMode switch
            {
                FileWriteMode.Override => Endpoints.UpdateEndpoint,
                FileWriteMode.CreateNew => Endpoints.CreateEndpoint,
                _ => throw new ArgumentOutOfRangeException(nameof(writeMode), writeMode, "such operation can't be done")
            };

            var multipartFormDataContent = new MultipartFormDataContent();

            var streamData = await Task.WhenAll(filesGroup.RelatedFiles.Select(MakeStreamContent)).ConfigureAwait(false);

            var fullBookmarkPath = GetFullPath(filesGroup);
            var stringContent = new StringContent(fullBookmarkPath);
            
            foreach (var streamContent in streamData)
            {
                var ext = streamContent.Headers.First(kvp => kvp.Key == "Extension").Value.First()!;
                multipartFormDataContent.Add(streamContent, EnumsHelper.ParseType(ext).ToString(), $"{filesGroup.Name}.{ext}");
            }
            multipartFormDataContent.Add(stringContent, "bookmarkPath");

            using var httpRequestMessage = await CreateHttpRequestMessageWithAuthHeader(HttpMethod.Post, endpointPath, multipartFormDataContent).ConfigureAwait(false);
            using var responseMessage = await _httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            return responseMessage.IsSuccessStatusCode;
        }

        private async Task<StreamContent> MakeStreamContent(FileProfile fileProfile)
        {
            var steam = await fileProfile.GetData().ConfigureAwait(false);
            var content = new StreamContent(steam);
            content.Headers.Add("Extension", fileProfile.FileType.GetExtension());
            return content;
        }

        public override async Task<bool> DeleteBookmark(FilesGroup filesGroup)
        {
            var fullBookmarkPath = GetFullPath(filesGroup);
            var stringContent = new StringContent(fullBookmarkPath);

            var multipartFormDataContent = new MultipartFormDataContent
            {
                {stringContent, "bookmarkPath"}
            };

            using var httpRequestMessage = await CreateHttpRequestMessageWithAuthHeader(HttpMethod.Post, Endpoints.DeleteBookmarkEndpoint, multipartFormDataContent).ConfigureAwait(false);
            using var responseMessage = await _httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            return responseMessage.IsSuccessStatusCode;
        }

        public override async Task<bool> DeleteDirectory(Folder folder, bool withContentWithin)
        {
            var fullBookmarkPath = GetFullPath(folder);
            var pathContent = new StringContent(fullBookmarkPath);
            
            var withContentWithinContent = new StringContent(withContentWithin.ToString());

            var multipartFormDataContent = new MultipartFormDataContent
            {
                {pathContent, "directoryPath"},
                {withContentWithinContent, "withContentWithin"},
            };

            using var httpRequestMessage = await CreateHttpRequestMessageWithAuthHeader(HttpMethod.Post, Endpoints.DeleteBookmarkFolderEndpoint, multipartFormDataContent).ConfigureAwait(false);
            using var responseMessage = await _httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            return responseMessage.IsSuccessStatusCode;
        }

        public override async Task<Folder?> GetHierarchy()
        {
            using var httpRequestMessage = await CreateHttpRequestMessageWithAuthHeader(HttpMethod.Get, $"{Endpoints.GetHierarchyEndpoint}?root={_hierarchyRoot}").ConfigureAwait(false);
            using var responseMessage = await _httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            if (responseMessage.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            responseMessage.EnsureSuccessStatusCode();
            var response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            var hierarchy = _bookmarkHierarchyUtils.Parse(response, this);
            return hierarchy;
        }

        public override async Task<bool> Clear(Folder folder)
        {
            var fullPath = GetFullPath(folder);
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                fullPath = "/";
            }
            var pathContent = new StringContent(fullPath);

            var multipartFormDataContent = new MultipartFormDataContent
            {
                {pathContent, "directoryPath"},
            };
            
            using var httpRequestMessage = await CreateHttpRequestMessageWithAuthHeader(HttpMethod.Post, Endpoints.ClearEndpoint, multipartFormDataContent).ConfigureAwait(false);
            using var responseMessage = await _httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            return responseMessage.IsSuccessStatusCode;
        }

        public override async Task<bool> Move(BookmarkHierarchyElement element, string newPath)
        {
            var originalFullPath = GetFullPath(element);
            var originalPathContent = new StringContent(originalFullPath);
            
            var newFullPath = GetFullPath(newPath);
            var newPathContent = new StringContent(newFullPath);

            var isFolder = element is Folder;
            var isFolderContent = new StringContent(isFolder.ToString());

            var multipartFormDataContent = new MultipartFormDataContent
            {
                {originalPathContent, "originalPath"},
                {newPathContent, "newPath"},
                {isFolderContent, "isFolder"},
            };
            
            using var httpRequestMessage = await CreateHttpRequestMessageWithAuthHeader(HttpMethod.Post, Endpoints.MoveEndpoint, multipartFormDataContent).ConfigureAwait(false);
            using var responseMessage = await _httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            return responseMessage.IsSuccessStatusCode;
        }

        public async Task<Stream> GetData(FileProfile fileProfile)
        {
            var filePathContent = new StringContent(fileProfile.LocalPath, Encoding.UTF8, "text/plain");
            var fileTypeContent = new StringContent(fileProfile.FileType.ToString(), Encoding.UTF8, "text/plain");
            var multipartFormDataContent = new MultipartFormDataContent
            {
                {filePathContent, "filePath"},
                {fileTypeContent, "fileType"},
            };
            using var httpRequestMessage = await CreateHttpRequestMessageWithAuthHeader(HttpMethod.Get, Endpoints.GetFileEndpoint, multipartFormDataContent).ConfigureAwait(false);
            var responseMessage = await _httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            responseMessage.EnsureSuccessStatusCode();
            var dataStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return dataStream;
        }

        [Obsolete("Trying to get rid of methods like this.")]
        private string GetFullPath(FileProfile fileProfile)
        {
            var initial = new StringBuilder(fileProfile.LocalPath.Length + 5);
            initial.Append(fileProfile.LocalPath, 1, fileProfile.LocalPath.Length - 1);
            initial.Append('.');
            initial.Append(fileProfile.FileType.GetExtension());
            return Path.Combine(_hierarchyRoot, initial.ToString()).Replace('\\', '/');
        }

        private string GetFullPath(BookmarkHierarchyElement hierarchyElement)
        {
            var partialPath = hierarchyElement.LocalPath;
            if (partialPath.Length > 0)
            {
                var firstChar = partialPath[0];
                if (firstChar == '\\' || firstChar == '/')
                {
                    partialPath = partialPath.Substring(1);
                }
            }
            return Path.Combine(_hierarchyRoot, partialPath).Replace('\\', '/');
        }
        
        private string GetFullPath(string partialPath)
        {
            return Path.Combine(_hierarchyRoot, partialPath).Replace('\\', '/');
        }
    }
}