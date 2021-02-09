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
        private const bool UseLocalServer = true;
        private const string VisportServerAddress = "http://lv.visco.no:8282/VcogBookmarkServer/";
        private const string LocalServerAddress = "https://localhost:5001/";
        private const string ServerAddressToUse = UseLocalServer ? LocalServerAddress : VisportServerAddress;
        static async Task Main(string[] args)
        {
            var bookmarkHierarchyService = new BookmarkHierarchyUtils();
            var networkService = new BookmarkNetworkService(ServerAddressToUse, bookmarkHierarchyService);
            var root = Directory.GetCurrentDirectory() ?? throw new Exception();
            var storageService = new StorageService(root + @"\root");
            var versionService = new BookmarkVersionService();

            // var f = storageService.Find(@"/root/test/Test2 - Copy");
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