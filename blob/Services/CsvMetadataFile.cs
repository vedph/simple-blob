using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleBlob.Cli.Services;

/// <summary>
/// CSV-based metadata file.
/// </summary>
public sealed class CsvMetadataFile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvMetadataFile"/>
    /// class.
    /// </summary>
    public CsvMetadataFile()
    {
        Delimiter = ",";
    }

    /// <summary>
    /// Gets or sets the delimiter. Default is <c>,</c>.
    /// </summary>
    public string Delimiter { get; set; }

    /// <summary>
    /// Reads the specified reader.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>
    /// Tuple of metadata where 1=name and 2=value.
    /// </returns>
    /// <exception cref="ArgumentNullException">reader</exception>
    public IList<Tuple<string, string>> Read(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        List<Tuple<string, string>> metadata = new();

        using CsvReader csv = new(reader,
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = Delimiter,
                HasHeaderRecord = false
            });
        while (csv.Read())
        {
            string? name = csv.GetField(0)?.Trim();
            if (string.IsNullOrEmpty(name)) continue;
            string? value = csv.GetField(1);
            if (name != null)
                metadata.Add(Tuple.Create(name, value ?? ""));
        }
        return metadata;
    }

    /// <summary>
    /// Reads the metadata from the file with the specified path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>
    /// Tuple of metadata where 1=name and 2=value.
    /// </returns>
    /// <exception cref="ArgumentNullException">path</exception>
    public IList<Tuple<string, string>> Read(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using StreamReader reader = new(path, Encoding.UTF8);
        return Read(reader);
    }

    /// <summary>
    /// Writes the specified metadata to the specified writer.
    /// </summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="writer">The writer.</param>
    /// <exception cref="ArgumentNullException">metadata or writer</exception>
    public void Write(IList<Tuple<string,string>> metadata, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(writer);

        using CsvWriter csv = new(writer, new CsvConfiguration(
            CultureInfo.InvariantCulture)
        {
            Delimiter = Delimiter,
            HasHeaderRecord = false
        });
        foreach (var m in metadata.OrderBy(t => t.Item1).ThenBy(t => t.Item2))
        {
            csv.WriteField(m.Item1);
            csv.WriteField(m.Item2);
            csv.NextRecord();
        }
        csv.Flush();
    }

    /// <summary>
    /// Writes the specified metadata to a file with the specified path.
    /// </summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="path">The file path.</param>
    /// <exception cref="ArgumentNullException">metadata or path</exception>
    public void Write(IList<Tuple<string, string>> metadata, string path)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(path);

        using StreamWriter writer = new(path, false, Encoding.UTF8);
        Write(metadata, writer);
    }
}
