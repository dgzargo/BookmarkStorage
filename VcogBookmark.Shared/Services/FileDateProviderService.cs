using System.IO;
using System.Threading.Tasks;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.Shared.Services
{
    public interface IFileDataProviderService
    {
        Task<Stream> GetData(FileProfile fileProfile);
    }
}