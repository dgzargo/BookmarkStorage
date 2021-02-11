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
        public IFileDataProviderService? ProviderService { get; set; }
        public IFileDataProviderService ProviderServiceInherited
        {
            get
            {
                if (ProviderService != null) return ProviderService;
                if (Parent == null) throw new Exception($"inherited {nameof(IFileDataProviderService)} doesn't exist");
                return Parent.ProviderServiceInherited;
            }
        }
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
    }

    public abstract class FilesGroup : BookmarkHierarchyElement
    {
        protected FilesGroup(string bookmarkName, DateTime lastTime) : base(bookmarkName)
        {
            LastTime = lastTime;
        }
        public DateTime LastTime { get; }
        public abstract IEnumerable<BookmarkFileType> FileTypes { get; }
        public IEnumerable<FileProfile> RelatedFiles => FileTypes.Select(type => new FileProfile(LocalPath, type, LastTime, ProviderServiceInherited));
    }

    public class Bookmark : FilesGroup
    {
        public Bookmark(string bookmarkName, DateTime lastTime) : base(bookmarkName, lastTime)
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

    public class EmptyFilesGroup : FilesGroup
    {
        public EmptyFilesGroup(FakeFilesGroup fake, FilesGroup filesGroup) : base(fake.Name, filesGroup.LastTime)
        {
            Parent = fake.Parent;
            FileTypes = filesGroup.FileTypes;
            ProviderService = new FromFileGroupProviderService(filesGroup);
        }
        
        public EmptyFilesGroup(FakeFilesGroup fake, Dictionary<BookmarkFileType, Stream> dataDictionary, DateTime lastTime) : base(fake.Name, lastTime)
        {
            Parent = fake.Parent;
            FileTypes = dataDictionary.Keys;
            ProviderService = new FromStreamProviderService(dataDictionary);
        }

        public override IEnumerable<BookmarkFileType> FileTypes { get; }
    }
}