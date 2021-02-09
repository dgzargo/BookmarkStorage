using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VcogBookmark.Shared;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;
using VcogBookmark.Shared.Services;
using VcogBookmarkServer.Services;

namespace VcogBookmarkServer.Controllers
{
    [ApiController, Route(Endpoints.BookmarkControllerRoute)]
    public class BookmarksController: ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly BookmarkHierarchyUtils _hierarchyUtils;

        public BookmarksController(IStorageService storageService, BookmarkHierarchyUtils hierarchyUtils)
        {
            _storageService = storageService;
            _hierarchyUtils = hierarchyUtils;
        }
        
        [HttpPost(Endpoints.CreateEndpoint)]
        public Task<IActionResult> InsertBookmark(IFormFileCollection formFileCollection, [FromForm]string bookmarkPath, [FromServices] GroupFilesDataService dataService)
        {
            return StoreBookmark(formFileCollection, bookmarkPath, DateTime.UtcNow, dataService, FileWriteMode.CreateNew);
        }

        [HttpPost(Endpoints.UpdateEndpoint)]
        public Task<IActionResult> UpdateBookmark(IFormFileCollection formFileCollection, [FromForm] string bookmarkPath, [FromServices] GroupFilesDataService dataService)
        {
            return StoreBookmark(formFileCollection, bookmarkPath, DateTime.UtcNow, dataService, FileWriteMode.Override);
        }

        private async Task<IActionResult> StoreBookmark(IFormFileCollection formFileCollection, string bookmarkPath, DateTime lastWriteDate,
            GroupFilesDataService dataService, FileWriteMode writeMode)
        {
            bookmarkPath = bookmarkPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            lastWriteDate = new DateTime(lastWriteDate.Year, lastWriteDate.Month, lastWriteDate.Day, lastWriteDate.Hour, lastWriteDate.Minute, lastWriteDate.Second, lastWriteDate.Kind); // truncate milliseconds off
            
            foreach (var formFile in formFileCollection)
            {
                var formFileHeader = formFile.Headers["Extension"];
                dataService.Add(EnumsHelper.ParseType(formFileHeader), formFile.OpenReadStream());
            }

            var fileTypes = formFileCollection.Select(formFile => formFile.Headers["Extension"].ToString()).Select(EnumsHelper.ParseType).ToArray();
            FilesGroup filesGroup;
            if (fileTypes.Contains(BookmarkFileType.BookmarkBody) && fileTypes.Contains(BookmarkFileType.BookmarkImage))
            {
                filesGroup = new Bookmark(bookmarkPath, lastWriteDate) {ProviderService = dataService};
            }
            else
            {
                return BadRequest("bookmark type isn't recognized");
            }
            var result = await _storageService.Save(filesGroup, writeMode);
            return result ? (IActionResult) Ok() : BadRequest();
        }
        
        [HttpPost(Endpoints.DeleteBookmarkEndpoint)]
        public async Task<IActionResult> DeleteBookmark([FromForm]string bookmarkPath)
        {
            bookmarkPath = bookmarkPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var filesGroup = await _storageService.Find(bookmarkPath);
            if (filesGroup == null) return BadRequest();
            var deletionResult = await _storageService.DeleteBookmark(filesGroup);
            return deletionResult ? (IActionResult) Ok() : BadRequest();
        }

        [HttpPost(Endpoints.DeleteBookmarkFolderEndpoint)]
        public async Task<IActionResult> DeleteDirectory([FromForm]string directoryPath)
        {
            directoryPath = directoryPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var folder = await _storageService.FindFolder(directoryPath);
            if (folder == null) return BadRequest();
            var deletionResult = await _storageService.DeleteDirectoryWithContentWithin(folder);
            return deletionResult ? (IActionResult) Ok() : BadRequest();
        }

        [HttpGet(Endpoints.GetHierarchyEndpoint)]
        public async Task<ActionResult<string>> Hierarchy([FromQuery]string? root)
        {
            var foundFolder = await _storageService.FindFolder(root ?? string.Empty);
            if (foundFolder == null) return BadRequest();
            return _hierarchyUtils.ToAlignedJson(foundFolder);
        }
        
        private string GetPureFileExtension(string fileName)
        {
            var dotIndex = fileName.LastIndexOf('.') + 1;
            return fileName.Substring(dotIndex, fileName.Length - dotIndex);
        }
    }
}