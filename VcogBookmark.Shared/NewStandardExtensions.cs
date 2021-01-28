using System;
using System.IO;

namespace VcogBookmark.Shared
{
    public static class NewStandardExtensions
    {
        // https://stackoverflow.com/questions/703281/getting-path-relative-to-the-current-working-directory
        public static string GetRelativePath(string folder, string filespec)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }

            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString()
                .Replace('/', Path.DirectorySeparatorChar));
        }
    }
}