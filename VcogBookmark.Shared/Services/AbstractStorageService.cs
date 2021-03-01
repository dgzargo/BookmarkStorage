using System;
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

        protected FilesGroup? Find(Folder folderToSearch, string path)
        {
            var pathFragments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Where(fragment => !string.IsNullOrWhiteSpace(fragment)).ToArray();
            if (pathFragments.Length == 0) return null;
            var folder = FindFolder(pathFragments.Take(pathFragments.Length - 1), folderToSearch);
            return folder?.Children.OfType<FilesGroup>().FirstOrDefault(fg => fg.Name == pathFragments.Last());
        }

        public async Task<FilesGroup?> Find(string path)
        {
            var folder = await GetHierarchy();
            return folder != null ? Find(folder, path) : null;
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

        
        protected Folder? FindFolder(Folder folderToSearch, string path)
        {
            var pathFragments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Where(fragment => !string.IsNullOrWhiteSpace(fragment)).ToArray();
            return FindFolder(pathFragments, folderToSearch);
        }

        public async Task<Folder?> FindFolder(string path)
        {
            var folder = await GetHierarchy();
            return folder != null ? FindFolder(folder, path) : null;
        }

        public abstract Task<bool> DeleteBookmark(FilesGroup filesGroup);
        public abstract Task<bool> DeleteDirectory(Folder folder, bool withContentWithin);
        public abstract Task<Folder?> GetHierarchy();
        public abstract Task<bool> Clear(Folder folder);
        public abstract Task<bool> Move(BookmarkHierarchyElement element, string newPath);
        public FakeFilesGroup MakeFake(string path)
        {
            var pathFragments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Where(fragment => !string.IsNullOrWhiteSpace(fragment)).ToArray();
            var parentHierarchy = MakeFakeFolder(pathFragments.Take(pathFragments.Length - 1));
            return new FakeFilesGroup(pathFragments.Last()) {Parent = parentHierarchy};
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private Folder MakeFakeFolder(IEnumerable<string> pathFragments)
        {
            if (!pathFragments.Any()) return new Folder(string.Empty);
            var fragmentsLeft = pathFragments.Take(pathFragments.Count() - 1);
            var supFolder = MakeFakeFolder(fragmentsLeft);
            var folder = new Folder(pathFragments.Last()) {Parent = supFolder};
            return folder;
        }
    }
}