using System;
using System.Collections.Generic;
using System.IO;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Services;

namespace VcogBookmark.Shared.Models
{
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