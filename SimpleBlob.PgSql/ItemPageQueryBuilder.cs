using SimpleBlob.Core;
using SimpleBlob.Sql;
using System;
using System.Data;
using System.Text;

namespace SimpleBlob.PgSql
{
    /// <summary>
    /// Query builder for items browsers.
    /// </summary>
    public static class ItemPageQueryBuilder
    {
        private static void AppendClause(int n, string name, string op,
            string value, StringBuilder sb)
        {
            // WHERE/AND name op value
            sb.Append(n == 1 ? "\nWHERE\n" : "\nAND\n");
            sb.Append(name);
            sb.Append(' ').Append(op).Append(' ');
            sb.Append(value);
        }

        /// <summary>
        /// Builds the query for the specified filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="cmd">The target command.</param>
        /// <returns>Query with 1=data query and 2=count query.</returns>
        public static Tuple<string, string> Build(BlobItemFilter filter, IDbCommand cmd)
        {
            bool fromContent = false;

            // head = ... item AS i
            StringBuilder head = new StringBuilder();
            head.Append(SqlSimpleBlobStore.T_ITEM).Append(" AS i");

            // tail will contain WHERE clauses
            StringBuilder tail = new StringBuilder();
            int n = 0;

            // path
            if (!string.IsNullOrEmpty(filter.Path))
            {
                if (filter.Path.Contains('*') || filter.Path.Contains('?'))
                {
                    string path = filter.Path.Replace('*', '%').Replace('?', '_');

                    SqlSimpleBlobStore.AddParameter(
                        "@path", DbType.String, path, cmd);
                    AppendClause(++n, "id", "LIKE", "@path", tail);
                }
                else
                {
                    SqlSimpleBlobStore.AddParameter(
                        "@path", DbType.String, filter.Path, cmd);
                    AppendClause(++n, "id", "=", "@path", tail);
                }
            }

            // MIME type
            if (!string.IsNullOrEmpty(filter.MimeType))
            {
                fromContent = true;
                head.Append("\nINNER JOIN ").Append(SqlSimpleBlobStore.T_CONT)
                    .Append(" AS ic ON i.id=ic.itemid");
                SqlSimpleBlobStore.AddParameter(
                    "@mimetype", DbType.String, filter.MimeType, cmd);
                AppendClause(++n, "ic.mimetype", "=", "@mimetype", tail);
            }

            // min date
            if (filter.MinDateModified != null)
            {
                SqlSimpleBlobStore.AddParameter("@mindate", DbType.DateTime,
                    filter.MinDateModified.Value, cmd);
                AppendClause(++n, "i.datemodified", ">=", "@mindate", tail);
            }

            // max date
            if (filter.MaxDateModified != null)
            {
                SqlSimpleBlobStore.AddParameter("@maxdate", DbType.DateTime,
                    filter.MaxDateModified.Value, cmd);
                AppendClause(++n, "i.datemodified", "<=", "@maxdate", tail);
            }

            // min size
            if (filter.MinSize > 0)
            {
                if (!fromContent)
                {
                    head.Append("\nINNER JOIN ").Append(SqlSimpleBlobStore.T_CONT)
                        .Append(" AS ic ON i.id=ic.itemid");
                    fromContent = true;
                }
                SqlSimpleBlobStore.AddParameter(
                    "@minsize", DbType.Int64, filter.MinSize, cmd);
                AppendClause(++n, "ic.size", ">=", "@minsize", tail);
            }

            // max size
            if (filter.MaxSize > 0)
            {
                if (!fromContent)
                {
                    head.Append("\nINNER JOIN ").Append(SqlSimpleBlobStore.T_CONT)
                        .Append(" AS ic ON i.id=ic.itemid");
                    fromContent = true;
                }
                SqlSimpleBlobStore.AddParameter(
                    "@maxsize", DbType.Int64, filter.MaxSize, cmd);
                AppendClause(++n, "ic.size", "<=", "@maxsize", tail);
            }

            // user ID
            if (!string.IsNullOrEmpty(filter.UserId))
            {
                if (!fromContent)
                {
                    head.Append("\nINNER JOIN ").Append(SqlSimpleBlobStore.T_CONT)
                        .Append(" AS ic ON i.id=ic.itemid");
                }
                SqlSimpleBlobStore.AddParameter("@userid", DbType.String,
                    filter.UserId, cmd);

                tail.Append(n == 1 ? "\nWHERE\n" : "\nAND\n")
                    .Append("(i.userid=@userid OR ic.userid=@userid)");
            }

            // properties
            if (filter.Properties?.Count > 0)
            {
                StringBuilder sub = new StringBuilder();
                sub.Append("SELECT id FROM ").Append(SqlSimpleBlobStore.T_PROP)
                    .AppendLine(" AS ip WHERE");

                for (int i = 0; i < filter.Properties.Count; i++)
                {
                    if (i > 0) sub.Append("\nOR\n");

                    string pn = $"@p{i + 1}n";
                    string pv = $"@p{i + 1}v";
                    SqlSimpleBlobStore.AddParameter(
                        pn, DbType.String, filter.Properties[i].Item1, cmd);
                    SqlSimpleBlobStore.AddParameter(
                        pv, DbType.String, filter.Properties[i].Item2, cmd);
                    sub.Append("(ip.itemid=i.id AND ip.name=").Append(pn)
                       .Append(" AND ip.value LIKE ").Append(pv).Append(')');
                }
                if (++n > 1) tail.Append(" AND ");
                tail.Append("EXISTS(").Append(sub).Append(')');
            }

            StringBuilder data = new StringBuilder();
            data.Append("SELECT i.id,i.userid,i.datemodified FROM ");
            data.Append(head);
            if (tail.Length > 0) data.Append(tail);
            data.Append("\nORDER BY i.id OFFSET ").Append(filter.GetSkipCount())
                .Append(" LIMIT ").Append(filter.PageSize).Append(';');

            StringBuilder tot = new StringBuilder();
            tot.Append("SELECT COUNT(i.id) FROM ");
            tot.Append(head);
            if (tail.Length > 0) tot.Append(tail);
            tot.Append(';');

            return Tuple.Create(data.ToString(), tot.ToString());
        }
    }
}
