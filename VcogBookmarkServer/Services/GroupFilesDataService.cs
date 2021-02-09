using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;
using VcogBookmark.Shared.Services;

namespace VcogBookmarkServer.Services
{
    public class GroupFilesDataService: IFileDataProviderService
    {
        private readonly Dictionary<BookmarkFileType, Stream> _data;

        public GroupFilesDataService()
        {
            _data = new Dictionary<BookmarkFileType, Stream>();
        }

        public void Add(BookmarkFileType fileType, Stream data)
        {
            _data[fileType] = data;
        }

        public Task<Stream> GetData(FileProfile fileProfile)
        {
            return Task.FromResult(_data[fileProfile.FileType]);
        }
    }
}