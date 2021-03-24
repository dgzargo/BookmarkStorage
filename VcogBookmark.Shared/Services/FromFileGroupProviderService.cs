using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VcogBookmark.Shared.Interfaces;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.Shared.Services
{
    public class FromFileGroupProviderService : IFileDataProviderService
    {
        private readonly FilesGroup _filesGroup;
        public FromFileGroupProviderService(FilesGroup filesGroup)
        {
            _filesGroup = filesGroup;
        }
        public Task<Stream> GetData(FileProfile fileProfile)
        {
            return _filesGroup.RelatedFiles.First(profile => profile.FileType == fileProfile.FileType).GetData();
        }
    }
}