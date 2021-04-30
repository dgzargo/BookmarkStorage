namespace VcogBookmark.Shared.Models
{
    public class FakeFilesGroup : BookmarkHierarchyElement
    {
        public FakeFilesGroup(string bookmarkName) : base(bookmarkName)
        {
        }

        public override bool IsFolder => false;
    }
}