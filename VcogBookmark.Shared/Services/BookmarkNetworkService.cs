using System;
using System.IO;
using System.Linq;
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

        public BookmarkNetworkService(string baseAddress, BookmarkHierarchyUtils bookmarkHierarchyUtils, string? hierarchyRoot = null)
        {
            _baseUri = new Uri(baseAddress);
            _bookmarkHierarchyUtils = bookmarkHierarchyUtils;
            _hierarchyRoot = hierarchyRoot ?? string.Empty;
            if (_hierarchyRoot.Any(c => c == '\\')) throw new ArgumentException();
        }

        public override async Task<bool> Save(FilesGroup filesGroup, FileWriteMode writeMode)
        {
            const string createPartialAddress = Endpoints.BookmarkControllerRoute + "/" + Endpoints.CreateEndpoint;
            const string updatePartialAddress = Endpoints.BookmarkControllerRoute + "/" + Endpoints.UpdateEndpoint;
            var endpointPath = writeMode switch
            {
                FileWriteMode.Override => updatePartialAddress,
                FileWriteMode.CreateNew => createPartialAddress,
                FileWriteMode.NotStrict => throw new NotImplementedException(),
                _ => throw new ArgumentOutOfRangeException(nameof(writeMode), writeMode, null)
            };

            var multipartFormDataContent = new MultipartFormDataContent();

            var streamData = await Task.WhenAll(filesGroup.RelatedFiles.Select(MakeStreamContent));

            var fullBookmarkPath = GetFullPath(filesGroup);
            var stringContent = new StringContent(fullBookmarkPath);
            
            foreach (var streamContent in streamData)
            {
                multipartFormDataContent.Add(streamContent);
            }
            multipartFormDataContent.Add(stringContent, "bookmarkPath");
            
            using var httpClient = new HttpClient{BaseAddress = _baseUri};
            var responseMessage = await httpClient.PostAsync(endpointPath, multipartFormDataContent);
            return responseMessage.IsSuccessStatusCode;
        }

        private async Task<StreamContent> MakeStreamContent(FileProfile fileProfile)
        {
            var steam = await fileProfile.GetData();
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
            var responseMessage = await client.PostAsync(requestPartialAddress, multipartFormDataContent);
            return responseMessage.IsSuccessStatusCode;
        }

        public override async Task<bool> DeleteDirectoryWithContentWithin(Folder folder)
        {
            const string requestPartialAddress = Endpoints.BookmarkControllerRoute + "/" + Endpoints.DeleteBookmarkFolderEndpoint;

            var fullBookmarkPath = GetFullPath(folder);
            var stringContent = new StringContent(fullBookmarkPath);

            var multipartFormDataContent = new MultipartFormDataContent
            {
                {stringContent, "bookmarkPath"}
            };

            using var client = new HttpClient {BaseAddress = _baseUri};
            var responseMessage = await client.PostAsync(requestPartialAddress, multipartFormDataContent);
            return responseMessage.IsSuccessStatusCode;
        }

        public override async Task<Folder> GetHierarchy()
        {
            const string requestPartialAddress = Endpoints.BookmarkControllerRoute + "/" + Endpoints.GetHierarchyEndpoint;
            using var client = new HttpClient {BaseAddress = _baseUri};
            var responseMessage = await client.GetAsync($"{requestPartialAddress}?root={_hierarchyRoot}");
            responseMessage.EnsureSuccessStatusCode();
            var response = await responseMessage.Content.ReadAsStringAsync();
            var hierarchy = _bookmarkHierarchyUtils.Parse(response);
            hierarchy.ProviderService = this;
            return hierarchy;
        }

        public override async Task<bool> Clear(Folder folder)
        {
            var fullPath = GetFullPath(folder);
            using var client = new HttpClient {BaseAddress = _baseUri};
            var responseMessage = await client.PostAsync(fullPath, null);
            return responseMessage.IsSuccessStatusCode;
        }

        public async Task<Stream> GetData(FileProfile fileProfile)
        {
            var fullPath = GetFullPath(fileProfile);
            using var client = new HttpClient {BaseAddress = _baseUri};
            var responseMessage = await client.GetAsync(fullPath);
            responseMessage.EnsureSuccessStatusCode();
            var dataStream = await responseMessage.Content.ReadAsStreamAsync();
            return dataStream;
        }

        private string GetFullPath(FileProfile fileProfile)
        {
            var initial = new StringBuilder(fileProfile.LocalPath.Length + 5);
            initial.Append(fileProfile.LocalPath);
            initial.Replace('\\', '/');
            initial.Append('.');
            initial.Append(fileProfile.FileType.GetExtension());
            return Path.Combine(_hierarchyRoot, initial.ToString());
        }

        private string GetFullPath(BookmarkHierarchyElement hierarchyElement)
        {
            return Path.Combine(_hierarchyRoot, hierarchyElement.LocalPath).Replace('\\', '/');
        }
    }
}