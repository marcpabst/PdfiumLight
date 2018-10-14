using System;
using System.IO;

namespace PdfiumLight
{
    internal static class StreamExtensions
    {
        public static byte[] ToByteArray(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            if (stream is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }

            if (stream.CanSeek)
                return ReadBytesFast(stream);
            else
                return ReadBytesSlow(stream);
        }

        private static byte[] ReadBytesFast(Stream stream)
        {
            byte[] data = new byte[stream.Length];

            int offset = 0;

            while (offset < data.Length)
            {
                int read = stream.Read(data, offset, data.Length - offset);

                if (read <= 0)
                    break;

                offset += read;
            }

            if (offset < data.Length)
                throw new InvalidOperationException("Incorrect length reported");

            return data;
        }

        private static byte[] ReadBytesSlow(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                CopyStream(stream, memoryStream);

                return memoryStream.ToArray();
            }
        }

        public static void CopyStream(Stream from, Stream to)
        {
            if (@from is null)
                throw new ArgumentNullException(nameof(from));

            if (to is null)
                throw new ArgumentNullException(nameof(to));

            var buffer = new byte[4096];

            while (true)
            {
                int read = from.Read(buffer, 0, buffer.Length);

                if (read == 0)
                    return;

                to.Write(buffer, 0, read);
            }
        }
    }
}