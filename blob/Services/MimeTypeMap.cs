using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace SimpleBlob.Cli.Services
{
    public sealed class MimeTypeMap
    {
        private Dictionary<string, string> _types;

        public void Load(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            if (_types == null) _types = new Dictionary<string, string>();
            else _types.Clear();

            using CsvReader csv = new CsvReader(reader,
                new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ",",
                    HasHeaderRecord = true
                });
            csv.Read(); // required before reading header
            csv.ReadHeader();
            while (csv.Read())
            {
                _types[csv.GetField("extension").ToLowerInvariant()] =
                    csv.GetField("type");
            }
        }

        public void Load(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            using StreamReader reader = new StreamReader(path, Encoding.UTF8);
            Load(reader);
        }

        public void LoadDefault()
        {
            using var reader = new StreamReader(Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("SimpleBlob.Cli.Assets.MimeTypes.csv"),
                Encoding.UTF8);
            Load(reader);
        }

        public string GetType(string ext)
        {
            if (ext == null) throw new ArgumentNullException(nameof(ext));

            if (_types == null)
            {
                _types = new Dictionary<string, string>();
                LoadDefault();
            }

            ext = ext.ToLowerInvariant();
            return _types.ContainsKey(ext) ? _types[ext] : null;
        }
    }
}
