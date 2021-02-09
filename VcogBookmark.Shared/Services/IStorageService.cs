using System.Threading.Tasks;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.Shared.Services
{
    public interface IStorageService
    {
        Task<bool> Save(FilesGroup filesGroup, FileWriteMode writeMode);
        FilesGroup? Find(Folder folderToSearch, string path);
        Task<FilesGroup?> Find(string path);
        Folder? FindFolder(Folder folderToSearch, string path);
        Task<Folder?> FindFolder(string path);
        Task<bool> DeleteBookmark(FilesGroup filesGroup);
        Task<bool> DeleteDirectoryWithContentWithin(Folder folder);
        Task<Folder> GetHierarchy();
        Task<bool> Clear(Folder folder);
    }
}