using System.IO;
using System.Threading.Tasks;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.Shared.Interfaces
{
    public interface IFileDataProviderService
    {
        Task<Stream> GetData(FileProfile fileProfile);
    }
}