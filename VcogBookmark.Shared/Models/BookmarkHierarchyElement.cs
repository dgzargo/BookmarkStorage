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
        public abstract bool IsFolder { get; }
        public virtual string LocalPath => Parent is null ? IsFolder ? Name + '/' : Name : IsFolder ? $"{Parent.LocalPath}{Name}/" : $"{Parent.LocalPath}{Name}";
    }
}