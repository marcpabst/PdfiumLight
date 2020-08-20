using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfiumLight
{
    public static class XPDFium
    {

        public static Image GetImage(this PdfDocument doc, int dpi = 300, int pageNumber = 0)
        {
            PdfPage page = doc.GetPage(pageNumber);
            int width = (int)Math.Round(page.Width * dpi / 72f);
            int height = (int)Math.Round(page.Height * dpi / 72f);
            return page.Render(width, height);
        }

        public static Image GetImage(string Pdf, string password = null, int dpi = 300, int pageNumber = 0)
        {
            using (PdfDocument doc = new PdfDocument(Pdf, password))
                return GetImage(doc, dpi, pageNumber);
        }


        public static Image GetImage(Stream Pdf, string password = null, int dpi = 300, int pageNumber = 0)
        {
            using (PdfDocument doc = new PdfDocument(Pdf, password))
                return GetImage(doc, dpi, pageNumber);
        }

        public static Image GetImage(byte[] Pdf, string password = null, int dpi = 300, int pageNumber = 0)
        {
            using (MemoryStream ms = new MemoryStream(Pdf))
                return GetImage(ms, password, dpi, pageNumber);
        }

        public static IEnumerable<Image> GetImages(this PdfDocument doc, int dpi = 300)
        {
            var images = new List<Image>();
            for (int i = 0; i < doc.PageCount(); i++)
            {
                PdfPage page = doc.GetPage(i);
                int width = (int)Math.Round(page.Width * dpi / 72f);
                int height = (int)Math.Round(page.Height * dpi / 72f);
                images.Add(page.Render(width, height));
            }
            return images;
        }

        public static IEnumerable<Image> GetImages(string Pdf, string password = null, int dpi = 300)
        {
            using (PdfDocument doc = new PdfDocument(Pdf, password))
                return GetImages(doc, dpi);
        }

        public static IEnumerable<Image> GetImages(Stream Pdf, string password = null, int dpi = 300)
        {
            using (PdfDocument doc = new PdfDocument(Pdf, password))
                return GetImages(doc, dpi);
        }

        public static IEnumerable<Image> GetImages(byte[] Pdf, string password = null, int dpi = 300)
        {
            using (MemoryStream ms = new MemoryStream(Pdf))
                return GetImages(ms, password, dpi);
        }
    }
}
