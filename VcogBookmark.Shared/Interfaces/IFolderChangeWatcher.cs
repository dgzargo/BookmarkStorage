using System;

namespace VcogBookmark.Shared.Interfaces
{
    public interface IFolderChangeWatcher : IDisposable
    {
        public IObservable<string> FolderChanged { get; }
    }
}