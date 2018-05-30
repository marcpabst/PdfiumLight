using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace PdfiumLight
{
    /// <summary>
    /// Represents a page of a PDF document.
    /// </summary>
    public class PdfPage : IDisposable
    {

        private static readonly Encoding FPDFEncoding = new UnicodeEncoding(false, false, false);

        private readonly IntPtr _form;
        private bool _disposed;

        /// <summary>
        /// Handle to the page.
        /// </summary>
        public IntPtr Page { get; private set; }

        /// <summary>
        /// Handle to the text page
        /// </summary>
        public IntPtr TextPage { get; private set; }

        /// <summary>
        /// Width of the page in pt
        /// </summary>
        public double Width { get; private set; }

        /// <summary>
        /// Height of th page in pt
        /// </summary>
        public double Height { get; private set; }

        /// <summary>
        /// The index og this page in the document
        /// </summary>
        public int PageNumber { get; private set; }

        /// <summary>
        /// Initializes a new instance of PdfPage
        /// </summary>
        /// <param name="document">The PDF document</param>
        /// <param name="form"></param>
        /// <param name="pageNumber">Number of this page in the document</param>
        public PdfPage(IntPtr document, IntPtr form, int pageNumber)
        {
            _form = form;

            PageNumber = pageNumber;

            Page = NativeMethods.FPDF_LoadPage(document, pageNumber);
            TextPage = NativeMethods.FPDFText_LoadPage(Page);
            NativeMethods.FORM_OnAfterLoadPage(Page, form);
            NativeMethods.FORM_DoPageAAction(Page, form, NativeMethods.FPDFPAGE_AACTION.OPEN);

            Width = NativeMethods.FPDF_GetPageWidth(Page);
            Height = NativeMethods.FPDF_GetPageHeight(Page);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                NativeMethods.FORM_DoPageAAction(Page, _form, NativeMethods.FPDFPAGE_AACTION.CLOSE);
                NativeMethods.FORM_OnBeforeClosePage(Page, _form);
                NativeMethods.FPDFText_ClosePage(TextPage);
                NativeMethods.FPDF_ClosePage(Page);

                _disposed = true;
            }


        }

        private RectangleF GetBounds(int index)
        {
            NativeMethods.FPDFText_GetCharBox(
                Page,
                index,
                out var left,
                out var right,
                out var bottom,
                out var top
            );

            return new RectangleF(
                (float)left,
                (float)top,
                (float)(right - left),
                (float)(bottom - top)
            );
        }


        private bool RenderPDFPageToDC(IntPtr dc, int dpiX, int dpiY, int boundsOriginX, int boundsOriginY, int boundsWidth, int boundsHeight, NativeMethods.FPDF flags)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);


            NativeMethods.FPDF_RenderPage(dc, Page, boundsOriginX, boundsOriginY, boundsWidth, boundsHeight, 0, flags);


            return true;
        }

        /// <summary>
        /// Renders the page.
        /// </summary>
        /// <param name="width">Render width in px</param>
        /// <param name="height">Render height in px</param>
        /// <param name="dpiX"></param>
        /// <param name="dpiY"></param>
        /// <param name="rotate">Specify the rotation of the rendered page</param>
        /// <param name="flags">Render flags</param>
        /// <returns>The rendered page as an Image</returns>
        public Image Render(int width, int height, float dpiX, float dpiY, PdfRotation rotate, PdfRenderFlags flags)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if ((flags & PdfRenderFlags.CorrectFromDpi) != 0)
            {
                width = width * (int)dpiX / 72;
                height = height * (int)dpiY / 72;
            }

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            bitmap.SetResolution(dpiX, dpiY);

            var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

            try
            {
                var handle = NativeMethods.FPDFBitmap_CreateEx(width, height, 4, data.Scan0, width * 4);


                try
                {
                    uint background = (flags & PdfRenderFlags.Transparent) == 0 ? 0xFFFFFFFF : 0x00FFFFFF;

                    NativeMethods.FPDFBitmap_FillRect(handle, 0, 0, width, height, background);

                    bool success = RenderPDFPageToBitmap(
                        handle,
                        (int)dpiX, (int)dpiY,
                        0, 0, width, height,
                        (int)rotate,
                        FlagsToFPDFFlags(flags),
                        (flags & PdfRenderFlags.Annotations) != 0
                    );

                    if (!success)
                        throw new Exception();
                }
                finally
                {
                    NativeMethods.FPDFBitmap_Destroy(handle);
                }

            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }

  
        private bool RenderPDFPageToBitmap(IntPtr bitmapHandle, int dpiX, int dpiY, int boundsOriginX, int boundsOriginY, int boundsWidth, int boundsHeight, int rotate, NativeMethods.FPDF flags, bool renderFormFill)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);


            if (renderFormFill)
                flags &= ~NativeMethods.FPDF.ANNOT;

            NativeMethods.FPDF_RenderPageBitmap(bitmapHandle, Page, boundsOriginX, boundsOriginY, boundsWidth, boundsHeight, rotate, flags);

            if (renderFormFill)
                NativeMethods.FPDF_FFLDraw(_form, bitmapHandle, Page, boundsOriginX, boundsOriginY, boundsWidth, boundsHeight, rotate, flags);


            return true;
        }


        /// <summary>
        /// Gets the index of the character at the provided position
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="tol">The tolerance</param>
        /// <returns>The zero-based index of the character at, or nearby the point (x,y). If there is no character at or nearby the point, return value will be -1. If an error occurs, -3 will be returned.</returns>
        public int GetCharIndexAtPos(double x, double y, double tol = 12)
        {

            return NativeMethods.FPDFText_GetCharIndexAtPos(TextPage, x, y, tol, tol);

        }

        /// <summary>
        /// Transforms a Point in device coordinates to PDF coordinates. 
        /// </summary>
        /// <param name="point">The point in device coordinates</param>
        /// <param name="renderWidth">Render with of the page</param>
        /// <param name="renderHeight">Render height of the page</param>
        /// <returns>The transformed Point</returns>
        public PointF PointToPdf(Point point, int renderWidth, int renderHeight)
        {

            NativeMethods.FPDF_DeviceToPage(
                Page,
                0,
                0,
                renderWidth,
                renderHeight,
                0,
                point.X,
                point.Y,
                out var deviceX,
                out var deviceY
            );

            return new PointF((float)deviceX, (float)deviceY);

        }

        /// <summary>
        /// Transforms a Rectangle in device coordinates to PDF coordinates. 
        /// Will also make sure to return a Rectangle with positive height and width.
        /// </summary>
        /// <param name="rect">The Rectangle in device coordinates</param>
        /// <param name="renderWidth">Render with of the page</param>
        /// <param name="renderHeight">Render height of the page</param>
        /// <returns>The transformed Rectangle</returns>
        public RectangleF RectangleToPdf(Rectangle rect, int renderWidth, int renderHeight)
        {

            NativeMethods.FPDF_DeviceToPage(
                Page,
                0,
                0,
                renderWidth,
                renderHeight,
                0,
                rect.Left,
                rect.Top,
                out var deviceX1,
                out var deviceY1
            );

            NativeMethods.FPDF_DeviceToPage(
                Page,
                0,
                0,
                renderWidth,
                renderHeight,
                0,
                rect.Right,
                rect.Bottom,
                out var deviceX2,
                out var deviceY2
            );

            return new RectangleF(
                (float)deviceX1,
                (float)deviceY1,
                (float)Math.Abs((deviceX2 - deviceX1)),
                (float)Math.Abs((deviceY2 - deviceY1))
            );

        }

        /// <summary>
        /// Gets the text fromf page
        /// </summary>
        /// <returns>The text/returns>
        public string GetPdfText()
        {
            int length = NativeMethods.FPDFText_CountChars(TextPage);
            return GetPdfText(0, length);

        }

        /// <summary>
        /// Gets the text from page specified by offset and length
        /// </summary>
        /// <returns>The text/returns>
        public string GetPdfText(int offset, int length)
        {
            var result = new byte[(length + 1) * 2];
            NativeMethods.FPDFText_GetText(TextPage, offset, length, result);
            return FPDFEncoding.GetString(result, 0, length * 2);
        }


        /// <summary>
        /// Rotates the page.
        /// </summary>
        /// <param name="rotation">Specify the rotation.</param>
        public void RotatePage(PdfRotation rotation)
        {
                NativeMethods.FPDFPage_SetRotation(Page, rotation);
        }

        /// <summary>
        /// Get the bounds of the text (specified by index and length) from the page. 
        /// </summary>
        /// <param name="startIndex">The start index of the text</param>
        /// <param name="length">The length of the text</param>
        /// <returns>List of the bounds for the text</returns>
        public IList<PdfRectangle> GetTextBounds(int startIndex, int length)
        {
            var result = new List<PdfRectangle>();

            int countRects = NativeMethods.FPDFText_CountRects(TextPage, startIndex, length);

            for (int i = 0; i < countRects; i++)
            {
                NativeMethods.FPDFText_GetRect(TextPage, i, out double left, out double top, out double right, out double bottom);

                RectangleF bounds = new RectangleF(
                    (float)left,
                    (float)top,
                    (float)(right - left),
                    (float)(bottom - top));

                if (bounds.Width == 0 || bounds.Height == 0)
                    continue;

                result.Add(new PdfRectangle(PageNumber, bounds));


            }

            return result;
        }

        /// <summary>
        /// Transforms a Rectangle in PDF coordinates to device coordinates. 
        /// Will also make sure to return a Rectangle with positive height and width.
        /// </summary>
        /// <param name="rect">The rect in PDF coordinates</param>
        /// <param name="renderWidth">Render with of the page</param>
        /// <param name="renderHeight">Render height of the page</param>
        /// <returns>The transformed Rectangle</returns>
        public Rectangle RectangleFromPdf(RectangleF rect, int renderWidth, int renderHeight)
        {
            NativeMethods.FPDF_PageToDevice(
                Page,
                0,
                0,
                renderWidth,
                renderHeight,
                0,
                rect.Left,
                rect.Top,
                out var deviceX1,
                out var deviceY1
            );

            NativeMethods.FPDF_PageToDevice(
                Page,
                0,
                0,
                renderWidth,
                renderHeight,
                0,
                rect.Right,
                rect.Bottom,
                out var deviceX2,
                out var deviceY2
            );

            return new Rectangle(
                Math.Min(deviceX1, deviceX2),
                Math.Min(deviceY1, deviceY2),
                Math.Abs(deviceX2 - deviceX1),
                Math.Abs(deviceY2 - deviceY1)
            );

        }

        private NativeMethods.FPDF FlagsToFPDFFlags(PdfRenderFlags flags)
        {
            return (NativeMethods.FPDF)(flags & ~(PdfRenderFlags.Transparent | PdfRenderFlags.CorrectFromDpi));
        }
    }
}
