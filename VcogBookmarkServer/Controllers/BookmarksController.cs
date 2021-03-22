using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VcogBookmark.Shared;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;
using VcogBookmark.Shared.Services;

namespace VcogBookmarkServer.Controllers
{
    [ApiController, Route(Endpoints.BookmarkControllerRoute)]
    public class BookmarksController: ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly IFolderChangeWatcher _changeWatcher;
        private readonly BookmarkHierarchyUtils _hierarchyUtils;

        public BookmarksController(IStorageService storageService, IFolderChangeWatcher changeWatcher, BookmarkHierarchyUtils hierarchyUtils)
        {
            _storageService = storageService;
            _changeWatcher = changeWatcher;
            _hierarchyUtils = hierarchyUtils;
        }
        
        [HttpPost(Endpoints.CreateEndpoint)]
        public Task<IActionResult> InsertBookmark([FromForm]string bookmarkPath)
        {
            return StoreBookmark(Request.Form.Files, bookmarkPath, DateTime.UtcNow, FileWriteMode.CreateNew);
        }

        [HttpPost(Endpoints.UpdateEndpoint)]
        public Task<IActionResult> UpdateBookmark([FromForm]string bookmarkPath)
        {
            return StoreBookmark(Request.Form.Files, bookmarkPath, DateTime.UtcNow, FileWriteMode.Override);
        }

        private async Task<IActionResult> StoreBookmark(IFormFileCollection formFileCollection, string bookmarkPath, DateTime lastWriteDate,
            FileWriteMode writeMode)
        {
            FormatPath(ref bookmarkPath);
            lastWriteDate = new DateTime(lastWriteDate.Year, lastWriteDate.Month, lastWriteDate.Day, lastWriteDate.Hour, lastWriteDate.Minute, lastWriteDate.Second, lastWriteDate.Kind); // truncate milliseconds off

            var dataDictionary = new Dictionary<BookmarkFileType, Stream>();
            foreach (var formFile in formFileCollection)
            {
                var formFileHeader = formFile.Headers["Extension"];
                dataDictionary.Add(EnumsHelper.ParseType(formFileHeader), formFile.OpenReadStream());
            }

            var fileTypes = formFileCollection.Select(formFile => formFile.Headers["Extension"].ToString()).Select(EnumsHelper.ParseType).ToArray();
            if (EnumsHelper.RecognizeFilesGroupType(fileTypes) == null)
            {
                return BadRequest("bookmark type isn't recognized");
            }
            var fakeFilesGroup = _storageService.MakeFake(bookmarkPath);
            var filesGroup = new ProxyFilesGroup(fakeFilesGroup, dataDictionary, lastWriteDate);
            var result = await _storageService.Save(filesGroup, writeMode);
            return result ? (IActionResult) Ok() : BadRequest();
        }
        
        [HttpPost(Endpoints.DeleteBookmarkEndpoint)]
        public async Task<IActionResult> DeleteBookmark([FromForm]string bookmarkPath)
        {
            FormatPath(ref bookmarkPath);
            var filesGroup = await _storageService.Find(bookmarkPath);
            if (filesGroup == null) return BadRequest();
            var deletionResult = await _storageService.DeleteBookmark(filesGroup);
            return deletionResult ? (IActionResult) Ok() : BadRequest();
        }

        [HttpPost(Endpoints.DeleteBookmarkFolderEndpoint)]
        public async Task<IActionResult> DeleteDirectory([FromForm]string directoryPath, [FromForm]bool withContentWithin)
        {
            FormatPath(ref directoryPath);
            var folder = await _storageService.FindFolder(directoryPath);
            if (folder == null) return BadRequest();
            var deletionResult = await _storageService.DeleteDirectory(folder, withContentWithin);
            return deletionResult ? (IActionResult) Ok() : BadRequest();
        }

        [HttpGet(Endpoints.GetHierarchyEndpoint)]
        public async Task<ActionResult<string>> Hierarchy([FromQuery]string? root)
        {
            var foundFolder = await _storageService.FindFolder(root ?? string.Empty);
            if (foundFolder == null) return NotFound();
            return _hierarchyUtils.ToAlignedJson(foundFolder);
        }

        [HttpPost(Endpoints.ClearEndpoint)]
        public async Task<IActionResult> Clear([FromForm]string directoryPath)
        {
            FormatPath(ref directoryPath);
            var folder = await _storageService.FindFolder(directoryPath);
            if (folder == null) return BadRequest();
            var clearResult = await _storageService.Clear(folder);
            return clearResult ? (IActionResult) Ok() : BadRequest();
        }

        [HttpPost(Endpoints.MoveEndpoint)]
        public async Task<IActionResult> Move([FromForm]string originalPath, [FromForm]string newPath, [FromForm]bool isFolder)
        {
            FormatPath(ref originalPath);
            FormatPath(ref newPath);
            var element = isFolder
                ? (BookmarkHierarchyElement?) await _storageService.FindFolder(originalPath)
                : await _storageService.Find(originalPath);
            if (element == null)
            {
                return BadRequest();
            }
            var moveResult = await _storageService.Move(element, newPath);
            return moveResult ? (IActionResult) Ok() : BadRequest();
        }

        [HttpGet(Endpoints.WatchChangesEndpoint)]
        public async Task<IActionResult> Watch([FromQuery]string root)
        {
            var pathFragments = root.Split('/').Where(fragment => !string.IsNullOrWhiteSpace(fragment));
            root = '/' + string.Join('/', pathFragments) + '/';
            if (root == "//") root = "/";
            var changedPath = await _changeWatcher.FolderChanged.Where(path => path.StartsWith(root)).FirstAsync().ToTask();
            return Ok('/' + changedPath.Substring(root.Length));
        }

        private void FormatPath(ref string path)
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }
    }
}