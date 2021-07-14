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
        /// The item ID. This can include <c>/</c> to represent a hierarchy.
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
