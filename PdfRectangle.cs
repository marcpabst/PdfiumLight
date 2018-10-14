using System;
using System.Drawing;

namespace PdfiumLight
{
    public readonly struct PdfRectangle : IEquatable<PdfRectangle>
    {
        // _page is offset by 1 so that Empty returns an invalid rectangle.
        private readonly int _page;

        public static readonly PdfRectangle Empty = new PdfRectangle();

        public PdfRectangle(int page, RectangleF bounds)
        {
            _page = page + 1;
            Bounds = bounds;
        }

        public PdfRectangle(int page, RectangleF bounds, int astartIndex, int asize)
        {
            _page = page + 1;
            Bounds = bounds;
        }

        public int Page => _page - 1;

        public RectangleF Bounds { get; }

        public bool IsValid => _page != 0;

        public bool Equals(PdfRectangle other)
        {
            return
                Page == other.Page &&
                Bounds == other.Bounds;
        }

        public override bool Equals(object obj)
        {
            return obj is PdfRectangle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Page * 397) ^ Bounds.GetHashCode();
            }
        }

        public static bool operator ==(PdfRectangle left, PdfRectangle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PdfRectangle left, PdfRectangle right)
        {
            return !left.Equals(right);
        }
    }
}