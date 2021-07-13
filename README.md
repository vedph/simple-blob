# Simple BLOB Store

A very simple BLOB store with minimal dependencies. This is used internally, as a support subsystem for other projects, but can eventually be used as a standalone utility service.

Currently the only implemented RDBMS is PostgreSQL, but others may follow.

Projects:

- `SimpleBlob.Core`: core components.
- `SimpleBlob.Sql`: shared components for SQL-based implementations.
- `SimpleBlob.PgSql`: PostgresSQL implementation.
- `SimpleBlobApi`: API wrapper.
