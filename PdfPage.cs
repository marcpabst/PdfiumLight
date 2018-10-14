using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
                out double left,
                out double right,
                out double bottom,
                out double top
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
            {
                throw new ObjectDisposedException(nameof(PdfPage));
            }

            NativeMethods.FPDF_RenderPage(dc, Page, boundsOriginX, boundsOriginY, boundsWidth, boundsHeight, 0, flags);

            return true;
        }

        /// <summary>
        /// Renders the page.
        /// </summary>
        /// <param name="width">
        /// The full width of the rendered image in px or percentage (if dpiX and dpiY are specified). 
        /// If 0, width will be calculated from height using the correct apsect ratio.
        /// Height and width can not be both set to zero. 
        /// </param>
        /// <param name="height">
        /// The full wiheightth of the rendered image in px or percentage (if dpiX and dpiY are specified). 
        /// If 0, height will be calculated from height using the correct apsect ratio.
        /// Height and width can not be both set to zero.
        /// </param>
        /// <param name="clipX">X value of the start point of the clipping area</param>
        /// <param name="clipY">Y value of the start point of the clipping area</param>
        /// <param name="clipWidth">Width of the clip area</param>
        /// <param name="clipHeight">Height of the clip area</param>
        /// <param name="dpiX">DPI to render page. If set, width and height will accept percentage.</param>
        /// <param name="dpiY">DPI to render page. If set, width and height will accept percentage.</param>
        /// <param name="rotate">Specify rotation.</param>
        /// <param name="flags">Specify flags.</param>
        /// <returns>Image from the page.</returns>
        public Image Render(int width, int height, int clipX, int clipY, int clipWidth, int clipHeight, float dpiX, float dpiY, PdfRotation rotate, PdfRenderFlags flags)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PdfPage));
            }

            if (height == 0 && width != 0)
            {
                height = width * (int)(Height / Width);
            }
            else if (height != 0 && width == 0)
            {
                width = height * (int)(Width / Height);
            }
            else if (height == 0 && width == 0)
            {
                throw new ArgumentException();
            }

            if (dpiX != 0 && dpiY != 0)
            {
                clipWidth = (int)(clipWidth / 100f * width / 100f * Width * 0.013888888888889 * dpiX);
                clipHeight = (int)(clipHeight / 100f * height / 100f * Height * 0.013888888888889 * dpiY);
                width = (int)(width / 100f * Width * 0.013888888888889 * dpiX);
                height = (int)(height / 100f * Height * 0.013888888888889 * dpiY);
            }

            var bitmap = new Bitmap(clipWidth, clipHeight, PixelFormat.Format32bppArgb);

            var data = bitmap.LockBits(new Rectangle(0, 0, clipWidth, clipHeight), ImageLockMode.ReadWrite, bitmap.PixelFormat);

            try
            {
                var handle = NativeMethods.FPDFBitmap_CreateEx(clipWidth, clipHeight, 4, data.Scan0, clipWidth * 4);

                try
                {
                    uint background = (flags & PdfRenderFlags.Transparent) == 0 ? 0xFFFFFFFF : 0x00FFFFFF;

                    NativeMethods.FPDFBitmap_FillRect(handle, 0, 0, clipWidth, clipHeight, background);

                    bool success = RenderPDFPageToBitmap(
                        handle,
                        (int)dpiX, (int)dpiY, -clipX, -clipY, width, height,
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

        /// <summary>
        /// Renders the page.
        /// </summary>
        /// <param name="width">
        /// The full width of the rendered image in px.
        /// If 0, width will be calculated from height using the correct apsect ratio.
        /// Height and width can not be both set to zero. 
        /// </param>
        /// <param name="height">
        /// The full wiheightth of the rendered image in px .
        /// If 0, height will be calculated from height using the correct apsect ratio.
        /// Height and width can not be both set to zero.
        /// </param>
        /// <param name="rotate">Specify rotation.</param>
        /// <param name="flags">Specify flags.</param>
        /// <returns>Image from the page.</returns>
        public Image Render(int width, int height, PdfRotation rotate, PdfRenderFlags flags)
        {
            if (height == 0 && width != 0) height = (int)((float)width * (Height / Width));
            else if (height != 0 && width == 0) width = (int)((float)height * (int)(Width / Height));
            else if (height == 0 && width == 0) throw new ArgumentException();
            return Render(width, height, 0, 0, width, height, 0, 0, rotate, flags);
        }

        /// <summary>
        /// Renders the page.
        /// </summary>
        /// <param name="width">
        /// The full width of the rendered image in px.
        /// If 0, width will be calculated from height using the correct apsect ratio.
        /// Height and width can not be both set to zero. 
        /// </param>
        /// <param name="height">
        /// The full wiheightth of the rendered image in px .
        /// If 0, height will be calculated from height using the correct apsect ratio.
        /// Height and width can not be both set to zero.
        /// </param>
        /// <returns>Image from the page.</returns>
        public Image Render(int width, int height)
        {
            if (height == 0 && width != 0) height = (int)((float)width * (Height / Width));
            else if (height != 0 && width == 0) width = (int)((float)height * (int)(Width / Height));
            else if (height == 0 && width == 0) throw new ArgumentException();
            return Render(width, height, 0, 0, width, height, 0, 0, PdfRotation.Rotate0, PdfRenderFlags.None);
        }

        /// <summary>
        /// Renders the page.
        /// </summary>
        /// <param name="width">
        /// The full width of the rendered image in px or percentage (if dpiX and dpiY are specified). 
        /// If 0, width will be calculated from height using the correct apsect ratio.
        /// Height and width can not be both set to zero. 
        /// </param>
        /// <param name="height">
        /// The full wiheightth of the rendered image in px or percentage (if dpiX and dpiY are specified). 
        /// If 0, height will be calculated from height using the correct apsect ratio.
        /// Height and width can not be both set to zero.
        /// </param>
        /// <param name="dpiX">DPI to render page. If set, width and height will accept percentage.</param>
        /// <param name="dpiY">DPI to render page. If set, width and height will accept percentage.</param>
        /// <param name="rotate">Specify rotation.</param>
        /// <param name="flags">Specify flags.</param>
        /// <returns>Image from the page.</returns>
        public Image Render(int width, int height, float dpiX, float dpiY, PdfRotation rotate, PdfRenderFlags flags)
        {
            return Render(width, height, 0, 0, width, height, dpiX, dpiY, rotate, flags);
        }

        /// <summary>
        /// Renders the page.
        /// </summary>
        /// <param name="width">
        /// The full width of the rendered image in px.
        /// If 0, width will be calculated from height using the correct apsect ratio.
        /// Height and width can not be both set to zero. 
        /// </param>
        /// <param name="height">
        /// The full wiheightth of the rendered image in px.
        /// If 0, height will be calculated from height using the correct apsect ratio.
        /// Height and width can not be both set to zero.
        /// </param>
        /// <param name="clipX">X value of the start point of the clipping area</param>
        /// <param name="clipY">Y value of the start point of the clipping area</param>
        /// <param name="clipWidth">Width of the clip area</param>
        /// <param name="clipHeight">Height of the clip area</param>
        /// <param name="rotate">Specify rotation.</param>
        /// <param name="flags">Specify flags.</param>
        /// <returns>Image from the page.</returns>
        public Image Render(int width, int height, int clipX, int clipY, int clipWidth, int clipHeight, PdfRotation rotate, PdfRenderFlags flags)
        {
            return Render(width, height, clipX, clipY, clipWidth, clipHeight, 0, 0, rotate, flags);
        }

        private bool RenderPDFPageToBitmap(IntPtr bitmapHandle, int dpiX, int dpiY, int boundsOriginX, int boundsOriginY, int boundsWidth, int boundsHeight, int rotate, NativeMethods.FPDF flags, bool renderFormFill)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PdfPage));
            }

            if (renderFormFill)
                flags &= ~NativeMethods.FPDF.ANNOT;

            NativeMethods.FPDF_RenderPageBitmap(bitmapHandle, Page, boundsOriginX, boundsOriginY, boundsWidth, boundsHeight, rotate, flags);

            if (renderFormFill)
            {
                NativeMethods.FPDF_FFLDraw(_form, bitmapHandle, Page, boundsOriginX, boundsOriginY, boundsWidth, boundsHeight, rotate, flags);
            }

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
             out double deviceX,
             out double deviceY
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
                out double deviceX1,
                out double deviceY1
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
                out double deviceX2,
                out double deviceY2
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
        public void RotatePage(PdfRotation rotation) => NativeMethods.FPDFPage_SetRotation(Page, rotation);

        /// <summary>
        /// Get the bounds of the text (specified by index and length) from the page. 
        /// </summary>
        /// <param name="startIndex">The start index of the text</param>
        /// <param name="length">The length of the text</param>
        /// <returns>List of the bounds for the text</returns>
        public PdfRectangle[] GetTextBounds(int startIndex, int length)
        {
            int countRects = NativeMethods.FPDFText_CountRects(TextPage, startIndex, length);

            var result = new PdfRectangle[countRects];

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

                result[i] = new PdfRectangle(PageNumber, bounds);
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
                out int deviceX1,
                out int deviceY1
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
                out int deviceX2,
                out int deviceY2
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