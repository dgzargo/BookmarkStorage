using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.Shared
{
    public static class EnumsHelper
    {
        public static string GetExtension(this BookmarkFileType fileType)
        {
            return fileType switch
            {
                BookmarkFileType.BookmarkBody => "vbm",
                BookmarkFileType.BookmarkImage => "jpg",
                _ => throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null)
            };
        }

        public static BookmarkFileType ParseType(string fileExtension)
        {
            return fileExtension switch
            {
                "vbm" => BookmarkFileType.BookmarkBody,
                "jpg" => BookmarkFileType.BookmarkImage,
                _ => throw new ArgumentOutOfRangeException(nameof(fileExtension), fileExtension, null)
            };
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public static Type? RecognizeFilesGroupType(IEnumerable<BookmarkFileType> presentFileTypes)
        {
            if (presentFileTypes.Contains(BookmarkFileType.BookmarkBody) || presentFileTypes.Contains(BookmarkFileType.BookmarkImage))
            {
                return typeof(Bookmark);
            }

            return null;
        }
    }
}