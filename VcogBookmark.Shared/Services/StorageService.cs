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
    public class StorageService
    {
        private readonly string _storageRootDirectory;

        public StorageService(string storageRootDirectory)
        {
            _storageRootDirectory = storageRootDirectory;
        }

        public async Task<bool> SaveBookmark(IEnumerable<Task<FileProfile>> bookmarkFiles, FileWriteMode writeMode)
        {
            var tasks = bookmarkFiles.Select(task => SaveFile(task, writeMode));
            
            try
            {
                var results = await Task.WhenAll(tasks);
                return results.All(result => result == true);
            }
            catch
            {
                return false;
            }
        }
        
        private async Task<bool> SaveFile(Task<FileProfile> fileProfileTask, FileWriteMode writeMode)
        {
            var fileProfile = await fileProfileTask;
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
            Directory.CreateDirectory(Path.GetDirectoryName(pathToSave)!);
            Directory.CreateDirectory(Path.GetDirectoryName(pathToSave)!);
            using var outputStream = new FileStream(pathToSave, fileMode);
            await fileInputStream.CopyToAsync(outputStream);
            File.SetCreationTimeUtc(fileProfile.LocalPath, fileProfile.LastTimeUtc);
            File.SetLastWriteTimeUtc(fileProfile.LocalPath, fileProfile.LastTimeUtc);
            return true;
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
                File.Delete(fileProfile.GetFullPath(_storageRootDirectory));
            }
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