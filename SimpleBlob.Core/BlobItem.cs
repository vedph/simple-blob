using System;

namespace SimpleBlob.Core
{
    /// <summary>
    /// A BLOB item. This defines the essential metadata of each BLOB record
    /// in the store.
    /// </summary>
    public class BlobItem
    {
        /// <summary>
        /// The item ID. This can include <c>\</c> to represent a hierarchy.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who last modified this item.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The date and time of the last modification.
        /// </summary>
        public DateTime DateModified { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobItem"/> class.
        /// </summary>
        public BlobItem()
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
            return $"{Id} by {UserId} at {DateModified}";
        }
    }
}
