using System.Collections.ObjectModel;

namespace PdfiumLight
{
    public class PdfBookmark
    {
        public PdfBookmark()
        {
            Children = new PdfBookmarkCollection();
        }

        public string Title { get; set; }

        public int PageIndex { get; set; }

        public PdfBookmarkCollection Children { get; set; }

        public override string ToString() => Title;
    }

    public class PdfBookmarkCollection : Collection<PdfBookmark>
    {
    }
}