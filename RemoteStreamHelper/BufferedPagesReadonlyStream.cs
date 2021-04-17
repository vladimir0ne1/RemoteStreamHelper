using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace RemoteStreamHelper
{
    /// <summary>
    /// Adds a buffering layer based on pages to read only operations on another stream.
    /// Usage example: read top x rows from zipped csv file in the network stream.
    /// </summary>
    [PublicAPI]
    public class BufferedPagesReadonlyStream : Stream
    {
        private const int DefaultPageSize = 1024 * 16;
        private readonly Stream baseStream;
        private readonly int pageSize;
        private readonly Dictionary<long, byte[]> cachedPages = new Dictionary<long, byte[]>();

        private long position;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferedPagesReadonlyStream" /> class.
        /// </summary>
        /// <param name="baseStream">Base stream to read and wrap with paged buffer.</param>
        /// <param name="pageSize">Buffer page size.</param>
        public BufferedPagesReadonlyStream(Stream baseStream, int pageSize = DefaultPageSize)
        {
            this.baseStream = baseStream;
            this.pageSize = pageSize;
            Length = baseStream.Length;
        }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => true;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length { get; }

        /// <inheritdoc />
        public override long Position
        {
            get => position;
            set => Seek(value, value >= 0 ? SeekOrigin.Begin : SeekOrigin.End);
        }

        private long PageOffset => position % pageSize;

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (position < 0 || position >= Length)
            {
                return 0;
            }

            var firstPageToRead = position / pageSize;
            var lastPageToRead = (position + count) / pageSize;
            var pagesToRead = lastPageToRead - firstPageToRead + 1;

            var data = new byte[pagesToRead * pageSize];
            var copied = 0;
            for (var i = firstPageToRead; i <= lastPageToRead; i++)
            {
                var pageData = ReadSinglePage(i);
                pageData.CopyTo(data, copied);
                copied += pageSize;
            }

            Array.Copy(data, PageOffset, buffer, offset, count);
            position += count;

            return count;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            var newPosition = position;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;
                case SeekOrigin.Current:
                    newPosition += offset;
                    break;
                case SeekOrigin.End:
                    newPosition = Length - Math.Abs(offset);
                    break;
            }
            if (newPosition < 0 || newPosition > Length)
            {
                throw new InvalidOperationException("Stream position is invalid.");
            }

            position = newPosition;

            return position;
        }

        /// <summary>
        /// Not applicable. Throws <see cref="NotSupportedException"/> exception.
        /// </summary>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <summary>
        /// Not applicable. Throws <see cref="NotSupportedException"/> exception.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <summary>
        /// Not applicable. Throws <see cref="NotSupportedException"/> exception.
        /// </summary>
        public override void Flush() => throw new NotSupportedException();

        private byte[] ReadSinglePage(long pageNumber)
        {
            if (cachedPages.TryGetValue(pageNumber, out var pageData) && pageData != null)
            {
                return pageData;
            }

            pageData = new byte[pageSize];
            baseStream.Seek(pageNumber * pageSize, SeekOrigin.Begin);
            baseStream.Read(pageData, 0, pageSize);
            cachedPages[pageNumber] = pageData;

            return pageData;
        }
    }
}
