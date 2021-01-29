using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VcogBookmark.ClientTools.Services;
using VcogBookmark.Shared;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Services;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var bookmarkHierarchyService = new BookmarkHierarchyService();
            var networkService = new BookmarkNetworkService("http://193.84.22.46:8282/VcogBookmarkServer/", bookmarkHierarchyService);// "https://localhost:5001/"
            var root = Directory.GetCurrentDirectory() ?? throw new Exception();
            var storageService = new StorageService(root);
            var versionService = new BookmarkVersionService();
            
            // await storageService.SaveBookmark(networkService.GetAllBookmarkFiles("ex"), FileWriteMode.NotStrict);
            // var result =  await networkService.DeleteBookmarkFromTheServer("abc");
            /*storageService.DeleteBookmark("StartupBookmark");
            storageService.DeleteBookmark("ex");//*/
            await new BookmarkStateService(versionService, storageService, networkService).UpdateState();

            /*using var textFile = File.OpenRead($"{root}/ex.{BookmarkFileType.BookmarkBody.GetExtension()}");
            using var imageFile = File.OpenRead($"{root}/ex.{BookmarkFileType.BookmarkImage.GetExtension()}");
            await networkService.LoadBookmarkToTheServer(
                textFile,
                imageFile,
                "ex2",
                true);//*/
        }
    }
}