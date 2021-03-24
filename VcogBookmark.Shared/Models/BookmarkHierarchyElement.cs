namespace VcogBookmark.Shared.Models
{
    public abstract class BookmarkHierarchyElement
    {
        protected BookmarkHierarchyElement(string name)
        {
            Name = name;
        }

        public Folder? Parent { get; set; }
        public string Name { get; set; }
        public string LocalPath => Parent is null ? Name : $"{Parent.LocalPath}/{Name}";
    }
}