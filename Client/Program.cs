using System;
using System.Linq;
using System.Threading.Tasks;
using VcogBookmark.Shared;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Interfaces;
using VcogBookmark.Shared.Models;

namespace Client
{
    static class Program
    {
        static async Task Main()
        {
            await Task.Delay(1000);

            /*await new LocalStorageFactory(Directory.GetCurrentDirectory() + @"\root", TimeSpan.FromSeconds(0.2))
                .SetupAndRunTests(TimeSpan.FromSeconds(0.6));//*/
            // await new NetworkStorageFactory("https://localhost:5001/").SetupAndRunTests(TimeSpan.FromSeconds(1));
            await new NetworkStorageFactory("https://localhost:5001/", "/subfolder/").SetupAndRunTests(TimeSpan.FromSeconds(0.6));
        }

        static async Task SetupAndRunTests(this IStorageFactory storageFactory, TimeSpan? withDelay = null)
        {
            using var folderChangeWatcher = storageFactory.CreateChangeWatcher();
            using var subscription = folderChangeWatcher.FolderChanged.Subscribe(path =>
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine($"File in folder '{path}' was changed");
                Console.ForegroundColor = ConsoleColor.Black;
            });
            //Task.Run(() => { Thread.Sleep(4000); subscription.Dispose(); Console.WriteLine("subscription cancelled"); });
            await storageFactory.CreateStorageService().RunTestActions(withDelay);
            await Task.Delay(1000);
        }

        static async Task RunTestActions(this IStorageService storageService, TimeSpan? withDelay = null)
        {
            var millisecondsDelay = (int)(withDelay?.TotalMilliseconds ?? 0);
            try
            {
                await storageService.TestStorage_Clear();
                await Task.Delay(millisecondsDelay);
                await storageService.TestStorage_Save("ex", "ex2", FileWriteMode.CreateNew);
                await Task.Delay(millisecondsDelay);
                await storageService.TestStorage_Save("ex", "ex2", FileWriteMode.Override);
                await Task.Delay(millisecondsDelay);
                await storageService.TestStorage_MoveBookmark("ex2", "test/ex");
                await Task.Delay(millisecondsDelay);
                await storageService.TestStorage_MoveFolder("test", "test2");
                await Task.Delay(millisecondsDelay);
                await storageService.TestStorage_DeleteBookmark("test2/ex");
                await Task.Delay(millisecondsDelay);
                await storageService.TestStorage_DeleteDirectory("test2", false);
                await Task.Delay(millisecondsDelay);
                await storageService.TestStorage_Save("ex", "test/ex", FileWriteMode.CreateNew);
                await Task.Delay(millisecondsDelay);
                await storageService.TestStorage_DeleteDirectory("test", true);
                await Task.Delay(millisecondsDelay);
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

            if (hierarchy.Children.OfType<Bookmark>().Count() != 1
                || hierarchy.Children.OfType<Bookmark>().First().Name != "ex"
                || hierarchy.Children.OfType<Folder>().Count() > 1
                || hierarchy.Children.OfType<Folder>().Count() == 1
                && hierarchy.Children.OfType<Folder>().Single().Name != "subfolder")
            {
                throw new Exception("Root folder state doesn't match for these test. Fix it manually!");
            }
            
            Console.WriteLine("-cleared-----------");
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
            
            PrintOutChangedDebugMessage(toBookmarkPath, toBookmarkPath);
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
            
            PrintOutChangedDebugMessage(fromBookmarkPath, toBookmarkPath);
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
            
            Console.WriteLine($"-\"/{fromFolderPath}\" and \"/{toFolderPath}\" were changed");
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
            
            PrintOutChangedDebugMessage(bookmarkToDeletePath, bookmarkToDeletePath);
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
            
            Console.WriteLine($"-\"/{folderToDeletePath}\" was changed");
        }

        static string GetContainingDirectory(string path)
        {
            var index = path.LastIndexOf('/');
            if (index == -1) return string.Empty;
            return path.Substring(0, index);
        }

        static void PrintOutChangedDebugMessage(string path1, string path2)
        {
            path1 = GetContainingDirectory(path1);
            path2 = GetContainingDirectory(path2);

            Console.WriteLine(path1 == path2
                ? $"-\"/{path1}\" was changed"
                : $"-\"/{path1}\" and \"/{path2}\" were changed");
        }
    }
}