# Simple BLOB Store

A very simple BLOB store with minimal dependencies. This is used internally, as a support subsystem for other projects, but can eventually be used as a standalone utility service.

Currently the only implemented RDBMS is PostgreSQL, but others may follow.

Projects:

- `SimpleBlob.Core`: core components.
- `SimpleBlob.Sql`: shared components for SQL-based implementations.
- `SimpleBlob.PgSql`: PostgresSQL implementation.
- `SimpleBlobApi`: API wrapper.

## Docker

Quick Docker image build:

```bash
docker build . -t vedph2020/simple-blob-api:1.0.0 -t vedph2020/simple-blob-api:latest
```

(replace with the current version).

## CLI Tool

Note: when using `*` in UNIX-based OS (Linux, MacOS) remember to escape it with a backslash (e.g. `\*.xml`).

### List Command

This command gets a paged list of BLOB items.

Syntax:

```ps1
./blob list [-n PageNumber] [-z PageSize] [-i IdFilter] [-m MimeType] [-d MinDate:MaxDate] [-s MinSize:MaxSize] [-l LastUser] [-o PropName=PropValue] [-f OutputFilePath] [-u UserName] [-p Password]
```

where:

- `-n` the page number (1-N). Default=1.
- `-z` the page size. Default=20.
- `-i` the BLOB ID filter. You can use wildcards `*` and `?`.
- `-m` the MIME type filter.
- `-d` the dates range filter: each date has format `YYYY-MM-DD`. You can specify the minimum date only (followed by `:`), the maximum date only (preceded by `:`), or both (min`:`max).
- `-s` the size range filter: each size is in bytes. You can specify the minimum size only (followed by `:`), the maximum size only (preceded by `:`), or both (min`:`max).
- `-l` the user filter. This is the user who last modified the item.
- `-o` the property filter. Each property has format name`=`value. Repeat `-o` for multiple properties; just any of them should be matched.
- `-f` the output file path. If not specified, the output will be displayed.
- `-u` the user name. If not specified, you will be prompted for it.
- `-p` the password. If not specified, you will be prompted for it.

Sample:

```ps1
./blob list -n 1 -z 10 -u zeus -p P4ss-W0rd!
```

### GetInfo Command

This command gets information about an item.

Syntax:

```ps1
./blob get-info ItemId [-f OutputFilePath] [-u UserName] [-p Password]
```

where:

- `-f` the output file path. If not specified, the output will be displayed.
- `-u` the user name. If not specified, you will be prompted for it.
- `-p` the password. If not specified, you will be prompted for it.

Sample:

```ps1
./blob get-info samples|fam-ge-tro-ric711-000000_01 -u zeus -p P4ss-W0rd!
```

### Upload Command

This command uploads a set of files, as defined from an input folder and a files mask. The mask can be a regular file system mask, or a regular expression. Also, files can optionally be recursively searched starting from the input folder.

It is assumed that each file matching the mask has in the same location a corresponding metadata file, with the same name suffixed with a custom extension. By default this extension is `.meta`. So, if a file to upload is `test.txt`, then the corresponding metadata file should be placed in the same directory with name `test.txt.meta`.

You should also specify the MIME type for the files to upload. If you don't specify any, the type will be automatically derived from the file extension, when possible. This follows the mapping of MIME types defined in `blob/Assets/MimeTypes.csv` (as derived from this [list of common MIME types](https://gist.github.com/jimschubert/94894c938d8f9f64c6863b28c70a22cc)). You can direct the tool to use another file, as far as it has the same structure: a CSV file with a header row and at least 2 columns with name `extension` and `type`.

Syntax:

```ps1
./blob upload <InputDir> <FileMask> [-x] [-r] [-t MimeType] [-m MetaExtension] [-e ExtensionAndMimeTypeList] [-s MetaSeparator] [-l IdSeparator] [-d] [-u UserName] [-p Password]
```

where:

- `InputDir` is the input directory.
- `FileMask` is the file mask. It can be a regular expression if `-p` is specified.
- `-x` specifies that `FileMask` is a regular expression pattern.
- `-r` recurses subdirectories.
- `-t` specifies the MIME type for _all_ the files matched. Do not specify this option if you want the type to be derived (when possible) from the file's extension.
- `-m` the extension expected to be found for metadata files. The default is `.meta`.
- `-e` the optional CSV MIME types file path, when you want to override the default list of MIME types.
- `--meta-sep` the separator used for the metadata file. The default is comma (`,`).
- `--id-sep` the separator used in BLOB IDs in a file-system like convention. The default is pipe (`|`). Slashes (`/` or `\`) automatically get converted into this separator when using file paths as IDs.
- `-c` to theck the file before uploading it. If the file size and CRC32C are the same, its metadata and properties are uploaded, but its content is not. This speeds up the process when some of the files have not changed.
- `-d` dry run (do not write to service).
- `-u` the user name. If not specified, you will be prompted for it.
- `-p` the password. If not specified, you will be prompted for it.

Sample:

```ps1
./blob upload c:\users\dfusi\desktop\up\ *.json -t application/json -u zeus -p P4ss-W0rd! -c
```

### Download Command

This command downloads all the files matching the specified filters into a root directory, together with their metadata companion file. If the file IDs include a path separator character, the same directory structure gets created under the output root folder.

Syntax:

```ps1
./blob list [-n PageNumber] [-z PageSize] [-i IdFilter] [-m MimeType] [-d MinDate:MaxDate] [-s MinSize:MaxSize] [-l LastUser] [-o PropName=PropValue] [- OutputFilePath] [-u UserName] [-p Password]
```

where:

- `-n` the page number to start from (1-N). Default=1.
- `-z` the page size. Default=20.
- `-i` the BLOB ID filter. You can use wildcards `*` and `?`.
- `-m` the MIME type filter.
- `-d` the dates range filter: each date has format `YYYY-MM-DD`. You can specify the minimum date only (followed by `:`), the maximum date only (preceded by `:`), or both (min`:`max).
- `-s` the size range filter: each size is in bytes. You can specify the minimum size only (followed by `:`), the maximum size only (preceded by `:`), or both (min`:`max).
- `-l` the user filter. This is the user who last modified the item.
- `-o` the property filter. Each property has format name`=`value. Repeat `-o` for multiple properties; just any of them should be matched.
- `-e` The metadata file extension. Default is `.meta`.
- `--meta-sep` the metadata file delimiter. Default is comma.
- `--id-sep` the virtual path separator used in item IDs. The default is pipe (`|`).
- `-u` the user name. If not specified, you will be prompted for it.
- `-p` the password. If not specified, you will be prompted for it.

Sample:

```ps1
./blob download c:\users\dfusi\desktop\down\ -u zeus -p P4ss-W0rd!
```
