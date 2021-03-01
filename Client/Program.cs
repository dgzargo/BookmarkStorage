using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VcogBookmark.ClientTools.Services;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;
using VcogBookmark.Shared.Services;

namespace Client
{
    static class Program
    {
        private const bool UseLocalServer = true;
        private const string VisportServerAddress = "http://lv.visco.no:8282/VcogBookmarkServer/";
        private const string LocalServerAddress = "https://localhost:5001/";
        private const string ServerAddressToUse = UseLocalServer ? LocalServerAddress : VisportServerAddress;
        static async Task Main(string[] args)
        {
            await Task.Delay(2000);
            
            await CreateStorageService<StorageService>().RunTestActions();
            await CreateStorageService<BookmarkNetworkService>().RunTestActions();
            await CreateStorageService<BookmarkNetworkService>().TestStorage_MoveBookmark("ex", "subroot/ex"); // prepare root for the next test
            await CreateStorageService<BookmarkNetworkService>("/subroot").RunTestActions();
            await CreateStorageService<BookmarkNetworkService>().TestStorage_MoveBookmark("subroot/ex", "ex"); // unprepare
        }

        static IStorageService CreateStorageService<TStorageService>(string? subRoot = null) where TStorageService : IStorageService
        {
            if (typeof(TStorageService) == typeof(BookmarkNetworkService))
            {
                return subRoot == null ? new BookmarkNetworkService(ServerAddressToUse) : new BookmarkNetworkService(ServerAddressToUse, subRoot);
            }
            if (typeof(TStorageService) == typeof(StorageService))
            {
                var root = Directory.GetCurrentDirectory() ?? throw new Exception();
                return subRoot == null ? new StorageService(root + @"\root") : new StorageService(root + @"\root" + subRoot);
            }
            throw new NotSupportedException("This StorageService type has no support!");
        }

        static async Task RunTestActions(this IStorageService storageService)
        {
            try
            {
                await storageService.TestStorage_Clear();
                await storageService.TestStorage_Save("ex", "ex2", FileWriteMode.CreateNew);
                await storageService.TestStorage_Save("ex", "ex2", FileWriteMode.Override);
                await storageService.TestStorage_MoveBookmark("ex2", "test/ex");
                await storageService.TestStorage_MoveFolder("test", "test2");
                await storageService.TestStorage_DeleteBookmark("test2/ex");
                await storageService.TestStorage_DeleteDirectory("test2", false);
                await storageService.TestStorage_Save("ex", "test/ex", FileWriteMode.CreateNew);
                await storageService.TestStorage_DeleteDirectory("test", true);
            }
            catch (Exception e)
            {
                throw new Exception($"{storageService.GetType().Name} failed!", e);
            }
        }

        static async Task TestStorage_Clear(this IStorageService storageService)
        {
            var hierarchy = await storageService.GetHierarchy();
            if (hierarchy == null)
            {
                throw new Exception("root folder does not exist!");
            }

            if (await storageService.Clear(hierarchy) == false)
            {
                throw new Exception($"{nameof(storageService.Clear)} was not successful!");
            }

            if (hierarchy.Children.Count != 1 || hierarchy.Children.First().GetType() != typeof(Bookmark) || hierarchy.Children.First().Name != "ex")
            {
                throw new Exception("Root folder state doesn't match for these test. Fix it manually!");
            }
        }

        static async Task TestStorage_Save(this IStorageService storageService, string fromBookmarkPath, string toBookmarkPath, FileWriteMode writeMode)
        {
            var fake = storageService.MakeFake(toBookmarkPath);
            var originBookmark = await storageService.Find(fromBookmarkPath);
            if (originBookmark == null)
            {
                throw new Exception($"Bookmark \"{fromBookmarkPath}\" does not exist!");
            }

            if (await storageService.Save(new ProxyFilesGroup(fake, originBookmark), writeMode) == false)
            {
                throw new Exception($"{nameof(storageService.Save)} bookmark to \"{toBookmarkPath}\" was not successful!");
            }
        }

        static async Task TestStorage_MoveBookmark(this IStorageService storageService, string fromBookmarkPath, string toBookmarkPath)
        {
            var originBookmark = await storageService.Find(fromBookmarkPath);
            if (originBookmark == null)
            {
                throw new Exception($"Bookmark \"{fromBookmarkPath}\" does not exist!");
            }

            if (await storageService.Move(originBookmark, toBookmarkPath) == false)
            {
                throw new Exception($"{nameof(storageService.Move)} bookmark to \"{toBookmarkPath}\" was not successful!");
            }
        }

        static async Task TestStorage_MoveFolder(this IStorageService storageService, string fromFolderPath, string toFolderPath)
        {
            var originBookmark = await storageService.FindFolder(fromFolderPath);
            if (originBookmark == null)
            {
                throw new Exception($"Folder \"{fromFolderPath}\" does not exist!");
            }

            if (await storageService.Move(originBookmark, toFolderPath) == false)
            {
                throw new Exception($"{nameof(storageService.Move)} folder to \"{toFolderPath}\" was not successful!");
            }
        }

        static async Task TestStorage_DeleteBookmark(this IStorageService storageService, string bookmarkToDeletePath)
        {
            var originBookmark = await storageService.Find(bookmarkToDeletePath);
            if (originBookmark == null)
            {
                throw new Exception($"Bookmark \"{bookmarkToDeletePath}\" does not exist!");
            }

            if (await storageService.DeleteBookmark(originBookmark) == false)
            {
                throw new Exception($"{nameof(storageService.DeleteBookmark)} \"{bookmarkToDeletePath}\" was not successful!");
            }
        }

        static async Task TestStorage_DeleteDirectory(this IStorageService storageService, string folderToDeletePath, bool withContentWithin)
        {
            var originFolder = await storageService.FindFolder(folderToDeletePath);
            if (originFolder == null)
            {
                throw new Exception($"Bookmark \"{folderToDeletePath}\" does not exist!");
            }

            if (await storageService.DeleteDirectory(originFolder, withContentWithin) == false)
            {
                throw new Exception($"{nameof(storageService.DeleteDirectory)} \"{folderToDeletePath}\" was not successful!");
            }
        }
    }
}