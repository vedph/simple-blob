using SimpleBlob.Core;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace SimpleBlob.Api.Models;

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

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("ItemId=").Append(ItemId);
        if (Properties?.Length > 0)
        {
            sb.Append(": ");
            sb.Append(string.Join("; ",
                Properties.Select(p => $"{p.Name}={p.Value}")));
        }
        return sb.ToString();
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

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Name}={Value}";
    }
}
