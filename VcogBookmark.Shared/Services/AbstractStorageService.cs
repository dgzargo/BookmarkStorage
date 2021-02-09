using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.Shared.Services
{
    public abstract class AbstractStorageService : IStorageService
    {
        public abstract Task<bool> Save(FilesGroup filesGroup, FileWriteMode writeMode);

        public FilesGroup? Find(Folder folderToSearch, string path)
        {
            var pathFragments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Where(fragment => !string.IsNullOrWhiteSpace(fragment)).ToArray();
            if (pathFragments.Length == 0) return null;
            var folder = FindFolder(pathFragments.Take(pathFragments.Length - 1), folderToSearch);
            return folder?.Children.OfType<FilesGroup>().FirstOrDefault(fg => fg.Name == pathFragments.Last());
        }

        public async Task<FilesGroup?> Find(string path)
        {
            return Find(await GetHierarchy(), path);
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private Folder? FindFolder(IEnumerable<string> path, Folder hierarchy)
        {
            if (!path.Any()) return hierarchy;
            var fragmentsLeft = path.Skip(1);
            var subfolder = hierarchy.Children
                .OfType<Folder>()
                .FirstOrDefault(f => f.Name == path.First());
            if (subfolder == null) return null;
            return FindFolder(fragmentsLeft, subfolder);
        }

        
        public Folder? FindFolder(Folder folderToSearch, string path)
        {
            var pathFragments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Where(fragment => !string.IsNullOrWhiteSpace(fragment)).ToArray();
            return FindFolder(pathFragments, folderToSearch);
        }

        public async Task<Folder?> FindFolder(string path)
        {
            return FindFolder(await GetHierarchy(), path);
        }

        public abstract Task<bool> DeleteBookmark(FilesGroup filesGroup);
        public abstract Task<bool> DeleteDirectoryWithContentWithin(Folder folder);
        public abstract Task<Folder> GetHierarchy();
        public abstract Task<bool> Clear(Folder folder);
    }
}