using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.Shared.Services
{
    public class BookmarkHierarchyService
    {
        public string ToJson(IBookmarkHierarchyElement hierarchy)
        {
            if (hierarchy is Bookmark bookmarkHierarchyBookmark)
            {
                return $"\"{bookmarkHierarchyBookmark.BookmarkName}@{bookmarkHierarchyBookmark.LastTime:o}\"";
                //return $"\"{bookmarkHierarchyBookmark.BookmarkName}\"";
            }
            if (hierarchy is BookmarkFolder bookmarkHierarchyFolder)
            {
                var listOfSerializedChildren = string.Join(",", bookmarkHierarchyFolder.Children.Select(ToJson));
                return bookmarkHierarchyFolder.FolderName == null
                    ? $"[{listOfSerializedChildren}]"
                    : $"{{\"{bookmarkHierarchyFolder.FolderName}\":[{listOfSerializedChildren}]}}";
            }
            throw new ArgumentOutOfRangeException(nameof(hierarchy));
        }

        public BookmarkFolder Parse(string jsonString)
        {
            var jsonArray = JArray.Parse(jsonString);
            return Parse(jsonArray);
        }

        private BookmarkFolder Parse(JArray jsonArray)
        {
            var children = new HashSet<IBookmarkHierarchyElement>(); // jsonArray.Count
            
            foreach (var jToken in jsonArray)
            {
                if (jToken.Type == JTokenType.String)
                {
                    var bookmarkData = jToken.ToString().Split('@');
                    if (bookmarkData.Length != 2) throw new FormatException("wrong bookmark format!");
                    var bookmark = new Bookmark(bookmarkData[0], DateTime.Parse(bookmarkData[1]).ToUniversalTime());
                    children.Add(bookmark);
                }
                if (jToken.Type == JTokenType.Object)
                {
                    var jObject = (JObject) jToken;
                    var firstProperty = jObject.Properties().First();
                    var folderName = firstProperty.Name;
                    var innerJsonArray = (JArray) firstProperty.Value;
                    var innerBookmarkFolder = Parse(innerJsonArray);
                    innerBookmarkFolder.FolderName = folderName;
                    children.Add(innerBookmarkFolder);
                }
            }

            return new BookmarkFolder(null, children);
        }
    }
}