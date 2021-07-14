using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleBlob.Cli.Services
{
    public static class BlobItemBuilder
    {
        /// <summary>
        /// Builds the JSON code for a BLOB item from its source and optional
        /// metadata.
        /// </summary>
        /// <param name="path">The BLOB path. If <c>path</c> is in metadata,
        /// metadata will override this value.</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>JSON code.</returns>
        /// <exception cref="ArgumentNullException">source</exception>
        public static string Build(string path,
            IList<Tuple<string, string>> metadata)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            StringBuilder sb = new StringBuilder("{");
            bool sourceAsPath = true;

            // metadata
            if (metadata != null)
            {
                int n = 0;

                foreach (var m in metadata)
                {
                    if (m.Item1 == "path") sourceAsPath = false;

                    if (++n > 1) sb.Append(',');
                    sb.Append(JsonHelper.Jsonize(m.Item1, true)).Append(':')
                      .Append(JsonHelper.Jsonize(m.Item2, true));
                }
            }

            if (sourceAsPath)
                sb.Append("\"path\": ").Append(JsonHelper.Jsonize(path, true));

            sb.Append('}');
            return sb.ToString();
        }
    }
}
