using System;
using System.Collections.Generic;
using System.Linq;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.ClientTools.Services
{
    public class BookmarkVersionService
    {
        public static readonly BookmarkVersionService Instance = new BookmarkVersionService(); // singleton
        private static readonly IEqualityComparer<BookmarkHierarchyElement> Comparer = new BookmarkHierarchyElementEqualityComparer();
        public Folder ObsoleteBookmarks(Folder? serverFolder, Folder? clientFolder)
        {
            return ExceptBookmarkHierarchy(clientFolder, serverFolder);
        }
        
        public Folder NewBookmarks(Folder? serverFolder, Folder? clientFolder)
        {
            return ExceptBookmarkHierarchy(serverFolder, clientFolder);
        }

        private Folder ExceptBookmarkHierarchy(Folder? minuendFolder, Folder? subtrahendFolder)
        {
            #region check input conditions

            if (minuendFolder == null)
            {
                return new Folder(string.Empty);
            }

            if (subtrahendFolder == null)
            {
                return minuendFolder;
            }
            
            if (minuendFolder.Name != subtrahendFolder.Name) throw new ArgumentException("folder names are different!");

            #endregion

            var folder = new Folder(minuendFolder.Name);
            
            var exceptedHierarchyElements = minuendFolder.Children.Except(subtrahendFolder.Children, Comparer);
            foreach (var exceptedHierarchyElement in exceptedHierarchyElements)
            {
                folder.Children.Add(exceptedHierarchyElement);
            }

            var untouchedFolders = IntersectionOfFolders(minuendFolder, subtrahendFolder);
            foreach (var untouchedFolderName in untouchedFolders)
            {
                var minuendSubFolder = minuendFolder.Children.OfType<Folder>().First(subFolder => subFolder.Name == untouchedFolderName);
                var subtrahendSubFolder = subtrahendFolder.Children.OfType<Folder>().First(subFolder => subFolder.Name == untouchedFolderName);
                folder.Children.Add(ExceptBookmarkHierarchy(minuendSubFolder, subtrahendSubFolder));
            }
            /*var minuendFolders = minuendFolder.Children.OfType<Folder>().ToArray();
            foreach (var subtrahendSubFolder in subtrahendFolder.Children.OfType<Folder>())
            {
                var correspondingMinuendFolder = minuendFolders.FirstOrDefault(element => element.Name == subtrahendSubFolder.Name);
                if (correspondingMinuendFolder is null) continue;
                var exceptedHierarchy = ExceptBookmarkHierarchy(correspondingMinuendFolder, subtrahendSubFolder);
                folder.Children.Add(exceptedHierarchy);
            }//*/

            return folder;

            IEnumerable<string> IntersectionOfFolders(Folder x, Folder y)
            {
                var xFolders = x.Children.OfType<Folder>();
                var yFolders = y.Children.OfType<Folder>();
                return xFolders.Intersect(yFolders, Comparer).Select(element => element.Name);
            }
        }
        
        private sealed class BookmarkHierarchyElementEqualityComparer : IEqualityComparer<BookmarkHierarchyElement>
        {
            public bool Equals(BookmarkHierarchyElement? x, BookmarkHierarchyElement? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                if (x.Name != y.Name) return false;
                if (x is FilesGroup filesGroupX && y is FilesGroup filesGroupY)
                {
                    return Equals(filesGroupX.LastTime, filesGroupY.LastTime);
                }
                if (x is Folder folderX && y is Folder folderY)
                {
                    // return (folderX.Children.Count > 0) == (folderY.Children.Count > 0);
                    return true;
                }
                throw new NotImplementedException(); // if inheritance structure was modified
            }

            public int GetHashCode(BookmarkHierarchyElement obj)
            {
                if (obj is FilesGroup filesGroup)
                {
                    unchecked
                    {
                        return (filesGroup.Name.GetHashCode() * 397) ^ filesGroup.LastTime.GetHashCode();
                    }
                }
                if (obj is Folder)
                {
                    return obj.Name.GetHashCode();
                }
                throw new NotImplementedException(); // if inheritance structure was modified
            }
        }
    }
}