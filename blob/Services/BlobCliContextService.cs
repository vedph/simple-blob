namespace SimpleBlob.Cli.Services;

/// <summary>
/// CLI context service.
/// </summary>
public class BlobCliContextService
{
    public BlobCliContextServiceConfig Configuration { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobCliContextService"/>
    /// class.
    /// </summary>
    /// <param name="config">The configuration.</param>
    public BlobCliContextService(BlobCliContextServiceConfig config)
    {
        Configuration = config;
    }
}

/// <summary>
/// Configuration for <see cref="BlobCliContextService"/>.
/// </summary>
public class BlobCliContextServiceConfig
{
    /// <summary>
    /// Gets or sets the connection string to the database.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the local directory to use when loading resources
    /// from the local file system.
    /// </summary>
    public string? LocalDirectory { get; set; }
}