using System;
using System.Collections.Generic;
using System.Linq;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.ClientTools.Services
{
    public class BookmarkVersionService
    {
        public BookmarkFolder ObsoleteBookmarks(BookmarkFolder serverBookmarkFolder, BookmarkFolder clientBookmarkFolder)
        {
            return ExceptBookmarkHierarchy(serverBookmarkFolder, clientBookmarkFolder);
        }
        
        public BookmarkFolder NewBookmarks(BookmarkFolder serverBookmarkFolder, BookmarkFolder clientBookmarkFolder)
        {
            return ExceptBookmarkHierarchy(clientBookmarkFolder, serverBookmarkFolder);
        }

        protected BookmarkFolder ExceptBookmarkHierarchy(BookmarkFolder minuendBookmarkFolder,
            BookmarkFolder subtrahendBookmarkFolder)
        {
            if (minuendBookmarkFolder.FolderName != subtrahendBookmarkFolder.FolderName) throw new ArgumentException("folder names are different!");

            var exceptedHierarchyElements = subtrahendBookmarkFolder.Children.OfType<Bookmark>()
                    .Except(minuendBookmarkFolder.Children.OfType<Bookmark>(), Bookmark.BookmarkNameLastTimeComparer);
            var exceptedHierarchyHashSet = new HashSet<IBookmarkHierarchyElement>(exceptedHierarchyElements);

            var minuendFolders = minuendBookmarkFolder.Children.OfType<BookmarkFolder>().ToArray();
            foreach (var subtrahendFolder in subtrahendBookmarkFolder.Children.OfType<BookmarkFolder>())
            {
                var correspondingMinuendFolder = minuendFolders.FirstOrDefault(element => element.FolderName == subtrahendFolder.FolderName);
                if (correspondingMinuendFolder is null) continue;
                var exceptedHierarchy = ExceptBookmarkHierarchy(correspondingMinuendFolder, subtrahendFolder);
                exceptedHierarchyHashSet.Add(exceptedHierarchy);
            }

            return new BookmarkFolder(minuendBookmarkFolder.FolderName, exceptedHierarchyHashSet);
        }
    }
}