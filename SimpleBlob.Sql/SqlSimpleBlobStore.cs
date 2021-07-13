using Force.Crc32;
using System;
using System.Data;
using System.IO;

namespace SimpleBlob.Sql
{
    /// <summary>
    /// SQL-based simple BLOB store.
    /// </summary>
    public abstract class SqlSimpleBlobStore
    {
        /// <summary>
        /// The name of the items table.
        /// </summary>
        public const string T_ITEM = "item";

        /// <summary>
        /// The name of the item properties table.
        /// </summary>
        public const string T_PROP = "item_property";

        /// <summary>
        /// The name of the item contents table.
        /// </summary>
        public const string T_CONT = "item_content";

        /// <summary>
        /// The block size used for BLOB content.
        /// </summary>
        protected const int BLOCK_SIZE = 8192;

        private bool _disposed;
        private IDbConnection _connection;

        /// <summary>
        /// The connection string.
        /// </summary>
        protected readonly string _connString;

        /// <summary>
        /// Gets the default connection for this store. This connection is open
        /// and ready to be used or re-used.
        /// </summary>
        protected IDbConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = GetConnection(_connString);
                    _connection.Open();
                }
                return _connection;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlSimpleBlobStore"/>
        /// class.
        /// </summary>
        /// <param name="connString">The connection string.</param>
        /// <exception cref="ArgumentNullException">connString</exception>
        protected SqlSimpleBlobStore(string connString)
        {
            _connString = connString
                ?? throw new ArgumentNullException(nameof(connString));
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <param name="connString">The connection string.</param>
        /// <returns>The connection.</returns>
        protected abstract IDbConnection GetConnection(string connString);

        /// <summary>
        /// Adds a parameter to the specified command.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="type">The parameter type.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="command">The target command.</param>
        /// <returns>The added parameter.</returns>
        public static IDbDataParameter AddParameter(string name, DbType type,
            object value, IDbCommand command)
        {
            IDbDataParameter p = command.CreateParameter();
            p.ParameterName = name;
            p.DbType = type;
            p.Value = value;
            command.Parameters.Add(p);
            return p;
        }

        /// <summary>
        /// Reads the content from the specified string, returning it as a bytes
        /// array with its CRC32C.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>Tuple with 1=bytes array and 2=CRC32C.</returns>
        /// <exception cref="ArgumentNullException">content</exception>
        protected static Tuple<byte[],long> ReadContent(Stream content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            MemoryStream ms = new MemoryStream();
            BinaryReader reader = new BinaryReader(content);
            uint crc = 0;

            while (true)
            {
                byte[] buf = reader.ReadBytes(BLOCK_SIZE);
                crc = Crc32Algorithm.Append(crc, buf);
                ms.Write(buf);
                if (buf.Length < BLOCK_SIZE) break;
            }

            return Tuple.Create(ms.ToArray(), Convert.ToInt64(crc));
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and
        /// unmanaged resources; <c>false</c> to release only unmanaged
        /// resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _connection?.Dispose();
                    _connection = null;
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing,
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
