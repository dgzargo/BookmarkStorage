using System;
using System.IO;
using VcogBookmark.Shared.Enums;

namespace VcogBookmark.Shared.Models
{
    public class FileProfile
    {
        public FileProfile(Stream data, string localPath, BookmarkFileType fileType, DateTime lastTimeUtc)
        {
            Data = data;
            LocalPath = localPath;
            FileType = fileType;
            LastTimeUtc = lastTimeUtc;
        }

        public Stream Data { get; }
        public string LocalPath { get; }
        public BookmarkFileType FileType { get; }
        public DateTime LastTimeUtc { get; } // todo

        public string GetFullPath(string root)
        {
            return Path.Combine(root, $"{LocalPath}.{FileType.GetExtension()}");
        }
    }
}