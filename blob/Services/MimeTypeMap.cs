using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace SimpleBlob.Cli.Services;

public sealed class MimeTypeMap
{
    private Dictionary<string, string>? _types;

    public void Load(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        if (_types == null) _types = new Dictionary<string, string>();
        else _types.Clear();

        using CsvReader csv = new(reader,
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true
            });
        csv.Read(); // required before reading header
        csv.ReadHeader();
        while (csv.Read())
        {
            _types[csv.GetField("extension")!.ToLowerInvariant()] =
                csv.GetField("type")!;
        }
    }

    public void Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using StreamReader reader = new(path, Encoding.UTF8);
        Load(reader);
    }

    public void LoadDefault()
    {
        using var reader = new StreamReader(Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("SimpleBlob.Cli.Assets.MimeTypes.csv")!,
            Encoding.UTF8);
        Load(reader);
    }

    public string? GetType(string ext)
    {
        ArgumentNullException.ThrowIfNull(ext);

        if (_types == null)
        {
            _types = new Dictionary<string, string>();
            LoadDefault();
        }

        ext = ext.ToLowerInvariant();
        return _types?.ContainsKey(ext) == true ? _types[ext] : null;
    }
}
