using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Services;

namespace VcogBookmark.Shared.Models
{
    public abstract class BookmarkHierarchyElement
    {
        protected BookmarkHierarchyElement(string name)
        {
            Name = name;
        }

        public Folder? Parent { get; set; }
        public string Name { get; set; }
        public string LocalPath => Parent is null ? Name : $"{Parent.LocalPath}/{Name}";
    }

    public class Folder: BookmarkHierarchyElement
    {
        public Folder(string? folderName) : base(folderName ?? string.Empty)
        {
            Children = new List<BookmarkHierarchyElement>();
        }
        
        public ICollection<BookmarkHierarchyElement> Children { get; }

        public IEnumerable<BookmarkHierarchyElement> GetUnwrapped()
        {
            return Children.Concat(Children.OfType<Folder>().SelectMany(folder => folder.GetUnwrapped()));
        }

        public DateTime? LastUpdateTime
        {
            get
            {
                var dates = GetUnwrapped().OfType<FilesGroup>().Select(filesGroup => filesGroup.LastTime).ToArray();
                return dates.Any() ? (DateTime?) dates.Max() : null;
            }
        }
    }

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
        public IFileDataProviderService ProviderService { get; }
    }

    public class Bookmark : FilesGroup
    {
        public Bookmark(string bookmarkName, DateTime lastTime, IFileDataProviderService providerService) : base(bookmarkName, lastTime, providerService)
        {
            FileTypes = new[] {BookmarkFileType.BookmarkBody, BookmarkFileType.BookmarkImage};
        }

        public override IEnumerable<BookmarkFileType> FileTypes { get; }
    }

    public class FakeFilesGroup : BookmarkHierarchyElement
    {
        public FakeFilesGroup(string bookmarkName) : base(bookmarkName)
        {
        }
    }

    public class ProxyFilesGroup : FilesGroup
    {
        public ProxyFilesGroup(FakeFilesGroup fake, FilesGroup filesGroup) : base(fake.Name, filesGroup.LastTime, new FromFileGroupProviderService(filesGroup))
        {
            Parent = fake.Parent;
            FileTypes = filesGroup.FileTypes;
        }
        
        public ProxyFilesGroup(FakeFilesGroup fake, Dictionary<BookmarkFileType, Stream> dataDictionary, DateTime lastTime) : base(fake.Name, lastTime, new FromStreamProviderService(dataDictionary))
        {
            Parent = fake.Parent;
            FileTypes = dataDictionary.Keys;
        }

        public override IEnumerable<BookmarkFileType> FileTypes { get; }
    }
}