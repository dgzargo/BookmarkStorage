using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Interfaces;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.Shared.Services
{
    public class FromStreamProviderService: IFileDataProviderService
    {
        private readonly Dictionary<BookmarkFileType, Stream> _dataDictionary;

        public FromStreamProviderService()
        {
            _dataDictionary = new Dictionary<BookmarkFileType, Stream>();
        }

        public FromStreamProviderService(Dictionary<BookmarkFileType, Stream> dataDictionaryDictionary)
        {
            _dataDictionary = dataDictionaryDictionary;
        }

        public Task<Stream> GetData(FileProfile fileProfile)
        {
            return Task.FromResult(_dataDictionary[fileProfile.FileType]);
        }
    }
}