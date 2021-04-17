using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using JetBrains.Annotations;

namespace RemoteStreamHelper
{
    /// <summary>
    /// Represents Aws S3 object as a readonly stream.
    /// </summary>
    [PublicAPI]
    public class AwsS3ReadonlyStream : Stream
    {
        private readonly IAmazonS3 s3;
        private readonly AmazonS3Uri s3Uri;
        private readonly string s3ObjectEtag;

        /// <summary>
        /// Initializes a new instance of the <see cref="AwsS3ReadonlyStream" /> class.
        /// </summary>
        /// <param name="s3">The <see cref="IAmazonS3"/>.</param>
        /// <param name="s3Uri">The <see cref="AmazonS3Uri"/>.</param>
        /// <param name="s3ObjectMetadata">The <see cref="GetObjectMetadataResponse"/>.</param>
        public AwsS3ReadonlyStream(IAmazonS3 s3, AmazonS3Uri s3Uri, GetObjectMetadataResponse s3ObjectMetadata)
        {
            this.s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
            this.s3Uri = s3Uri ?? throw new ArgumentNullException(nameof(s3Uri));

            Length = s3ObjectMetadata.ContentLength;
            s3ObjectEtag = s3ObjectMetadata.ETag;
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
        public override long Position { get; set; }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var request = new GetObjectRequest
            {
                BucketName = s3Uri.Bucket,
                Key = s3Uri.Key,
                EtagToMatch = s3ObjectEtag,
                ByteRange = new ByteRange(Position, Position + count)
            };

            using var response = await s3.GetObjectAsync(request, cancellationToken);
            await using var responseStream = response.ResponseStream;
            var readBytes = await responseStream.ReadFullAsync(buffer, offset, count, cancellationToken);
            Position += readBytes;

            return readBytes;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => Position + offset,
                SeekOrigin.End => Length - Math.Abs(offset),
                _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
            };

            if (Position < 0)
            {
                throw new IOException($"Seek position {Position} before Begin");
            }

            if (Position > Length)
            {
                throw new IOException($"Seek position {Position} after End");
            }

            return Position;
        }

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Flush() => throw new NotSupportedException();
    }
}
