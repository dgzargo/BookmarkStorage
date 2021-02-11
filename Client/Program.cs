using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VcogBookmark.ClientTools.Services;
using VcogBookmark.Shared;
using VcogBookmark.Shared.Enums;
using VcogBookmark.Shared.Models;
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
            await Task.Delay(2000);
            var bookmarkHierarchyService = new BookmarkHierarchyUtils();
            var networkService = new BookmarkNetworkService(ServerAddressToUse, bookmarkHierarchyService);
            var root = Directory.GetCurrentDirectory() ?? throw new Exception();
            var storageService = new StorageService(root + @"\root");
            var versionService = new BookmarkVersionService();

            // await new BookmarkStateService(versionService, storageService, networkService).UpdateState();
            
            var hierarchy = await storageService.GetHierarchy();
            var result = await storageService.Clear(hierarchy);//*/
            
            /*var fake = storageService.MakeFake(@"/ex2");
            var originBookmark = await storageService.Find(@"/ex") ?? throw new InvalidOperationException();
            var result = await storageService.Save(new EmptyFilesGroup(fake, originBookmark), FileWriteMode.CreateNew);//*/
            
            /*var originBookmark = await storageService.Find(@"/ex2") ?? throw new InvalidOperationException();
            var result = await storageService.Move(originBookmark, @"/ex3");//*/
            
            /*var originBookmark = await storageService.FindFolder(@"/test") ?? throw new InvalidOperationException();
            var result = await storageService.Move(originBookmark, @"/123");//*/

            /*var originBookmark = await storageService.Find(@"/ex3") ?? throw new InvalidOperationException();
            var result = await storageService.DeleteBookmark(originBookmark);//*/
            
            /*var originFolder = await storageService.FindFolder(@"/test - Copy") ?? throw new InvalidOperationException();
            var result = await storageService.DeleteDirectory(originFolder, true);//*/

            if (result == false)
            {
                throw new Exception("something went wrong...");
            }
        }
    }
}