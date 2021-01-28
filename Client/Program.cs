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
            // await storageService.SaveBookmark(networkService.GetAllBookmarkFiles("ex"), FileWriteMode.NotStrict); // there is a bug: need to change update time of file
            /*await networkService.LoadBookmarkToTheServer(
                File.OpenRead($"{root}/ex.{BookmarkFileType.BookmarkBody.GetExtension()}"),
                File.OpenRead($"{root}/ex.{BookmarkFileType.BookmarkBody.GetExtension()}"),
                "ex2",
                true);//*/
            // var result =  await networkService.DeleteBookmarkFromTheServer("abc");
            var versionService = new BookmarkVersionService();
            await new BookmarkStateService(versionService, storageService, networkService).UpdateState();
        }
    }
}