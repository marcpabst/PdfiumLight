using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfiumLight
{
    /// <summary>
    /// Represents a PDF document.
    /// </summary>
    public class PdfDocument : IDisposable
    {
        private static readonly Encoding FPDFEncoding = new UnicodeEncoding(false, false, false);

        private IntPtr _document;
        private IntPtr _form;
        private bool _disposed;
        private NativeMethods.FPDF_FORMFILLINFO _formCallbacks;
        private GCHandle _formCallbacksHandle;
        private int _id;
        private Stream _stream;

        /// <summary>
        /// Initializes a new instance of PdfDocument
        /// </summary>
        /// <param name="path">The path to the PDF-file</param>
        /// <param name="password">Password to decrypt PDF</param>
        public PdfDocument(string path, string password = null)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));

            LoadFile(File.OpenRead(path), password);
        }

        /// <summary>
        /// Initializes a new instance of PdfDocument a .NET Stream. Allows opening huge PDFs without loading them into memory first.
        /// </summary>
        /// <param name="stream">The input Stream. Don't dispose prior to closing the PDF.</param>
        /// <param name="password">Password to decrypt PDF</param>
        public PdfDocument(Stream stream, string password = null)
        {
            LoadFile(stream, password);
        }

        /// <summary>
        /// The Bookmarks of this PDF document. 
        /// </summary>
        public PdfBookmarkCollection Bookmarks { get; private set; }

        private void LoadFile(Stream stream, string password)
        {
            PdfLibrary.EnsureLoaded();

            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _id = StreamManager.Register(stream);

            var document = NativeMethods.FPDF_LoadCustomDocument(stream, password, _id);

            if (document == IntPtr.Zero)
            {
                throw new PdfException((PdfError)NativeMethods.FPDF_GetLastError());
            }

            LoadDocument(document);
        }

        /// <summary>
        /// This method returns the dimension of the pages in this document without loading them into memory first
        /// </summary>
        /// <returns>The List of page dimensions</returns>
        public SizeF[] GetPageSize()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PdfDocument));
            }

            int pageCount = NativeMethods.FPDF_GetPageCount(_document);

            var result = new SizeF[pageCount];

            for (int i = 0; i < pageCount; i++)
            {
                result[i] = GetPageSize(i);
            }

            return result;
        }

        /// <summary>
        /// This method returns the dimension of a page in this document without loading it into memory first
        /// </summary>
        /// <param name="pageNumber">The page number of the page you want to retrice the dimensions for</param>
        /// <returns>Dimesions as SizeF</returns>
        public SizeF GetPageSize(int pageNumber)
        {
            NativeMethods.FPDF_GetPageSizeByIndex(_document, pageNumber, out double width, out double height);

            return new SizeF((float)width, (float)height);
        }

        public void Save(Stream stream)
        {
            NativeMethods.FPDF_SaveAsCopy(_document, stream, NativeMethods.FPDF_SAVE_FLAGS.FPDF_NO_INCREMENTAL);
        }

        protected void LoadDocument(IntPtr document)
        {
            _document = document;

            NativeMethods.FPDF_GetDocPermissions(_document);

            _formCallbacks = new NativeMethods.FPDF_FORMFILLINFO();
            _formCallbacksHandle = GCHandle.Alloc(_formCallbacks, GCHandleType.Pinned);

            // Depending on whether XFA support is built into the PDFium library, the version
            // needs to be 1 or 2. We don't really care, so we just try one or the other.

            for (int i = 1; i <= 2; i++)
            {
                _formCallbacks.version = i;

                _form = NativeMethods.FPDFDOC_InitFormFillEnvironment(_document, _formCallbacks);
                if (_form != IntPtr.Zero)
                {
                    break;
                }
            }

            NativeMethods.FPDF_SetFormFieldHighlightColor(_form, 0, 0xFFE4DD);
            NativeMethods.FPDF_SetFormFieldHighlightAlpha(_form, 100);

            NativeMethods.FORM_DoDocumentJSAction(_form);
            NativeMethods.FORM_DoDocumentOpenAction(_form);


            Bookmarks = LoadBookmarks(NativeMethods.FPDF_BookmarkGetFirstChild(document, IntPtr.Zero));
        }

        private PdfBookmarkCollection LoadBookmarks(IntPtr bookmark)
        {
            var result = new PdfBookmarkCollection();
            if (bookmark == IntPtr.Zero)
                return result;

            result.Add(LoadBookmark(bookmark));

            while ((bookmark = NativeMethods.FPDF_BookmarkGetNextSibling(_document, bookmark)) != IntPtr.Zero)
            {
                result.Add(LoadBookmark(bookmark));
            }

            return result;
        }

        private PdfBookmark LoadBookmark(IntPtr bookmark)
        {
            var pdfbookmark = new PdfBookmark
            {
                Title = GetBookmarkTitle(bookmark),
                PageIndex = (int)GetBookmarkPageIndex(bookmark)
            };

            // Action = NativeMethods.FPDF_BookmarkGetAction(_bookmark);
            // if (Action != IntPtr.Zero)
            //    ActionType = NativeMethods.FPDF_ActionGetType(Action);

            var child = NativeMethods.FPDF_BookmarkGetFirstChild(_document, bookmark);

            if (child != IntPtr.Zero)
            {
                pdfbookmark.Children = LoadBookmarks(child);
            }

            return pdfbookmark;
        }

        private string GetBookmarkTitle(IntPtr bookmark)
        {
            uint length = NativeMethods.FPDF_BookmarkGetTitle(bookmark, null, 0);
            byte[] buffer = new byte[length];
            NativeMethods.FPDF_BookmarkGetTitle(bookmark, buffer, length);

            string result = Encoding.Unicode.GetString(buffer);

            if (result.Length > 0 && result[result.Length - 1] == 0)
            {
                result = result.Substring(0, result.Length - 1);
            }

            return result;
        }

        private uint GetBookmarkPageIndex(IntPtr bookmark)
        {
            IntPtr dest = NativeMethods.FPDF_BookmarkGetDest(_document, bookmark);

            return (dest != IntPtr.Zero)
                ? NativeMethods.FPDFDest_GetPageIndex(_document, dest)
                : 0u;
        }

        /// <summary>
        /// This method will load and return the sepecific page
        /// </summary>
        /// <param name="page">The null-based index of the page</param>
        /// <returns>A new PdfPage</returns>
        public PdfPage GetPage(int page) => new PdfPage(_document, _form, page);

        /// <summary>
        /// Returns the page count of the document. This will call a native Pdfium function.
        /// </summary>
        /// <returns></returns>
        public int PageCount() => NativeMethods.FPDF_GetPageCount(_document);

        /// <summary>
        /// Dtailed meta informations from the PDF document
        /// </summary>
        /// <returns>A PdfInformation containing the metadata</returns>
        public PdfInformation GetInformation()
        {
            return new PdfInformation
            {
                Creator = GetMetaText("Creator"),
                Title = GetMetaText("Title"),
                Author = GetMetaText("Author"),
                Subject = GetMetaText("Subject"),
                Keywords = GetMetaText("Keywords"),
                Producer = GetMetaText("Producer"),
                CreationDate = GetMetaTextAsDate("CreationDate"),
                ModificationDate = GetMetaTextAsDate("ModDate")
            };
        }

        private string GetMetaText(string tag)
        {
            // Length includes a trailing \0.

            uint length = NativeMethods.FPDF_GetMetaText(_document, tag, null, 0);
            if (length <= 2)
                return string.Empty;

            byte[] buffer = new byte[length];
            NativeMethods.FPDF_GetMetaText(_document, tag, buffer, length);

            return Encoding.Unicode.GetString(buffer, 0, (int)(length - 2));
        }

        public DateTime? GetMetaTextAsDate(string tag)
        {
            string dt = GetMetaText(tag);

            if (string.IsNullOrEmpty(dt))
                return null;

            Regex dtRegex =
                new Regex(
                    @"(?:D:)(?<year>\d\d\d\d)(?<month>\d\d)(?<day>\d\d)(?<hour>\d\d)(?<minute>\d\d)(?<second>\d\d)(?<tz_offset>[+-zZ])?(?<tz_hour>\d\d)?'?(?<tz_minute>\d\d)?'?");

            Match match = dtRegex.Match(dt);

            if (match.Success)
            {
                var year = match.Groups["year"].Value;
                var month = match.Groups["month"].Value;
                var day = match.Groups["day"].Value;
                var hour = match.Groups["hour"].Value;
                var minute = match.Groups["minute"].Value;
                var second = match.Groups["second"].Value;
                var tzOffset = match.Groups["tz_offset"]?.Value;
                var tzHour = match.Groups["tz_hour"]?.Value;
                var tzMinute = match.Groups["tz_minute"]?.Value;

                string formattedDate = $"{year}-{month}-{day}T{hour}:{minute}:{second}.0000000";

                if (tzOffset != null && tzOffset.Length == 1)
                {
                    switch (tzOffset[0])
                    {
                        case 'Z':
                        case 'z':
                            formattedDate += "+0";
                            break;
                        case '+':
                        case '-':
                            formattedDate += $"{tzOffset}{tzHour}:{tzMinute}";
                            break;
                    }
                }

                try
                {
                    return DateTime.Parse(formattedDate);
                }
                catch (FormatException)
                {
                    return null;
                }
            }

            return null;
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private NativeMethods.FPDF FlagsToFPDFFlags(PdfRenderFlags flags)
        {
            return (NativeMethods.FPDF)(flags & ~(PdfRenderFlags.Transparent | PdfRenderFlags.CorrectFromDpi));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                StreamManager.Unregister(_id);

                if (_form != IntPtr.Zero)
                {
                    NativeMethods.FORM_DoDocumentAAction(_form, NativeMethods.FPDFDOC_AACTION.WC);
                    NativeMethods.FPDFDOC_ExitFormFillEnvironment(_form);
                    _form = IntPtr.Zero;
                }

                if (_document != IntPtr.Zero)
                {
                    NativeMethods.FPDF_CloseDocument(_document);
                    _document = IntPtr.Zero;
                }

                if (_formCallbacksHandle.IsAllocated)
                    _formCallbacksHandle.Free();

                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }

                _disposed = true;
            }
        }
    }
}