using System;

namespace SimpleBlobApi.Models
{
    /// <summary>
    /// BLOB item's content metadata.
    /// </summary>
    public class BlobItemContentMetaModel
    {
        /// <summary>
        /// The identifier of the item this content belongs to.
        /// </summary>
        public string ItemId { get; set; }

        /// <summary>
        /// The MIME type of the item.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// The item's content hash (CRC32C).
        /// See https://github.com/force-net/Crc32.NET.
        /// </summary>
        public long Hash { get; set; }

        /// <summary>
        /// The content's size in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// The identifier of the user who last modified this item.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The date and time of the last modification.
        /// </summary>
        public DateTime DateModified { get; set; }
    }
}
