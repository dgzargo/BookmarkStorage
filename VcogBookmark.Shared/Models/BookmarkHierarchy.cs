using System;
using System.Collections.Generic;

namespace VcogBookmark.Shared.Models
{
    public interface IBookmarkHierarchyElement
    {
    }

    public class BookmarkFolder: IBookmarkHierarchyElement
    {
        public BookmarkFolder(string? folderName, HashSet<IBookmarkHierarchyElement> children)
        {
            FolderName = folderName;
            Children = children;
        }

        public string? FolderName { get; set; }
        public HashSet<IBookmarkHierarchyElement> Children { get; }

        public IEnumerable<Bookmark> GetUnwrappedBookmarks()
        {
            foreach (var (nameList, time) in GetUnwrappedBookmarksWithoutFolderName())
            {
                yield return new Bookmark(string.Join("/", nameList), time);
            }
        }
        
        private IEnumerable<(List<string>, DateTime? LastTime)> GetUnwrappedBookmarksWithoutFolderName()
        {
            foreach (var child in Children)
            {
                if (child is Bookmark bookmark)
                {
                    yield return (new List<string>(5) {bookmark.BookmarkName}, bookmark.LastTime);
                    continue;
                }

                if (child is BookmarkFolder folder)
                {
                    foreach (var valueTuple in folder.GetUnwrappedBookmarksWithoutFolderName())
                    {
                        valueTuple.Item1.Add(folder.FolderName!);
                        yield return valueTuple;
                    }
                    continue;
                }

                throw new NotImplementedException("some type was added");
            }
        }
    }

    public class Bookmark: IBookmarkHierarchyElement
    {
        private sealed class BookmarkNameLastTimeEqualityComparer : IEqualityComparer<Bookmark>
        {
            public bool Equals(Bookmark? x, Bookmark? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.BookmarkName == y.BookmarkName && Nullable.Equals(x.LastTime, y.LastTime);
            }

            public int GetHashCode(Bookmark obj)
            {
                unchecked
                {
                    return (obj.BookmarkName.GetHashCode() * 397) ^ obj.LastTime.GetHashCode();
                }
            }
        }

        public static IEqualityComparer<Bookmark> BookmarkNameLastTimeComparer { get; } = new BookmarkNameLastTimeEqualityComparer();

        public Bookmark(string bookmarkName, DateTime? lastTime)
        {
            BookmarkName = bookmarkName;
            LastTime = lastTime;
        }

        public string BookmarkName { get; }
        public DateTime? LastTime { get; }
    }
}