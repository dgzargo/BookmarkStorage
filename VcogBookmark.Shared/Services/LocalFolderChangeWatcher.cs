using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VcogBookmark.Shared.Interfaces;

namespace VcogBookmark.Shared.Services
{
    public class LocalFolderChangeWatcher : IFolderChangeWatcher
    {
        public LocalFolderChangeWatcher(string fileSystemPath, TimeSpan groupEventInterval) // TimeSpan should smooth frequent file system event and double ping time
        {
            fileSystemPath = Path.GetFullPath(fileSystemPath) + '\\';
            
            _folderChangedRepeatedEvents = new Subject<string>();
            _fileSystemWatcher = new FileSystemWatcher(fileSystemPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.Size | NotifyFilters.FileName, // LastWrite filter could cause a bug by pushing unexpected directory path instead of filepath.
            };
            _fileSystemWatcher.Error += ErrorEventHandler;
            _fileSystemWatcher.Changed += FileChangedHandler;
            _fileSystemWatcher.Created += FileChangedHandler;
            _fileSystemWatcher.Deleted += FileChangedHandler;
            _fileSystemWatcher.Renamed += FileChangedHandler;

            _directorySystemWatcher = new FileSystemWatcher(fileSystemPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.DirectoryName, // LastAccess or LastWrite filter could cause a bug by pushing unexpected filepath instead of directory path. Fixed by adding 'guid.touch' file in Move<Folder> method.
            };
            _directorySystemWatcher.Error += ErrorEventHandler;
            _directorySystemWatcher.Changed += FolderChangedHandler;
            _directorySystemWatcher.Created += FolderChangedHandler;
            _directorySystemWatcher.Deleted += FolderChangedHandler;
            _directorySystemWatcher.Renamed += FolderChangedHandler;
            
            _fileSystemWatcher.EnableRaisingEvents = true;
            _directorySystemWatcher.EnableRaisingEvents = true;

            var closing = _folderChangedRepeatedEvents.Delay(groupEventInterval);

            FolderChanged = _folderChangedRepeatedEvents.Window(() => closing)
                .SelectMany(window => window.ToList())
                .Where(list => list.Count > 0)
                .SelectMany(list => list.Distinct())
                .Select(GetFromRootPath);

            // https://stackoverflow.com/a/340454/11621099
            string GetFromRootPath(string path)
            {
                Uri fromUri = new Uri(fileSystemPath);
                Uri toUri = new Uri(path);

                Uri relativeUri = fromUri.MakeRelativeUri(toUri);
                return '/' + Uri.UnescapeDataString(relativeUri.ToString());
            }
        }
        
        private readonly FileSystemWatcher _fileSystemWatcher;
        private readonly FileSystemWatcher _directorySystemWatcher;
        private readonly Subject<string> _folderChangedRepeatedEvents;

        public IObservable<string> FolderChanged { get; }
        public void Dispose()
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            _directorySystemWatcher.EnableRaisingEvents = false;
            _folderChangedRepeatedEvents.OnCompleted();
            
            _fileSystemWatcher.Error -= ErrorEventHandler;
            _fileSystemWatcher.Changed -= FileChangedHandler;
            _fileSystemWatcher.Created -= FileChangedHandler;
            _fileSystemWatcher.Deleted -= FileChangedHandler;
            _fileSystemWatcher.Renamed -= FileChangedHandler;
            _fileSystemWatcher.Dispose();
            _directorySystemWatcher.Error -= ErrorEventHandler;
            _directorySystemWatcher.Changed -= FolderChangedHandler;
            _directorySystemWatcher.Created -= FolderChangedHandler;
            _directorySystemWatcher.Deleted -= FolderChangedHandler;
            _directorySystemWatcher.Renamed -= FolderChangedHandler;
            _directorySystemWatcher.Dispose();
            _folderChangedRepeatedEvents.Dispose();
        }

        private void ErrorEventHandler(object sender, ErrorEventArgs e)
        {
            _folderChangedRepeatedEvents.OnError(e.GetException());
        }
        private void FileChangedHandler(object sender, FileSystemEventArgs e)
        {
            _folderChangedRepeatedEvents.OnNext(Path.GetDirectoryName(e.FullPath) + '\\');
        }
        private void FolderChangedHandler(object sender, FileSystemEventArgs e)
        {
            _folderChangedRepeatedEvents.OnNext(e.FullPath + '\\');
        }
    }
}