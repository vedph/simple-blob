namespace SimpleBlob.Core;

/// <summary>
/// A property attached to a <see cref="BlobItem"/>.
/// </summary>
public class BlobItemProperty
{
    /// <summary>
    /// Gets or sets the property identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the item identifier.
    /// </summary>
    public string ItemId { get; set; }

    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the property value.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Name}={Value} ({ItemId})";
    }
}
