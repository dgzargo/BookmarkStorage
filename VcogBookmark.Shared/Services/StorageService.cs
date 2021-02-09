using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.Shared.Services
{
    public class StorageService : AbstractStorageService, IFileDataProviderService
    {
        private readonly string _storageRootDirectory;

        public StorageService(string storageRootDirectory)
        {
            _storageRootDirectory = storageRootDirectory;
        }

        
        public override Task<bool> Save(FilesGroup filesGroup, FileWriteMode writeMode)
        {
            return filesGroup.RelatedFiles
                .Select(profile => SaveFile(profile, writeMode))
                .GatherResults();
        }
        private async Task<bool> SaveFile(FileProfile fileProfile, FileWriteMode writeMode)
        {
            var fileInputStream = await fileProfile.GetData();
            var pathToSave = GetFullPath(fileProfile);
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

        public override Task<bool> DeleteBookmark(FilesGroup filesGroup)
        {
            foreach (var fileProfile in filesGroup.RelatedFiles)
            {
                var fullPath = GetFullPath(fileProfile);
                var fileInfo = new FileInfo(fullPath);
                fileInfo.Attributes = FileAttributes.Normal;
                fileInfo.Delete();
                // RecursiveDeleteEmptyDirectories(fileInfo.Directory);
            }
            // EnsureRootDirectoryExists();
            return Task.FromResult(true);
        }
        
        private void RecursiveDeleteEmptyDirectories(Folder? folder)
        {
            if (folder == null) return;
            var directoryInfo = new DirectoryInfo(GetFullPath(folder));
            if (directoryInfo.EnumerateFileSystemInfos().Any()) return; // check if empty
            directoryInfo.Attributes = FileAttributes.Normal;
            directoryInfo.Delete();
            RecursiveDeleteEmptyDirectories(folder.Parent);
        }

        public override Task<bool> DeleteDirectoryWithContentWithin(Folder folder)
        {
            Directory.Delete(GetFullPath(folder), true);
            return Task.FromResult(true);
        }

        public override Task<Folder> GetHierarchy()
        {
            return Directory.Exists(_storageRootDirectory)
                ? Task.FromResult(GetHierarchy(_storageRootDirectory))
                : throw new Exception("root directory does not exist!");
        }

        private Folder GetHierarchy(string fullPath) // the folder must exist
        {
            var directoryNames = Directory.GetDirectories(fullPath);
            var fileNames = Directory.GetFiles(fullPath);

            var folder = new Folder(null) {ProviderService = this};

            foreach (var filePath in fileNames)
            {
                if (filePath.EndsWith(".vbm") && File.Exists($"{filePath.Substring(0, filePath.LastIndexOf('.'))}.jpg"))
                {
                    var lastWriteTime = File.GetLastWriteTimeUtc(filePath);
                    var lastWriteTimeTrimmed = new DateTime(lastWriteTime.Year, lastWriteTime.Month, lastWriteTime.Day, lastWriteTime.Hour, lastWriteTime.Minute, lastWriteTime.Second, lastWriteTime.Kind);
                    var pureFilename = Path.GetFileNameWithoutExtension(filePath);
                    var bookmark = new Bookmark(pureFilename, lastWriteTimeTrimmed)
                    {
                        Parent = folder,
                        ProviderService = this,
                    };
                    folder.Children.Add(bookmark);
                }
            }

            foreach (var subDirectoryPath in directoryNames)
            {
                var subDir = GetHierarchy(subDirectoryPath);
                subDir.Name = Path.GetFileName(subDirectoryPath) ?? string.Empty;
                subDir.Parent = folder;
                folder.Children.Add(subDir);
            }

            return folder;
        }

        public override async Task<bool> Clear(Folder folder)
        {
            var thisFolder = await FindFolder(folder.LocalPath);
            if (thisFolder == null) return true; // it doesn't exist so it's already clean
            
            var registeredFiles = thisFolder.Children
                .OfType<FilesGroup>()
                .SelectMany(group => group.RelatedFiles)
                .Select(GetFullPath);
            var presentFiles = Directory.EnumerateFiles(GetFullPath(folder));
            var exceptedFiles = presentFiles.Except(registeredFiles);
            
            foreach (var exceptedFile in exceptedFiles)
            {
                var fileInfo = new FileInfo(exceptedFile);
                fileInfo.Attributes = FileAttributes.Normal;
                fileInfo.Delete();
            }

            return await folder.Children.OfType<Folder>().Select(Clear).GatherResults();
        }

        public Task<Stream> GetData(FileProfile fileProfile)
        {
            var stream = new FileStream(GetFullPath(fileProfile), FileMode.Open, FileAccess.Read);
            return Task.FromResult<Stream>(stream);
        }

        private string GetFullPath(FileProfile fileProfile)
        {
            return GetFullPath($"{fileProfile.LocalPath}.{fileProfile.FileType.GetExtension()}");
        }
        private string GetFullPath(BookmarkHierarchyElement hierarchyElement)
        {
            return GetFullPath(hierarchyElement.LocalPath);
        }
        private string GetFullPath(string partialPath)
        {
            partialPath = partialPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return Path.Combine(_storageRootDirectory, partialPath);
        }
        private string GetFullPath(string root, string partialPath)
        {
            var localPath = Path.Combine(root, partialPath);
            return GetFullPath(localPath);
        }
    }
}