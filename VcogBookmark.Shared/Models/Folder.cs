using System.Collections.Generic;
using System.Linq;

namespace VcogBookmark.Shared.Models
{
    public class Folder: BookmarkHierarchyElement
    {
        public Folder(string? folderName) : base(folderName ?? string.Empty)
        {
            Children = new List<BookmarkHierarchyElement>();
        }
        
        public ICollection<BookmarkHierarchyElement> Children { get; }

        public IEnumerable<BookmarkHierarchyElement> GetUnwrapped()
        {
            return Children.Concat(Children.OfType<Folder>().SelectMany(folder => folder.GetUnwrapped()));
        }

        public override bool IsFolder => true;
    }
}