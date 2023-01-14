using SimpleBlob.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SimpleBlob.Api.Models;

/// <summary>
/// BLOB items filter model.
/// </summary>
public sealed class BlobItemFilterModel
{
    /// <summary>
    /// The page number.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; }

    /// <summary>
    /// The page size.
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the ID to match. You can use <c>*</c>=any
    /// character 0-N times, <c>?</c>=any character 0-1 times.
    /// </summary>
    [MaxLength(300)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the content.
    /// </summary>
    [MaxLength(200)]
    public string MimeType { get; set; }

    /// <summary>
    /// Gets or sets the minimum date modified.
    /// </summary>
    public DateTime? MinDateModified { get; set; }

    /// <summary>
    /// Gets or sets the maximum date modified.
    /// </summary>
    public DateTime? MaxDateModified { get; set; }

    /// <summary>
    /// Gets or sets the minimum length of the content.
    /// </summary>
    public long MinSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum length of the content.
    /// </summary>
    public long MaxSize { get; set; }

    /// <summary>
    /// The user identifier for both the item and its content.
    /// </summary>
    [MaxLength(50)]
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets the properties to match. Any of these properties
    /// should match. This is a string where each property is represented
    /// by a <c>name=value</c> pair, and separated by comma.
    /// </summary>
    [MaxLength(500)]
    public string Properties { get; set; }

    /// <summary>
    /// Converts this model to an items filter.
    /// </summary>
    /// <returns>Items filter.</returns>
    public BlobItemFilter ToFilter()
    {
        BlobItemFilter filter = new BlobItemFilter
        {
            PageNumber = PageNumber,
            PageSize = PageSize,
            Id = Id,
            MimeType = MimeType,
            MinDateModified = MinDateModified,
            MaxDateModified = MaxDateModified,
            MinSize = MinSize,
            MaxSize = MaxSize,
            UserId = UserId
        };

        if (!string.IsNullOrEmpty(Properties))
        {
            filter.Properties = new List<Tuple<string, string>>();
            foreach (string pair in Properties.Split(',',
                StringSplitOptions.RemoveEmptyEntries))
            {
                int i = pair.IndexOf('=');
                Tuple<string, string> p = i == -1
                    ? Tuple.Create(pair, "*")
                    : Tuple.Create(pair.Substring(0, i), pair.Substring(i + 1));
                filter.Properties.Add(p);
            }
        }

        return filter;
    }
}
