using System;
using System.Collections.Generic;
using System.Linq;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Interfaces;

namespace VcogBookmark.Shared.Models
{
    public abstract class FilesGroup : BookmarkHierarchyElement
    {
        protected FilesGroup(string bookmarkName, DateTime lastTime, IFileDataProviderService providerService) : base(bookmarkName)
        {
            LastTime = lastTime;
            ProviderService = providerService;
        }
        public DateTime LastTime { get; }
        public abstract IEnumerable<BookmarkFileType> FileTypes { get; }
        public IEnumerable<FileProfile> RelatedFiles => FileTypes.Select(type => new FileProfile(LocalPath, type, LastTime, ProviderService));
        private IFileDataProviderService ProviderService { get; }
    }
}