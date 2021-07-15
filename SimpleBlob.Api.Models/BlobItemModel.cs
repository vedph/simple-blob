using SimpleBlob.Core;
using System.ComponentModel.DataAnnotations;

namespace SimpleBlob.Api.Models
{
    /// <summary>
    /// A BLOB item.
    /// </summary>
    public sealed class BlobItemModel
    {
        /// <summary>
        /// The item ID. This can include a separator character to represent
        /// a hierarchy, but slashes should be avoided: the forward slash is
        /// reserved for URI segments, while the backslash happens to be
        /// rewritten into <c>/</c> by some services like Kestrel
        /// (https://techblog.dorogin.com/how-to-pass-a-value-with-a-backslash-to-asp-net-4c8540a65f85).
        /// </summary>
        [Required]
        [MaxLength(300)]
        public string Id { get; set; }

        /// <summary>
        /// Converts to item.
        /// </summary>
        /// <returns>The item.</returns>
        public BlobItem ToItem()
        {
            return new BlobItem
            {
                Id = Id,
            };
        }
    }
}
