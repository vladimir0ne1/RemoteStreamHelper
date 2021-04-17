using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace RemoteStreamHelper
{
    /// <summary>
    /// Provides extension method(s) for <see cref="Stream"/>.
    /// </summary>
    [PublicAPI]
    public static class StreamX
    {
        /// <summary>
        /// Reads data from <paramref name="stream"/> into <param name="buffer"></param>.
        /// It continue reading until end of stream reached or required bytes count fetched.
        /// Covers situations when underlying <paramref name="stream"/> returns less bytes than requested.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <param name="buffer">The buffer to write the data into.</param>
        /// <param name="offset">
        /// The byte offset in <paramref name="buffer"/> at which to begin writing data from <paramref name="stream"/>.
        /// </param>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        public static int ReadFull(this Stream stream, byte[] buffer, int offset, int count)
        {
            return ReadFullAsync(stream, buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously reads data from <paramref name="stream"/> into <param name="buffer"></param>.
        /// It continue reading until end of stream reached or required bytes count fetched.
        /// Covers situations when underlying <paramref name="stream"/> returns less bytes than requested.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <param name="buffer">The buffer to write the data into.</param>
        /// <param name="offset">
        /// The byte offset in <paramref name="buffer"/> at which to begin writing data from <paramref name="stream"/>.
        /// </param>
        /// <param name="count">Number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous read operation.
        /// The value of the TResult parameter contains the total number of bytes read into the buffer.
        /// </returns>
        public static async Task<int> ReadFullAsync(
            this Stream stream,
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            int read, totalRead = 0;

            do
            {
                read = await stream.ReadAsync(buffer, totalRead + offset, count - totalRead, cancellationToken);
                totalRead += read;
            } while (read > 0 && totalRead < count);

            return totalRead;
        }
    }
}
