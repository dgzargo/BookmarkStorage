using System;
using System.Collections.Generic;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Interfaces;

namespace VcogBookmark.Shared.Models
{
    public class Bookmark : FilesGroup
    {
        public Bookmark(string bookmarkName, DateTime lastTime, IFileDataProviderService providerService) : base(bookmarkName, lastTime, providerService)
        {
            FileTypes = new[] {BookmarkFileType.BookmarkBody, BookmarkFileType.BookmarkImage};
        }

        public override IEnumerable<BookmarkFileType> FileTypes { get; }
    }
}