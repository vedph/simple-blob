using SimpleBlob.Core;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SimpleBlobApi.Models
{
    /// <summary>
    /// Blob item properties.
    /// </summary>
    public sealed class BlobItemPropertiesModel
    {
        /// <summary>
        /// The item identifier.
        /// </summary>
        [Required]
        [MaxLength(300)]
        public string ItemId { get; set; }

        /// <summary>
        /// The properties.
        /// </summary>
        public BlobItemPropertyModel[] Properties { get; set; }

        /// <summary>
        /// Converts this model into a list of properties.
        /// </summary>
        /// <returns>Properties.</returns>
        public IList<BlobItemProperty> ToProperties()
        {
            List<BlobItemProperty> properties = new List<BlobItemProperty>();
            if (Properties?.Length > 0)
            {
                foreach (BlobItemPropertyModel p in Properties)
                {
                    properties.Add(new BlobItemProperty
                    {
                        ItemId = ItemId,
                        Name = p.Name,
                        Value = p.Value
                    });
                }
            }
            return properties;
        }
    }

    /// <summary>
    /// A single BLOB item's property.
    /// </summary>
    public sealed class BlobItemPropertyModel
    {
        /// <summary>
        /// The property name.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// The property value.
        /// </summary>
        [MaxLength(1000)]
        public string Value { get; set; }
    }
}
