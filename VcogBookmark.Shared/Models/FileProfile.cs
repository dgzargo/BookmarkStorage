using System;
using System.IO;
using System.Threading.Tasks;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Services;

namespace VcogBookmark.Shared.Models
{
    public class FileProfile
    {
        public FileProfile(string localPath, BookmarkFileType fileType, DateTime lastTimeUtc, IFileDataProviderService providerService)
        {
            LocalPath = localPath;
            FileType = fileType;
            LastTimeUtc = lastTimeUtc;
            ProviderService = providerService;
        }
        public string LocalPath { get; }
        public BookmarkFileType FileType { get; }
        public DateTime LastTimeUtc { get; }
        private IFileDataProviderService ProviderService { get; }

        public Task<Stream> GetData()
        {
            return ProviderService.GetData(this);
        }
    }
}