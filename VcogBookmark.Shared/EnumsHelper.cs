using System;
using VcogBookmark.Shared.Enums;

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

        public static TEnum[] AllEnumValues<TEnum>() where TEnum: Enum
        {
            return (TEnum[]) Enum.GetValues(typeof(TEnum));
        }
    }
}