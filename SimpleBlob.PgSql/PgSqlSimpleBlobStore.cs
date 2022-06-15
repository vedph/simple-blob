using Fusi.Tools.Data;
using Npgsql;
using NpgsqlTypes;
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
            using StreamReader reader = new(
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
            List<BlobItem> items = new();
            while (reader.Read())
            {
                items.Add(new BlobItem
                {
                    Id = reader.GetString(reader.GetOrdinal("id")),
                    UserId = reader.GetString(reader.GetOrdinal("user_id")),
                    DateModified = reader.GetDateTime(reader.GetOrdinal("date_modified"))
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
            cmd.CommandText = $"INSERT INTO {T_ITEM}(id,user_id,date_modified)\n" +
                "VALUES(@id,@user_id,@date_modified)\n" +
                "ON CONFLICT(id) DO UPDATE " +
                "SET user_id=@user_id,date_modified=@date_modified;";

            AddParameter("@id", DbType.String, item.Id, cmd);
            AddParameter("@user_id", DbType.String, item.UserId, cmd);
            AddParameter("@date_modified", DbType.DateTime,
                DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc), cmd);

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
            cmd.CommandText = $"SELECT id,user_id,date_modified FROM {T_ITEM}\n" +
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
            NpgsqlCommand cmd = new(
                $"INSERT INTO {T_CONT}(item_id,mime_type,hash,size," +
                $"user_id,date_modified,content)\n" +
                "VALUES(@item_id,@mime_type,@hash,@size,@user_id,@date_modified,@content)\n" +
                "ON CONFLICT(item_id) DO UPDATE SET mime_type=@mime_type,hash=@hash," +
                "size=@size,user_id=@user_id,date_modified=@date_modified," +
                "content=@content;", (NpgsqlConnection)Connection);

            AddParameter("@item_id", DbType.String, content.ItemId, cmd);
            AddParameter("@mime_type", DbType.String, content.MimeType, cmd);
            AddParameter("@hash", DbType.Int64, t.Item2, cmd);
            AddParameter("@size", DbType.Int64, t.Item1.Length, cmd);
            AddParameter("@user_id", DbType.String, content.UserId, cmd);
            AddParameter("@date_modified", DbType.DateTime,
                DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc), cmd);

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
        /// <param name="metadataOnly">True to get only metadata without content.
        /// </param>
        /// <returns>The content, or null if not found.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        public BlobItemContent GetContent(string id, bool metadataOnly)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            if (metadataOnly)
            {
                IDbCommand cmd = Connection.CreateCommand();
                cmd.CommandText = "SELECT item_id,mime_type,hash,size," +
                    $"user_id,date_modified FROM {T_CONT}\n" +
                    "WHERE item_id=@item_id;";
                AddParameter("@item_id", DbType.String, id, cmd);
                using IDataReader reader = cmd.ExecuteReader();
                if (!reader.Read()) return null;
                return new BlobItemContent
                {
                    ItemId = id,
                    MimeType = reader.GetString(1),
                    Hash = reader.GetInt64(2),
                    Size = reader.GetInt64(3),
                    UserId = reader.GetString(4),
                    DateModified = reader.GetDateTime(5)
                };
            }
            else
            {
                NpgsqlCommand cmd = new("SELECT item_id,mime_type," +
                    "hash,size,user_id,date_modified,content\n" +
                    $"FROM {T_CONT} WHERE item_id=@item_id;",
                    (NpgsqlConnection)Connection);

                AddParameter("@item_id", DbType.String, id, cmd);

                using NpgsqlDataReader reader = cmd.ExecuteReader();

                if (!reader.Read()) return null;

                return new BlobItemContent
                {
                    ItemId = reader.GetString("item_id"),
                    MimeType = reader.GetString("mime_type"),
                    Hash = reader.GetInt64("hash"),
                    Size = reader.GetInt64("size"),
                    UserId = reader.GetString("user_id"),
                    DateModified = reader.GetDateTime("date_modified"),
                    Content = new MemoryStream((byte[])reader["content"])
                };
            }
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
                "WHERE item_id=@item_id ORDER BY name,value;";
            AddParameter("@item_id", DbType.String, id, cmd);

            using IDataReader reader = cmd.ExecuteReader();
            List<BlobItemProperty> properties = new();

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
            cmd.CommandText = $"INSERT INTO {T_PROP}(item_id,name,value)\n" +
                "VALUES(@item_id,@name,@value);";
            AddParameter("@item_id", DbType.String, null, cmd);
            AddParameter("@name", DbType.String, null, cmd);
            AddParameter("@value", DbType.String, null, cmd);

            foreach (var prop in properties)
            {
                ((DbParameter)cmd.Parameters["item_id"]).Value = id;
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
            cmd.CommandText = $"DELETE FROM {T_PROP} WHERE item_id=@item_id;";
            AddParameter("@item_id", DbType.String, id, cmd);
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
