﻿using Fusi.Tools.Data;
using System;
using System.Collections.Generic;

namespace SimpleBlob.Core;

/// <summary>
/// Filter for items.
/// </summary>
/// <seealso cref="PagingOptions" />
public class BlobItemFilter : PagingOptions
{
    /// <summary>
    /// Gets or sets the ID to match. You can use <c>*</c>=any
    /// character 0-N times, <c>?</c>=any character 0-1 times.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the content.
    /// </summary>
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
    /// Gets or sets the user identifier for the item or its content.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets the properties to match. Any of these properties
    /// should match.
    /// </summary>
    public List<Tuple<string,string>> Properties { get; set; }
}
