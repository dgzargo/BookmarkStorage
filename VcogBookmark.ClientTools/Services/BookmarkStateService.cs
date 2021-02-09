using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VcogBookmark.Shared;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;
using VcogBookmark.Shared.Services;

namespace VcogBookmark.ClientTools.Services
{
    public class BookmarkStateService
    {
        public TimeSpan UpdateDelay { get; set; }
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly BookmarkVersionService _versionService;
        private readonly IStorageService _targetStorageService;
        private readonly IStorageService _sourceStorageService;

        public BookmarkStateService(BookmarkVersionService versionService, IStorageService targetStorageService, IStorageService sourceStorageService)
        {
            _versionService = versionService;
            _targetStorageService = targetStorageService;
            _sourceStorageService = sourceStorageService;
            UpdateDelay = TimeSpan.FromMinutes(15);
        }

        public async Task<bool> UpdateState()
        {
            var sourceHierarchy = await _sourceStorageService.GetHierarchy();
            var targetHierarchy = await _targetStorageService.GetHierarchy();

            var obsoleteHierarchyElements = _versionService.ObsoleteBookmarks(sourceHierarchy, targetHierarchy).GetUnwrapped().ToArray();
            var newHierarchyElements = _versionService.NewBookmarks(sourceHierarchy, targetHierarchy).GetUnwrapped().ToArray();
            
            foreach (var obsoleteHierarchyElement in obsoleteHierarchyElements.OfType<FilesGroup>())
            {
                await _targetStorageService.DeleteBookmark(obsoleteHierarchyElement);
            }

            foreach (var newHierarchyElement in newHierarchyElements.OfType<FilesGroup>())
            {
                var result = await _targetStorageService.Save(newHierarchyElement, FileWriteMode.CreateNew);
                if (result == false) return false;
            }
            
            /*foreach (var folder in obsoleteHierarchyElements.OfType<Folder>().Where(folder => folder.Children.Count == 0))
            {
                await _targetStorageService.DeleteDirectoryWithContentWithin(folder);
            }// todo: clear all obsolete folders and make new */ 
            
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
                var stateSuccessfullyUpdated = await UpdateState();
                if (stateSuccessfullyUpdated)
                {
                    await Task.Delay(UpdateDelay, _cancellationTokenSource.Token);
                }
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