using System;
using VcogBookmark.Shared.Interfaces;
using VcogBookmark.Shared.Models;
using VcogBookmark.Shared.Services;

namespace VcogBookmark.Shared
{
    public interface IStorageFactory
    {
        IStorageService CreateStorageService();
        IFolderChangeWatcher CreateChangeWatcher();
    }

    public sealed class LocalStorageFactory : IStorageFactory
    {
        private readonly string _fileSystemPath;
        private readonly TimeSpan _groupEventInterval;

        public LocalStorageFactory(string fileSystemPath, TimeSpan groupEventInterval)
        {
            _fileSystemPath = fileSystemPath;
            _groupEventInterval = groupEventInterval;
        }
        
        public IStorageService CreateStorageService()
        {
            return new StorageService(_fileSystemPath);
        }

        public IFolderChangeWatcher CreateChangeWatcher()
        {
            return new LocalFolderChangeWatcher(_fileSystemPath, _groupEventInterval);
        }
    }

    public sealed class NetworkStorageFactory : IStorageFactory
    {
        private readonly string _serverUrl;
        private readonly string? _relativePath;
        private readonly AccountTokenController _accountTokenController;

        public NetworkStorageFactory(string serverUrl, Person person, string? relativePath = null)
        {
            _serverUrl = serverUrl;
            _relativePath = relativePath;
            _accountTokenController = new AccountTokenController(serverUrl, person.Login, person.Password);
        }
        
        public IStorageService CreateStorageService()
        {
            return new BookmarkNetworkService(_serverUrl, _accountTokenController, _relativePath);
        }

        public IFolderChangeWatcher CreateChangeWatcher()
        {
            return new NetworkFolderChangeWatcher(_serverUrl, _accountTokenController, _relativePath);
        }
    }
}