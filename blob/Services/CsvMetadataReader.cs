using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SimpleBlob.Cli.Services
{
    /// <summary>
    /// CSV-based metadata reader.
    /// </summary>
    public sealed class CsvMetadataReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CsvMetadataReader"/>
        /// class.
        /// </summary>
        public CsvMetadataReader()
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
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            List<Tuple<string, string>> metadata = new List<Tuple<string, string>>();

            using CsvReader csv = new CsvReader(reader,
                new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = Delimiter,
                    HasHeaderRecord = false
                });
            while (csv.Read())
            {
                string name = csv.GetField(0).Trim();
                if (string.IsNullOrEmpty(name)) continue;
                string value = csv.GetField(1);
                metadata.Add(Tuple.Create(name, value));
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
            if (path == null) throw new ArgumentNullException(nameof(path));

            using StreamReader reader = new StreamReader(path, Encoding.UTF8);
            return Read(reader);
        }
    }
}
