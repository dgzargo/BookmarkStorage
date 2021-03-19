using System.Threading.Tasks;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.Shared.Services
{
    public interface IStorageService
    {
        Task<bool> Save(FilesGroup filesGroup, FileWriteMode writeMode);
        Task<Folder?> GetHierarchy();
        Task<FilesGroup?> Find(string path);
        Task<Folder?> FindFolder(string path);
        Task<bool> DeleteBookmark(FilesGroup filesGroup);
        Task<bool> DeleteDirectory(Folder folder, bool withContentWithin);
        Task<bool> Clear(Folder folder);
        Task<bool> Move(BookmarkHierarchyElement element, string newPath);
        FakeFilesGroup MakeFake(string path);
    }
}