﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;
using VcogBookmark.Shared.Services;

namespace VcogBookmarkServer.Controllers
{
    [ApiController, Route("bookmarks")]
    public class BookmarksController: ControllerBase
    {
        private readonly StorageService _storageService;
        private readonly BookmarkHierarchyService _hierarchyService;

        public BookmarksController(StorageService storageService, BookmarkHierarchyService hierarchyService)
        {
            _storageService = storageService;
            _hierarchyService = hierarchyService;
        }
        
        [HttpPost("create")]
        public async Task<IActionResult> InsertBookmark(IFormFile textFile, IFormFile imageFile, [FromForm]string bookmarkPath)
        {
            if (GetPureFileExtension(textFile.FileName) != "vbm" || imageFile.ContentType != "image/jpeg")
                return BadRequest();
            
            bookmarkPath = bookmarkPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var date = DateTime.UtcNow;
            date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind); // truncate milliseconds off

            // var result = await _storageService.WriteFiles(textFile.OpenReadStream(), imageFile.OpenReadStream(), bookmarkPath, FileWriteMode.CreateNew);

            var result = await _storageService.SaveBookmark(
                new[]
                {
                    Task.FromResult(new FileProfile(textFile.OpenReadStream(), bookmarkPath, BookmarkFileType.BookmarkBody, date)),
                    Task.FromResult(new FileProfile(imageFile.OpenReadStream(), bookmarkPath, BookmarkFileType.BookmarkImage, date)),
                },
                FileWriteMode.CreateNew);
            
            return result ? (IActionResult) Ok() : BadRequest();
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateBookmark(IFormFile textFile, IFormFile imageFile, [FromForm] string bookmarkPath)
        {
            if (GetPureFileExtension(textFile.FileName) != "vbm" || imageFile.ContentType != "image/jpeg")
                return BadRequest();

            bookmarkPath = bookmarkPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var date = DateTime.UtcNow;
            date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind); // truncate milliseconds off

            // var result = await _storageService.WriteFiles(textFile.OpenReadStream(), imageFile.OpenReadStream(), bookmarkPath, FileWriteMode.Override);
            
            var result = await _storageService.SaveBookmark(
                new[]
                {
                    Task.FromResult(new FileProfile(textFile.OpenReadStream(), bookmarkPath, BookmarkFileType.BookmarkBody, date)),
                    Task.FromResult(new FileProfile(imageFile.OpenReadStream(), bookmarkPath, BookmarkFileType.BookmarkImage, date)),
                },
                FileWriteMode.Override);
            
            return result ? (IActionResult) Ok() : BadRequest();
        }

        [HttpPost("delete")]
        public IActionResult DeleteBookmark([FromForm]string bookmarkPath)
        {
            bookmarkPath = bookmarkPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            _storageService.DeleteBookmark(bookmarkPath);
            return Ok();
        }
        
        

        [HttpPost("delete-directory")]
        public IActionResult DeleteDirectory([FromForm]string directoryPath)
        {
            directoryPath = directoryPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            _storageService.DeleteDirectoryWithContentWithin(directoryPath);
            return Ok();
        }

        [HttpGet("hierarchy")]
        public ActionResult<string> Hierarchy([FromQuery]string? root)
        {
            var hierarchy = _storageService.GetHierarchy(root ?? string.Empty);
            return _hierarchyService.ToAlignedJson(hierarchy);
        }
        
        private string GetPureFileExtension(string fileName)
        {
            var dotIndex = fileName.LastIndexOf('.') + 1;
            return fileName.Substring(dotIndex, fileName.Length - dotIndex);
        }
    }
}