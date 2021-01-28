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

        public static TEnum[] AllEnumValues<TEnum>() where TEnum: Enum
        {
            return (TEnum[]) Enum.GetValues(typeof(TEnum));
        }
    }
}