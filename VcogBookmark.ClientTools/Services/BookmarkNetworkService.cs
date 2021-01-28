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
        private readonly Uri _baseUri;

        public BookmarkNetworkService(string baseAddress, BookmarkHierarchyService bookmarkHierarchyService)
        {
            _bookmarkHierarchyService = bookmarkHierarchyService;
            _baseUri = new Uri(baseAddress);
        }
        
        public async Task<BookmarkFolder> GetHierarchy()
        {
            const string requestPartialAddress = "bookmarks/hierarchy";
            using var client = new HttpClient {BaseAddress = _baseUri};
            var responseMessage = await client.GetAsync(requestPartialAddress);
            responseMessage.EnsureSuccessStatusCode();
            var response = await responseMessage.Content.ReadAsStringAsync();
            return _bookmarkHierarchyService.Parse(response);
        }

        public async Task<FileProfile> GetFile(string filePath, BookmarkFileType fileType)
        {
            var client = new HttpClient {BaseAddress = _baseUri};
            var fileExtension = fileType.GetExtension();
            var responseMessage = await client.GetAsync($"{filePath}.{fileExtension}");
            var headers = responseMessage.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            responseMessage.EnsureSuccessStatusCode();
            var dataStream = await responseMessage.Content.ReadAsStreamAsync();
            return new FileProfile(dataStream, filePath, fileType, DateTime.Today);
        }

        public IEnumerable<Task<FileProfile>> GetAllBookmarkFiles(string filePath)
        {
            return  EnumsHelper.AllEnumValues<BookmarkFileType>().Select(fileType => GetFile(filePath, fileType));
        }

        public Task LoadBookmarkToTheServer(IEnumerable<FileProfile> fileProfiles, bool makeNewBookmark)
        {
            // var bookmarkPath =
            throw new NotImplementedException();
        }
        
        public async Task<bool> LoadBookmarkToTheServer(Stream textFile, Stream imageFile, string bookmarkPath,
            bool makeNewBookmark)
        {
            var bookmarkName = LastSubstring(bookmarkPath, '/');
            using var httpClient = new HttpClient{BaseAddress = _baseUri};
            
            var textContent = new StreamContent(textFile);
            // textContent.Headers
            
            var imageContent = new StreamContent(imageFile);
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");

            var stringContent = new StringContent(bookmarkPath);
            // stringContent.Headers

            var multipartFormDataContent = new MultipartFormDataContent
            {
                {textContent, "textFile", $"{bookmarkName}.{BookmarkFileType.BookmarkBody.GetExtension()}"},
                {imageContent, "imageFile", $"{bookmarkName}.{BookmarkFileType.BookmarkImage.GetExtension()}"},
                {stringContent, "bookmarkPath"},
            };
            var responseMessage = await httpClient.PostAsync(makeNewBookmark? "bookmarks/create" : "bookmarks/update", multipartFormDataContent);
            return responseMessage.IsSuccessStatusCode;

            string LastSubstring(string s, char c)
            {
                var lastIndex = bookmarkPath.LastIndexOf(c);
                if (lastIndex == -1) lastIndex = 0;
                return s.Substring(lastIndex, s.Length - lastIndex);
            }
        }

        public async Task<bool> DeleteBookmarkFromTheServer(string bookmarkPath)
        {
            const string requestPartialAddress = "bookmarks/delete";
            using var client = new HttpClient {BaseAddress = _baseUri};
            
            var stringContent = new StringContent(bookmarkPath);
            var multipartFormDataContent = new MultipartFormDataContent
            {
                {stringContent, "bookmarkPath"},
            };
            
            var responseMessage = await client.PostAsync(requestPartialAddress, multipartFormDataContent);
            return responseMessage.IsSuccessStatusCode;
        }
    }
}