using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.Shared.Services
{
    public class BookmarkNetworkService : AbstractStorageService, IFileDataProviderService
    {
        private readonly BookmarkHierarchyUtils _bookmarkHierarchyUtils;
        private readonly string _hierarchyRoot;
        private readonly Uri _baseUri;

        public BookmarkNetworkService(string baseAddress, string? hierarchyRoot = null, BookmarkHierarchyUtils? bookmarkHierarchyUtils = null)
        {
            _baseUri = new Uri(baseAddress);
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

        public override async Task<bool> Save(FilesGroup filesGroup, FileWriteMode writeMode)
        {
            const string createPartialAddress = Endpoints.BookmarkControllerRoute + "/" + Endpoints.CreateEndpoint;
            const string updatePartialAddress = Endpoints.BookmarkControllerRoute + "/" + Endpoints.UpdateEndpoint;
            var endpointPath = writeMode switch
            {
                FileWriteMode.Override => updatePartialAddress,
                FileWriteMode.CreateNew => createPartialAddress,
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
            
            using var httpClient = new HttpClient{BaseAddress = _baseUri};
            using var responseMessage = await httpClient.PostAsync(endpointPath, multipartFormDataContent).ConfigureAwait(false);
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
            const string requestPartialAddress = Endpoints.BookmarkControllerRoute + "/" + Endpoints.DeleteBookmarkEndpoint;
            
            var fullBookmarkPath = GetFullPath(filesGroup);
            var stringContent = new StringContent(fullBookmarkPath);

            var multipartFormDataContent = new MultipartFormDataContent
            {
                {stringContent, "bookmarkPath"}
            };

            using var client = new HttpClient {BaseAddress = _baseUri};
            using var responseMessage = await client.PostAsync(requestPartialAddress, multipartFormDataContent).ConfigureAwait(false);
            return responseMessage.IsSuccessStatusCode;
        }

        public override async Task<bool> DeleteDirectory(Folder folder, bool withContentWithin)
        {
            const string requestPartialAddress = Endpoints.BookmarkControllerRoute + "/" + Endpoints.DeleteBookmarkFolderEndpoint;

            var fullBookmarkPath = GetFullPath(folder);
            var pathContent = new StringContent(fullBookmarkPath);
            
            var withContentWithinContent = new StringContent(withContentWithin.ToString());

            var multipartFormDataContent = new MultipartFormDataContent
            {
                {pathContent, "directoryPath"},
                {withContentWithinContent, "withContentWithin"}
            };

            using var client = new HttpClient {BaseAddress = _baseUri};
            using var responseMessage = await client.PostAsync(requestPartialAddress, multipartFormDataContent).ConfigureAwait(false);
            return responseMessage.IsSuccessStatusCode;
        }

        public override async Task<Folder?> GetHierarchy()
        {
            const string requestPartialAddress = Endpoints.BookmarkControllerRoute + "/" + Endpoints.GetHierarchyEndpoint;
            using var client = new HttpClient {BaseAddress = _baseUri};
            using var responseMessage = await client.GetAsync($"{requestPartialAddress}?root={_hierarchyRoot}").ConfigureAwait(false);
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
            const string requestPartialAddress = Endpoints.BookmarkControllerRoute + "/" + Endpoints.ClearEndpoint;
            
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
            
            using var client = new HttpClient {BaseAddress = _baseUri};
            using var responseMessage = await client.PostAsync(requestPartialAddress, multipartFormDataContent).ConfigureAwait(false);
            return responseMessage.IsSuccessStatusCode;
        }

        public override async Task<bool> Move(BookmarkHierarchyElement element, string newPath)
        {
            const string requestPartialAddress = Endpoints.BookmarkControllerRoute + "/" + Endpoints.MoveEndpoint;
            
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
            
            using var client = new HttpClient {BaseAddress = _baseUri};
            using var responseMessage = await client.PostAsync(requestPartialAddress, multipartFormDataContent).ConfigureAwait(false);
            return responseMessage.IsSuccessStatusCode;
        }

        public async Task<Stream> GetData(FileProfile fileProfile)
        {
            var fullPath = GetFullPath(fileProfile);
            using var client = new HttpClient {BaseAddress = _baseUri};
            var responseMessage = await client.GetAsync(fullPath).ConfigureAwait(false);
            responseMessage.EnsureSuccessStatusCode();
            var dataStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return dataStream;
        }

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