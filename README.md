# Simple BLOB Store

A very simple BLOB store with minimal dependencies. This is used internally, as a support subsystem for other projects, but can eventually be used as a standalone utility service.

Currently the only implemented RDBMS is PostgreSQL, but others may follow.

Projects:

- `SimpleBlob.Core`: core components.
- `SimpleBlob.Sql`: shared components for SQL-based implementations.
- `SimpleBlob.PgSql`: PostgresSQL implementation.
- `SimpleBlob.Api.Models`: models used by API.
- `SimpleBlobApi`: API wrapper.
- `blob`: CLI client.

## Docker

Quick Docker image build:

```bash
docker build . -t vedph2020/simple-blob-api:1.0.0 -t vedph2020/simple-blob-api:latest
```

(replace with the current version).

## API

### Account

`GET /api/accounts/emailexists/{email}`: Check if the specified email address is already registered.

`GET /api/accounts/nameexists/{name}`: Check if the specified user name is already registered.

`POST /api/accounts/register`: Registers the specified user.

`GET /api/accounts/resendconfirm/{email}`: Resends the confirmation email.

`GET /api/accounts/confirm`: Confirms the registration.

`POST /api/accounts/changepassword`: Changes the user's password.

`POST /api/accounts/resetpassword/request`: Requests the password reset. This generates an email message to the requester, with a special link to follow to effectively reset his password.

`GET /api/accounts/resetpassword/apply`: Resets the password using the received token.

`DELETE /api/accounts/{name}`: Delete the user with the specified username.

### Authentication

`POST /api/auth/login`: Logins the specified user.

`GET /api/auth/logout`: Logs the user out.

### Item

`GET /api/items`: Gets the items matching the specified filter.

`POST /api/items`: Adds or updates the specified item.

`GET /api/items/{id}`: Gets the item with the specified ID.

`DELETE /api/items/{id}`: Deletes the item with the specified ID.

### ItemContent

`POST /api/contents/{id}`: Uploads the BLOB item's content.

`GET /api/contents/{id}`: Downloads the BLOB item's content.

`GET /api/contents/{id}/meta`: Gets the BLOB item's content metadata.

### ItemProperty

`GET /api/properties/{id}`: Gets the properties of the item with the specified ID.

`DELETE /api/properties/{id}`: Deletes all the properties of the BLOB item with the specified ID.

`POST /api/properties/{id}/add`: Adds the specified properties to a BLOB item.

`POST /api/properties/{id}/set`: Sets the specified properties for a BLOB item.

### User

`GET /api/users`: Gets the specified page from the list of registered users. Use page size=0 to get all the users at once.

`PUT /api/users`: Update the specified user data.

`GET /api/users/{name}`: Gets the details about the user with the specified ID.

`GET /api/user-info`: Gets the details about the current user.

`GET /api/users-from-names`: Gets information about all the users whose names are specified.

`POST /api/users/{name}/roles`: Adds the user to the specified roles.

`DELETE /api/users/{name}/roles`: Removes the user from the specified roles.

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

### Delete Command

This command deletes the specified BLOB item.

Syntax:

```ps1
./blob delete ItemId [-c] [-u UserName] [-p Password]
```

where:

- `ItemId` is the ID of the item to delete.
- `-c` skips the confirmation prompt.
- `-u` the user name. If not specified, you will be prompted for it.
- `-p` the password. If not specified, you will be prompted for it.

Sample:

```ps1
./blob delete samples|fam-ge-tro-ric711-000000_01 -c -u zeus -p P4ss-W0rd!
```

### Upload Command

This command uploads a set of files, as defined from an input folder and a files mask. The mask can be a regular file system mask, or a regular expression. Also, files can optionally be recursively searched starting from the input folder.

It is assumed that each file matching the mask has in the same location a corresponding metadata file, with the same name suffixed with a custom extension. By default this extension is `.meta`. So, if a file to upload is `test.txt`, then the corresponding metadata file should be placed in the same directory with name `test.txt.meta`.

You should also specify the MIME type for the files to upload. If you don't specify any, the type will be automatically derived from the file extension, when possible. This follows the mapping of MIME types defined in `blob/Assets/MimeTypes.csv` (as derived from this [list of common MIME types](https://gist.github.com/jimschubert/94894c938d8f9f64c6863b28c70a22cc)). You can direct the tool to use another file, as far as it has the same structure: a CSV file with a header row and at least 2 columns with name `extension` and `type`.

Syntax:

```ps1
./blob upload <InputDir> <FileMask> [-x] [-r] [-t MimeType] [-m MetaExtension] [-e ExtensionAndMimeTypeList] [--meta-sep MetaSeparator] [-l IdSeparator] [-d] [-u UserName] [-p Password]
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
./blob list [-n PageNumber] [-z PageSize] [-i IdFilter] [-m MimeType] [-d MinDate:MaxDate] [-s MinSize:MaxSize] [-l LastUser] [-o <Name>=<Value>] [-f OutputFilePath] [--meta-sep MetaSeparator] [-u UserName] [-p Password]
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
- `--meta-sep` the separator used for the metadata file. The default is comma (`,`).
- `-f` the output file path. If not specified, the output will be displayed.
- `--id-sep` the virtual path separator used in item IDs. The default is pipe (`|`).
- `-u` the user name. If not specified, you will be prompted for it.
- `-p` the password. If not specified, you will be prompted for it.

Sample:

```ps1
./blob download c:\users\dfusi\desktop\down\ -u zeus -p P4ss-W0rd!
```

### Add Properties Command

This command adds the specified properties to a BLOB item. The properties can be just added, or can replace all the existing properties of the item, according to the option chosen. So, you can also use this command to remove all the properties from an item.

Syntax:

```ps1
./blob add-props <ItemId> [-o <Name>=<Value>] [-f MetadataFilePath] [--meta-sep MetaSeparator] [-r] [-u UserName] [-p Password]
```

- `-o` the property. Each property has format name`=`value. Repeat `-o` for multiple properties.
- `-f` the optional metadata file path. If specified, properties will be loaded from that file. This is a delimited file without header.
- `--meta-sep` the separator used for the metadata file. The default is comma (`,`).
- `-r` remove all the existing properties before adding the new ones (if any).
- `-u` the user name. If not specified, you will be prompted for it.
- `-p` the password. If not specified, you will be prompted for it.

If both the metadata file and `-o` are used to specify properties, these will be combined together.

Sample:

```ps1
./blob add-props samples|fam-ge-tro-ric711-000000_01 -o category=test -u zeus -p P4ss-W0rd!
```

### List Users Command

This command lists the registered users.

Syntax:

```ps1
./blob list-users [-n PageNumber] [-z PageSize] [-m NameOrIdFilter] [-f OutputFilePath] [-u UserName] [-p Password]
```

where:

- `-n` the page number (1-N). Default=1.
- `-z` the page size. Default=20.
- `-m` the user name or ID filter. Any portion of the name/ID must match the filter.
- `-f` the output file path. If not specified, the output will be displayed.
- `-u` the user name. If not specified, you will be prompted for it.
- `-p` the password. If not specified, you will be prompted for it.

Sample:

```ps1
./blob list-users -u zeus -p P4ss-W0rd!
```

### Add User Command

This command adds a new user to the BLOB service.

Syntax:

```ps1
./blob add-user <Name> <Password> <Email> [-f FirstName] [-l LastName] [-u UserName] [-p Password]
```

where:

- `Name` the name of the user to add. This is the username and must be unique in the service.
- `Password` the password for the user being added.
- `Email` the email address of the user being added.
- `-f` the first name of the user being added.
- `-l` the last name of the user being added.
- `-u` the user name. If not specified, you will be prompted for it.
- `-p` the password. If not specified, you will be prompted for it.

Sample:

```ps1
./blob add-user tester P4ss-W0rd! tester@somewhere.org -f Mario -l Rossi -u zeus -p P4ss-W0rd!
```

### Delete User Command

This commands deletes the specified user from the BLOB service.

Syntax:

```ps1
./blob delete-user <Name> [-c] [-u UserName] [-p Password]
```

where:

- `Name` the name of the user to delete.
- `-c` skips the confirmation prompt.
- `-u` the user name. If not specified, you will be prompted for it.
- `-p` the password. If not specified, you will be prompted for it.

Sample:

```ps1
./blob delete-user tester -c -u zeus -p P4ss-W0rd!
```

### Add User Roles Command

This commands adds the specified roles to a user.

Syntax:

```ps1
./blob add-user-roles <Name> [-r RoleName] [-u UserName] [-p Password]
```

where:

- `Name` the name of the user to delete.
- `-r` the name of the role to add. Repeat this option for all the roles you want to add.
- `-u` the user name. If not specified, you will be prompted for it.
- `-p` the password. If not specified, you will be prompted for it.

Sample:

```ps1
./blob add-user-roles tester -r admin -u zeus -p P4ss-W0rd!
```

### Delete User Roles Command

This commands deletes the specified roles of a user.

Syntax:

```ps1
./blob delete-user-roles <Name> [-r RoleName] [-u UserName] [-p Password]
```

where:

- `Name` the name of the user to delete.
- `-r` the name of the role to delete. Repeat this option for all the roles you want to delete.
- `-u` the user name. If not specified, you will be prompted for it.
- `-p` the password. If not specified, you will be prompted for it.

Sample:

```ps1
./blob delete-user-roles tester -r admin -u zeus -p P4ss-W0rd!
```
