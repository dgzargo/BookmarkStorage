using System.Threading.Tasks;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.Shared.Services
{
    public interface IStorageService
    {
        Task<bool> Save(FilesGroup filesGroup, FileWriteMode writeMode);
        // FilesGroup? Find(Folder folderToSearch, string path);
        Task<FilesGroup?> Find(string path);
        // Folder? FindFolder(Folder folderToSearch, string path);
        Task<Folder?> FindFolder(string path);
        Task<bool> DeleteBookmark(FilesGroup filesGroup);
        Task<bool> DeleteDirectory(Folder folder, bool withContentWithin);
        Task<Folder?> GetHierarchy();
        Task<bool> Clear(Folder folder);
        Task<bool> Move(BookmarkHierarchyElement element, string newPath);
        FakeFilesGroup MakeFake(string path);
    }
}