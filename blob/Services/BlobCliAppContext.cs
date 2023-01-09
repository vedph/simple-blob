﻿using Fusi.Cli.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;

namespace SimpleBlob.Cli.Services;

/// <summary>
/// CLI app context.
/// </summary>
/// <seealso cref="CliAppContext" />
public class BlobCliAppContext : CliAppContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlobCliAppContext"/>
    /// class.
    /// </summary>
    /// <param name="config">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public BlobCliAppContext(IConfiguration? config, ILogger? logger)
        : base(config, logger)
    {
    }

    /// <summary>
    /// Gets the context service.
    /// </summary>
    /// <param name="dbName">The database name.</param>
    /// <exception cref="ArgumentNullException">dbName</exception>
    public virtual BlobCliContextService GetContextService(string dbName)
    {
        if (dbName is null) throw new ArgumentNullException(nameof(dbName));

        return new BlobCliContextService(
            new BlobCliContextServiceConfig
            {
                ConnectionString = string.Format(CultureInfo.InvariantCulture,
                  Configuration!.GetConnectionString("Default")!, dbName),
                LocalDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "Assets")
            });
    }
}