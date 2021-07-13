using System;
using System.IO;

namespace SimpleBlob.Core
{
    /// <summary>
    /// A BLOB item's content.
    /// </summary>
    public class BlobItemContent
    {
        /// <summary>
        /// Gets or sets the identifier of the item this content belongs to.
        /// </summary>
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the item.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Gets or sets the item's content hash (CRC32C).
        /// See https://github.com/force-net/Crc32.NET.
        /// </summary>
        public long Hash { get; set; }

        /// <summary>
        /// Gets or sets the content's size in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the content stream.
        /// </summary>
        public Stream Content { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who last modified this item.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The date and time of the last modification.
        /// </summary>
        public DateTime DateModified { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobItemContent"/> class.
        /// </summary>
        public BlobItemContent()
        {
            DateModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{ItemId} [{MimeType}] {Size}";
        }
    }
}
