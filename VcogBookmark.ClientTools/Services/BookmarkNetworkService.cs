using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using VcogBookmark.Shared;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;
using VcogBookmark.Shared.Services;

namespace VcogBookmark.ClientTools.Services
{
    public class BookmarkNetworkService
    {
        private readonly BookmarkHierarchyService _bookmarkHierarchyService;
        private readonly string _hierarchyRoot;
        private readonly Uri _baseUri;

        public BookmarkNetworkService(string baseAddress, BookmarkHierarchyService bookmarkHierarchyService, string? hierarchyRoot = null)
        {
            _bookmarkHierarchyService = bookmarkHierarchyService;
            _hierarchyRoot = hierarchyRoot ?? string.Empty;
            if (_hierarchyRoot.Any(c => c == '\\')) throw new ArgumentException();
            _baseUri = new Uri(baseAddress);
        }
        
        public async Task<BookmarkFolder> GetHierarchy()
        {
            const string requestPartialAddress = Endpoints.BookmarkControllerRoute + "/" + Endpoints.GetHierarchyEndpoint;
            using var client = new HttpClient {BaseAddress = _baseUri};
            var responseMessage = await client.GetAsync($"{requestPartialAddress}?root={_hierarchyRoot}");
            responseMessage.EnsureSuccessStatusCode();
            var response = await responseMessage.Content.ReadAsStringAsync();
            return _bookmarkHierarchyService.Parse(response);
        }

        public async Task<FileProfile> GetFile(string filePath, BookmarkFileType fileType)
        {
            var fullFilePath = Path.Combine(_hierarchyRoot, filePath).Replace('\\', '/');
            var fileExtension = fileType.GetExtension();
            
            using var client = new HttpClient {BaseAddress = _baseUri};
            var responseMessage = await client.GetAsync($"{fullFilePath}.{fileExtension}");
            responseMessage.EnsureSuccessStatusCode();
            var dataStream = await responseMessage.Content.ReadAsStreamAsync();
            var dateHeaderReceived = responseMessage.Headers.TryGetValues("File-Last-Modified", out var values);
            if (!dateHeaderReceived) throw new Exception();
            var date = DateTime.Parse(values!.First()).ToUniversalTime();
            return new FileProfile(dataStream, filePath, fileType, date);
        }

        public IEnumerable<Task<FileProfile>> GetAllBookmarkFiles(string filePath)
        {
            return  EnumsHelper.AllEnumValues<BookmarkFileType>().Select(fileType => GetFile(filePath, fileType)); // todo
        }
        
        public async Task<bool> LoadBookmarkToTheServer(Stream textFile, Stream imageFile, string bookmarkPath,
            bool makeNewBookmark)
        {
            const string createPartialAddress = Endpoints.BookmarkControllerRoute + "/" + Endpoints.CreateEndpoint;
            const string updatePartialAddress = Endpoints.BookmarkControllerRoute + "/" + Endpoints.UpdateEndpoint;

            var bookmarkName = Path.GetFileNameWithoutExtension(bookmarkPath);
            
            var textContent = new StreamContent(textFile);
            // textContent.Headers
            
            var imageContent = new StreamContent(imageFile);
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");

            var fullBookmarkPath = Path.Combine(_hierarchyRoot, bookmarkPath).Replace('\\', '/');
            var stringContent = new StringContent(fullBookmarkPath);
            // stringContent.Headers

            var multipartFormDataContent = new MultipartFormDataContent
            {
                {textContent, "textFile", $"{bookmarkName}.{BookmarkFileType.BookmarkBody.GetExtension()}"},
                {imageContent, "imageFile", $"{bookmarkName}.{BookmarkFileType.BookmarkImage.GetExtension()}"},
                {stringContent, "bookmarkPath"},
            };
            
            using var httpClient = new HttpClient{BaseAddress = _baseUri};
            var responseMessage = await httpClient.PostAsync(makeNewBookmark? createPartialAddress : updatePartialAddress, multipartFormDataContent);
            return responseMessage.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteBookmarkFromTheServer(string bookmarkPath)
        {
            const string requestPartialAddress = Endpoints.BookmarkControllerRoute + "/" + Endpoints.DeleteBookmarkEndpoint;
            
            var fullBookmarkPath = Path.Combine(_hierarchyRoot, bookmarkPath).Replace('\\', '/');
            var stringContent = new StringContent(fullBookmarkPath);

            var multipartFormDataContent = new MultipartFormDataContent
            {
                {stringContent, "bookmarkPath"}
            };

            using var client = new HttpClient {BaseAddress = _baseUri};
            var responseMessage = await client.PostAsync(requestPartialAddress, multipartFormDataContent);
            return responseMessage.IsSuccessStatusCode;
        }
    }
}