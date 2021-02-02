using System;
using System.Threading;
using System.Threading.Tasks;
using VcogBookmark.Shared;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Services;

namespace VcogBookmark.ClientTools.Services
{
    public class BookmarkStateService
    {
        public TimeSpan UpdateDelay { get; set; }
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly BookmarkVersionService _versionService;
        private readonly IStorageService _storageService;
        private readonly BookmarkNetworkService _networkService;

        public BookmarkStateService(BookmarkVersionService versionService, IStorageService storageService, BookmarkNetworkService networkService)
        {
            _versionService = versionService;
            _storageService = storageService;
            _networkService = networkService;
            UpdateDelay = TimeSpan.FromMinutes(15);
        }

        public async Task<bool> UpdateState()
        {
            var serverBookmarkFolder = await _networkService.GetHierarchy();
            var clientBookmarkFolder = _storageService.GetHierarchy();
            
            var obsoleteHierarchyElements = _versionService.ObsoleteBookmarks(serverBookmarkFolder, clientBookmarkFolder).GetUnwrappedBookmarks();
            var newHierarchyElements = _versionService.NewBookmarks(serverBookmarkFolder, clientBookmarkFolder).GetUnwrappedBookmarks();
            
            foreach (var obsoleteHierarchyElement in obsoleteHierarchyElements)
            {
                _storageService.DeleteBookmark(obsoleteHierarchyElement.BookmarkName);
            }

            /*var bookmarkSavingTasks = newHierarchyElements.Select(newHierarchyElement =>
                _storageService.SaveBookmark(_networkService.GetAllBookmarkFiles(newHierarchyElement.BookmarkName),
                    FileWriteMode.NotStrict));
            var result = await Task.WhenAll(bookmarkSavingTasks);
            return result.All(partialResult => partialResult == true); //*/
            foreach (var newHierarchyElement in newHierarchyElements)
            {
                var bookmarkFiles = _networkService.GetAllBookmarkFiles(newHierarchyElement.BookmarkName);
                var result = await bookmarkFiles.SelectAsync(task => _storageService.SaveFile(task, FileWriteMode.NotStrict)).GatherResults();
                //var result = await _storageService.SaveBookmark(bookmarkFiles, FileWriteMode.NotStrict);
                if (result == false) return false;
            }
            return true;
        }

        public async Task StartObserving(CancellationToken cancellationToken = default)
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                throw new Exception("The method was invoked twice without cancellation!");

            _cancellationTokenSource = new CancellationTokenSource();
            
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(_cancellationTokenSource.Cancel);
            }
            while (true)
            {
                await UpdateState();
                await Task.Delay(UpdateDelay, _cancellationTokenSource.Token);
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        public void StopObserving()
        {
            if (_cancellationTokenSource is null || _cancellationTokenSource.IsCancellationRequested)
                throw new Exception("The method was invoked twice");
            
            _cancellationTokenSource.Cancel();
        }
    }
}