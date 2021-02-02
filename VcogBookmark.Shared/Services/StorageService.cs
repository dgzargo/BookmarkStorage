using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;
using static VcogBookmark.Shared.EnumsHelper;

namespace VcogBookmark.Shared.Services
{
    public interface IStorageService
    {
        Task<bool> SaveFile(FileProfile fileProfileTask, FileWriteMode writeMode);
        void DeleteBookmark(string bookmarkPath);
        void DeleteBookmark(IEnumerable<FileProfile> info);
        void DeleteDirectoryWithContentWithin(string directoryPath);
        void DeleteDirectoryWithContentWithin(DirectoryInfo directoryInfo);
        BookmarkFolder GetHierarchy(string root = "");
    }

    public class StorageService : IStorageService
    {
        private readonly string _storageRootDirectory;

        public StorageService(string storageRootDirectory)
        {
            _storageRootDirectory = storageRootDirectory;
        }

        public async Task<bool> SaveFile(FileProfile fileProfile, FileWriteMode writeMode)
        {
            var fileInputStream = fileProfile.Data;
            var pathToSave = fileProfile.GetFullPath(_storageRootDirectory);
            if (writeMode == FileWriteMode.CreateNew && File.Exists(pathToSave))
            {
                return false;
            }
            if (writeMode == FileWriteMode.Override && !File.Exists(pathToSave))
            {
                return false;
            }
            var fileMode = writeMode switch
            {
                FileWriteMode.Override => FileMode.Create,
                FileWriteMode.CreateNew => FileMode.CreateNew,
                FileWriteMode.NotStrict => FileMode.Create,
                _ => throw new ArgumentOutOfRangeException(nameof(writeMode), writeMode, null)
            };
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(pathToSave)!);
                using (var outputStream = new FileStream(pathToSave, fileMode))
                {
                    await fileInputStream.CopyToAsync(outputStream);
                }

                File.SetCreationTimeUtc(pathToSave, fileProfile.LastTimeUtc);
                File.SetLastWriteTimeUtc(pathToSave, fileProfile.LastTimeUtc);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public void DeleteBookmark(string bookmarkPath)
        {
            var fileProfiles = AllEnumValues<BookmarkFileType>()
                .Select(type => new FileProfile(Stream.Null, bookmarkPath, type, DateTime.Now)); // todo: rework for bookmarks of different kind
            DeleteBookmark(fileProfiles);
        }
        
        public void DeleteBookmark(IEnumerable<FileProfile> info)
        {
            foreach (var fileProfile in info)
            {
                var fullPath = fileProfile.GetFullPath(_storageRootDirectory);
                var fileInfo = new FileInfo(fullPath);
                fileInfo.Attributes = FileAttributes.Normal;
                fileInfo.Delete();
                // RecursiveDeleteEmptyDirectories(fileInfo.Directory);
            }
            // EnsureRootDirectoryExists();
        }

        #region delete empty directories recursive descending

        private void RecursiveDeleteEmptyDirectories(DirectoryInfo? directoryInfo)
        {
            if (directoryInfo == null) return;
            if (directoryInfo.EnumerateFileSystemInfos().Any()) return; // check if empty

            directoryInfo.Attributes = FileAttributes.Normal;
            directoryInfo.Delete();
            RecursiveDeleteEmptyDirectories(directoryInfo.Parent);
        }

        private void EnsureRootDirectoryExists()
        {
            Directory.CreateDirectory(_storageRootDirectory);
        }

        #endregion

        public void DeleteDirectoryWithContentWithin(string directoryPath)
        {
            var dir = new DirectoryInfo(Path.Combine(_storageRootDirectory, directoryPath));
            DeleteDirectoryWithContentWithin(dir);
        }

        public void DeleteDirectoryWithContentWithin(DirectoryInfo directoryInfo)
        {
            directoryInfo.Delete(true);
        }
        
        public BookmarkFolder GetHierarchy(string root = "")
        {
            var rootPath = Path.Combine(_storageRootDirectory, root);
            if (!Directory.Exists(rootPath))
            {
                return new BookmarkFolder(null, new HashSet<IBookmarkHierarchyElement>()); // 0
            }

            var directoryNames = Directory.GetDirectories(rootPath);
            var fileNames = Directory.GetFiles(rootPath);
            
            var children = new HashSet<IBookmarkHierarchyElement>(); // directoryNames.Length + fileNames.Length / 2

            foreach (var fileName in fileNames)
            {
                if (fileName.EndsWith(".vbm") && File.Exists($"{fileName.Substring(0, fileName.LastIndexOf('.'))}.jpg"))
                {
                    var lastWriteTime = File.GetLastWriteTimeUtc(fileName);
                    var lastWriteTimeTrimmed = new DateTime(lastWriteTime.Year, lastWriteTime.Month, lastWriteTime.Day, lastWriteTime.Hour, lastWriteTime.Minute, lastWriteTime.Second, lastWriteTime.Kind);
                    var filename = NewStandardExtensions.GetRelativePath(_storageRootDirectory, fileName);
                    var pureFilename = Path.GetFileNameWithoutExtension(filename);
                    var bookmark = new Bookmark(pureFilename, lastWriteTimeTrimmed);
                    children.Add(bookmark);
                }
            }

            foreach (var directoryName in directoryNames)
            {
                var directoryHierarchy = GetHierarchy(NewStandardExtensions.GetRelativePath(_storageRootDirectory, directoryName));
                directoryHierarchy.FolderName = NewStandardExtensions.GetRelativePath(rootPath, directoryName);
                children.Add(directoryHierarchy);
            }

            return new BookmarkFolder(null, children);
        }
    }
}