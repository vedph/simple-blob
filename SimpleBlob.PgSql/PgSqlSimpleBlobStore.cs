using Fusi.Tools.Data;
using Npgsql;
using SimpleBlob.Core;
using SimpleBlob.Sql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace SimpleBlob.PgSql
{
    /// <summary>
    /// Simple BLOB store for PostgreSql.
    /// </summary>
    /// <seealso cref="SqlSimpleBlobStore" />
    public sealed class PgSqlSimpleBlobStore : SqlSimpleBlobStore, ISimpleBlobStore
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PgSqlSimpleBlobStore"/>
        /// class.
        /// </summary>
        /// <param name="connString">The connection string.</param>
        public PgSqlSimpleBlobStore(string connString) : base(connString)
        {
        }

        /// <summary>
        /// Gets a new connection object.
        /// </summary>
        /// <param name="connString">The connection string.</param>
        /// <returns>The connection.</returns>
        protected override IDbConnection GetConnection(string connString)
            => new NpgsqlConnection(connString);

        /// <summary>
        /// Gets the code for the store schema.
        /// </summary>
        /// <returns>Code.</returns>
        public string GetSchema()
        {
            using StreamReader reader = new StreamReader(
                Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("SimpleBlob.PgSql.Assets.Schema.pgsql"),
                Encoding.UTF8);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Gets the specified page of items.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>The page.</returns>
        /// <exception cref="ArgumentNullException">filter</exception>
        public DataPage<BlobItem> GetItems(BlobItemFilter filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            IDbCommand cmd = Connection.CreateCommand();
            var dataAndTot = ItemPageQueryBuilder.Build(filter, cmd);
            cmd.CommandText = dataAndTot.Item2;
            object r = cmd.ExecuteScalar();
            int total;
            if (r == null || (total = Convert.ToInt32(r)) == 0)
            {
                return new DataPage<BlobItem>(filter.PageNumber,
                    filter.PageSize, 0, Array.Empty<BlobItem>());
            }

            cmd.CommandText = dataAndTot.Item1;
            using var reader = cmd.ExecuteReader();
            List<BlobItem> items = new List<BlobItem>();
            while (reader.Read())
            {
                items.Add(new BlobItem
                {
                    Id = reader.GetString(reader.GetOrdinal("id")),
                    UserId = reader.GetString(reader.GetOrdinal("userid")),
                    DateModified = reader.GetDateTime(reader.GetOrdinal("datemodified"))
                });
            }
            return new DataPage<BlobItem>(filter.PageNumber, filter.PageSize,
                total, items);
        }

        /// <summary>
        /// Adds or updates the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="ArgumentNullException">item or userId</exception>
        public void AddItem(BlobItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            // do an upsert as PostgreSql now supports this
            // https://www.postgresql.org/docs/current/sql-insert.html#SQL-ON-CONFLICT
            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandText = $"INSERT INTO {T_ITEM}(id,userid,datemodified)\n" +
                "VALUES(@id,@userid,@datemodified)\n" +
                "ON CONFLICT(id) DO UPDATE " +
                "SET userid=@userid,datemodified=@datemodified;";

            AddParameter("@id", DbType.String, item.Id, cmd);
            AddParameter("@userid", DbType.String, item.UserId, cmd);
            AddParameter("@datemodified", DbType.DateTime, DateTime.UtcNow, cmd);

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Gets the item with the specified identifier.
        /// </summary>
        /// <param name="id">The item's identifier.</param>
        /// <returns>The item, or null if not found.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        public BlobItem GetItem(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandText = $"SELECT id,userid,datemodified FROM {T_ITEM}\n" +
                "WHERE id=@id;";
            AddParameter("@id", DbType.String, id, cmd);
            using IDataReader reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            return new BlobItem
            {
                Id = id,
                UserId = reader.GetString(1),
                DateModified = reader.GetDateTime(2)
            };
        }

        /// <summary>
        /// Deletes the item with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <exception cref="ArgumentNullException">id</exception>
        public void DeleteItem(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandText = $"DELETE FROM {T_ITEM} WHERE id=@id;";
            AddParameter("@id", DbType.String, id, cmd);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Sets the content for the item with the specified identifier.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>True if set, false if item not found.</returns>
        /// <exception cref="ArgumentNullException">content</exception>
        public bool SetContent(BlobItemContent content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            if (GetItem(content.ItemId) == null) return false;

            var t = ReadContent(content.Content);
            NpgsqlCommand cmd = new NpgsqlCommand(
                $"INSERT INTO {T_CONT}(itemid,mimetype,hash,size," +
                $"userid,datemodified,content)\n" +
                "VALUES(@itemid,@mimetype,@hash,@size,@userid,@datemodified,@content)\n" +
                "ON CONFLICT(itemid) DO UPDATE SET mimetype=@mimetype,hash=@hash," +
                "size=@size,userid=@userid,datemodified=@datemodified," +
                "content=@content;", (NpgsqlConnection)Connection);

            AddParameter("@itemid", DbType.String, content.ItemId, cmd);
            AddParameter("@mimetype", DbType.String, content.MimeType, cmd);
            AddParameter("@hash", DbType.Int64, t.Item2, cmd);
            AddParameter("@size", DbType.Int64, t.Item1.Length, cmd);
            AddParameter("@userid", DbType.String, content.UserId, cmd);
            AddParameter("@datemodified", DbType.DateTime, DateTime.UtcNow, cmd);

            // https://stackoverflow.com/questions/46128132/how-to-insert-and-retrieve-image-from-postgresql-using-c-sharp
            NpgsqlParameter p = cmd.CreateParameter();
            p.ParameterName = "@content";
            p.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Bytea;
            p.Value = t.Item1;
            cmd.Parameters.Add(p);

            cmd.ExecuteNonQuery();
            return true;
        }

        /// <summary>
        /// Gets the content of the item with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>The content, or null if not found.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        public BlobItemContent GetContent(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            NpgsqlCommand cmd = new NpgsqlCommand("SELECT itemid,mimetype," +
                "hash,size,userid,datemodified,content\n" +
                $"FROM {T_CONT} WHERE itemid=@itemid;",
                (NpgsqlConnection)Connection);

            AddParameter("@itemid", DbType.String, id, cmd);

            using NpgsqlDataReader reader = cmd.ExecuteReader();

            if (!reader.Read()) return null;

            return new BlobItemContent
            {
                ItemId = reader.GetString("itemid"),
                MimeType = reader.GetString("mimetype"),
                Hash = reader.GetInt64("hash"),
                Size = reader.GetInt64("size"),
                UserId = reader.GetString("userid"),
                DateModified = reader.GetDateTime("datemodified"),
                Content = new MemoryStream((byte[])reader["content"])
            };
        }

        /// <summary>
        /// Gets the properties of the item with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Properties.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        public IList<BlobItemProperty> GetProperties(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            IDbCommand cmd = Connection.CreateCommand();
            cmd.CommandText = $"SELECT id,name,value FROM {T_PROP}\n" +
                "WHERE itemid=@itemid ORDER BY name,value;";
            AddParameter("@itemid", DbType.String, id, cmd);

            using IDataReader reader = cmd.ExecuteReader();
            List<BlobItemProperty> properties = new List<BlobItemProperty>();

            while (reader.Read())
            {
                properties.Add(new BlobItemProperty
                {
                    Id = reader.GetInt32(0),
                    ItemId = id,
                    Name = reader.GetString(1),
                    Value = reader.GetString(2)
                });
            }

            return properties;
        }

        private bool AddProperties(string id, IList<BlobItemProperty> properties,
            IDbTransaction trans)
        {
            if (GetItem(id) == null) return false;

            IDbCommand cmd = Connection.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = $"INSERT INTO {T_PROP}(itemid,name,value)\n" +
                "VALUES(@itemid,@name,@value);";
            AddParameter("@itemid", DbType.String, null, cmd);
            AddParameter("@name", DbType.String, null, cmd);
            AddParameter("@value", DbType.String, null, cmd);

            foreach (var prop in properties)
            {
                ((DbParameter)cmd.Parameters["itemid"]).Value = id;
                ((DbParameter)cmd.Parameters["name"]).Value = prop.Name;
                ((DbParameter)cmd.Parameters["value"]).Value = prop.Value;
                cmd.ExecuteNonQuery();
            }

            return true;
        }

        /// <summary>
        /// Adds the properties.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="properties">The properties.</param>
        /// <returns>True if added, false if target item not found.</returns>
        /// <exception cref="ArgumentNullException">id or properties</exception>
        public bool AddProperties(string id, IList<BlobItemProperty> properties)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            using IDbTransaction trans = Connection.BeginTransaction();
            try
            {
                if (!AddProperties(id, properties, trans)) return false;
                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Sets the properties.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="properties">The properties.</param>
        /// <returns>True if set, false if target item not found.</returns>
        /// <exception cref="ArgumentNullException">id or properties</exception>
        public bool SetProperties(string id, IList<BlobItemProperty> properties)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            using IDbTransaction trans = Connection.BeginTransaction();
            try
            {
                DeleteProperties(id);
                if (!AddProperties(id, properties, trans)) return false;
                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                Debug.WriteLine(ex.ToString());
                throw;
            }
        }

        private void DeleteProperties(string id, IDbTransaction trans)
        {
            IDbCommand cmd = Connection.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = $"DELETE FROM {T_PROP} WHERE itemid=@id;";
            AddParameter("@id", DbType.String, id, cmd);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Deletes the properties.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <exception cref="ArgumentNullException">id</exception>
        public void DeleteProperties(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            DeleteProperties(id, null);
        }
    }
}
