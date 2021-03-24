using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VcogBookmark.Shared.Interfaces;
using VcogBookmark.Shared.Models;

namespace VcogBookmark.Shared.Services
{
    public class BookmarkHierarchyUtils
    {
        public static readonly BookmarkHierarchyUtils Instance = new BookmarkHierarchyUtils();
        public string ToJson(BookmarkHierarchyElement hierarchy, bool serializeAsRoot)
        {
            if (hierarchy is FilesGroup bookmarkHierarchyBookmark)
            {
                return $"\"{bookmarkHierarchyBookmark.Name}@{bookmarkHierarchyBookmark.LastTime:o}\"";
            }
            if (hierarchy is Folder bookmarkHierarchyFolder)
            {
                var listOfSerializedChildren = string.Join(",", bookmarkHierarchyFolder.Children.Select(folder => ToJson(folder, false)));
                return serializeAsRoot
                    ? $"[{listOfSerializedChildren}]"
                    : $"{{\"{bookmarkHierarchyFolder.Name}\":[{listOfSerializedChildren}]}}";
            }
            throw new ArgumentOutOfRangeException(nameof(hierarchy));
        }

        [Obsolete("just for human eyes")]
        public string ToAlignedJson(BookmarkHierarchyElement hierarchy)
        {
            if (hierarchy is FilesGroup)
            {
                return ToJson(hierarchy, true); // no aligning for one bookmark
            }
            if (hierarchy is Folder)
            {
                var jsonString = ToJson(hierarchy, true);
                return JArray.Parse(jsonString).ToString(Formatting.Indented);
            }
            throw new ArgumentOutOfRangeException(nameof(hierarchy));
        }

        public Folder Parse(string jsonString, IFileDataProviderService providerService)
        {
            var jsonArray = JArray.Parse(jsonString);
            return Parse(jsonArray, providerService);
        }

        private Folder Parse(JArray jsonArray, IFileDataProviderService providerService)
        {
            var folder = new Folder(null);
            
            foreach (var jToken in jsonArray)
            {
                if (jToken.Type == JTokenType.String)
                {
                    var bookmarkData = jToken.ToString().Split('@');
                    if (bookmarkData.Length != 2) throw new FormatException("wrong bookmark format!");
                    var bookmark = new Bookmark(bookmarkData[0], DateTime.Parse(bookmarkData[1]).ToUniversalTime(), providerService);
                    bookmark.Parent = folder;
                    folder.Children.Add(bookmark);
                }
                if (jToken.Type == JTokenType.Object)
                {
                    var jObject = (JObject) jToken;
                    var firstProperty = jObject.Properties().First();
                    var folderName = firstProperty.Name;
                    var innerJsonArray = (JArray) firstProperty.Value;
                    var innerBookmarkFolder = Parse(innerJsonArray, providerService);
                    innerBookmarkFolder.Name = folderName;
                    innerBookmarkFolder.Parent = folder;
                    folder.Children.Add(innerBookmarkFolder);
                }
            }

            return folder;
        }
    }
}