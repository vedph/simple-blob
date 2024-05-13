# Simple BLOB Store

- [Simple BLOB Store](#simple-blob-store)
  - [Docker](#docker)
  - [Quick Start](#quick-start)
  - [Database Schema](#database-schema)
  - [API](#api)
    - [Account](#account)
    - [Auth](#auth)
    - [Item](#item)
    - [ItemContent](#itemcontent)
    - [ItemProperty](#itemproperty)
    - [User](#user)
  - [CLI Tool](#cli-tool)
    - [List Command](#list-command)
    - [GetInfo Command](#getinfo-command)
    - [Delete Command](#delete-command)
    - [Upload Command](#upload-command)
    - [Download Command](#download-command)
    - [Add Properties Command](#add-properties-command)
    - [List Users Command](#list-users-command)
    - [Add User Command](#add-user-command)
    - [Delete User Command](#delete-user-command)
    - [Add User Roles Command](#add-user-roles-command)
    - [Delete User Roles Command](#delete-user-roles-command)
    - [Update User Command](#update-user-command)
    - [Show Settings Command](#show-settings-command)
  - [History](#history)
    - [3.0.2](#302)
    - [3.0.1](#301)
    - [3.0.0](#300)
    - [2.0.6](#206)
    - [2.0.5](#205)
    - [2.0.4](#204)
    - [2.0.3](#203)
    - [2.0.2](#202)
    - [2.0.1](#201)
    - [2.0.0](#200)

A very simple BLOB store with minimal dependencies. This is used internally, as a support subsystem for other projects, but can eventually be used as a standalone utility service.

Currently the only implemented RDBMS is PostgreSQL.

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
docker build . -t vedph2020/simple-blob-api:3.0.2 -t vedph2020/simple-blob-api:latest
```

(replace with the current version).

## Quick Start

1. download `docker-compose.yml` in some folder.

2. create a folder `var/db/pgsql` in the host to hold the database files. If you desire, you can change the path in the script.

3. run it with `docker-compose up` (prefix `sudo` for Linux/MacOS).

You can use the CLI client from project `blob`. This is for Windows, Linux, or MacOS.

If you want to test the functions, use the batch in `demo.zip`. In this case:

1. unzip the full archive with its files and folders in some folder, e.g. `blob`. For Windows you will run `demo.bat`, for Linux `demo.sh`. For MacOS, it's easier to rename `demo.sh` in `demo.command`. Also, for non-Windows OSes ensure that you have execution rights on this file (e.g. `chmod 777 ./demo.sh`).

2. place the CLI binaries under a `cli` subfolder in the `blob` folder (or whatever you named it). Here too, ensure that for non-Windows OSes you have execution rights on the `blob` file.

3. ensure that the BLOB service is running (see above for `docker-compose.yml`).

4. run the batch. This will tour you along the main functions provided by the CLI, step by step.

When you deploy the API to some server, you must change the base API URI in the `appsettings.json` file of the CLI client application, so that it reflects the server location. Also, on the server side:

- _ensure you change the default user's API password_, as the default one used in this repository is of course just for the purpose of playing with the system.
- check the CORS allowed locations to eventually add your own.

In both cases, the easiest way to change these settings is setting them (via environment variables) in the Docker compose script.

## Database Schema

In the current implementation, BLOBs are saved in a RDBMS. This fits the usual deployment scenario where your service has easy access to data services, but runs on a limited space host.

The schema is very simple and essentially includes just 3 tables (the auth tables come from the .NET Framework): `item` for each BLOB item metadata; `item_property` for custom metadata; `item_content` for its content.

![schema](./schema.png)

## API

### Account

`GET /api/accounts/emailexists/{email}`: Checks if the specified email address is already registered.

`GET /api/accounts/nameexists/{name}`: Checks if the specified user name is already registered.

`POST /api/accounts/register?confirmed=true`: Registers the specified user (here we use `confirmed` to avoid email confirmation).

`GET /api/accounts/resendconfirm/{email}`: Resends the confirmation email.

`GET /api/accounts/confirm`: Confirms the registration.

`POST /api/accounts/changepassword`: Changes the user's password.

`POST /api/accounts/resetpassword/request`: Requests the password reset. This generates an email message to the requester, with a special link to follow to effectively reset his password.

`GET /api/accounts/resetpassword/apply`: Resets the password using the received token.

`DELETE /api/accounts/{name}`: Deletes the user with the specified username.

### Auth

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

`PUT /api/users`: Updates the specified user data.

`GET /api/users/{name}`: Gets the details about the user with the specified ID.

`GET /api/user-info`: Gets the details about the current user.

`GET /api/users-from-names`: Gets information about all the users whose names are specified.

`POST /api/users/{name}/roles`: Adds the user to the specified roles.

`DELETE /api/users/{name}/roles`: Removes the user from the specified roles.

## CLI Tool

Note: when using reserved characters like `*` or `|` in UNIX-based OS (Linux, MacOS) remember to escape it with a backslash (e.g. `\*.xml`) or include the value in quotes.

>💡 In bash, enclosing characters in double quotes preserves the literal value of all characters within double quotes (`"..."`), with the exception of dollar, backtick, and backslash. The backslash retains its special meaning only when followed by one of the following characters: dollar, backtick, double quote, backslash, or newline.

### List Command

- 🔒 roles: `admin`, `browser`

🎯 Show a paged list of BLOB items.

>Note that for added security a user with `reader`/`writer` role but without the `browser` (or `admin`) role cannot get the list of items. This way, he can just retrieve a file if he has its name, but he's not able to find out which files are present.

```bash
./blob list [-n <NUMBER>] [-z <SIZE>] [-i <ITEM_ID>] [-t <MIME_TYPE>] [--datemin <DATE>] [--datemax <DATE>] [--szmin <SIZE>] [--szmax <SIZE>] [-l <USER_NAME>] [--props <PROPERTIES>] [-f <FILE_PATH>] [-r] [-u <USER>] [-p <PASSWORD>]
```

- `-n <NUMBER>` the page number (1-N). Default=1.
- `-z <SIZE>` the page size. Default=20.
- `-i <ITEM_ID>` the BLOB ID filter. You can use wildcards `*` and `?`.
- `-t <MIME_TYPE>` the MIME type filter.
- `--datemin <DATE>` the minimum date filter: each date has format `YYYY-MM-DD`.
- `--datemax <DATE>` the maximum date filter: each date has format `YYYY-MM-DD`.
- `--szmin <SIZE>` the minimum size filter (in bytes).
- `--szmax <SIZE>` the maximum size filter (in bytes).
- `-l <USER_NAME>` the user filter. This is the user who last modified the item.
- `-props` the property filter. This is a comma-delimited string, where each property is expressed as name`=`value. Any of these properties must match for the item to match.
- `-f <FILE_PATH>` the output file path. If not specified, the output will be displayed, rather than saved into a file.
- `-r`: raw list, i.e. list all the item IDs only, from all the matching pages. This can be used to get a list of item IDs for some further processing, like when you want to delete all the items or all the items matching some criteria (such commands are not available for security reasons).
- `-u <USER>` the user name. If not specified, you will be prompted for it.
- `-p <PASSWORD>` the password. If not specified, you will be prompted for it.

Example:

```bash
./blob list -n 1 -z 10 -u zeus -p "P4ss-W0rd!"
```

### GetInfo Command

- 🔒 roles: all

🎯 Get information about an item.

```bash
./blob get-info <ITEM_ID> [-f <FILE_PATH>] [-u <USER>] [-p <PASSWORD>]
```

- `<ITEM_ID>` the ID of the item to get info for.
- `-f <FILE_PATH>` the output file path. If not specified, the output will be displayed.
- `-u <USER>` the user name. If not specified, you will be prompted for it.
- `-p <PASSWORD>` the password. If not specified, you will be prompted for it.

Example:

```bash
./blob get-info "samples|fam-ge-tro-ric711-000000_01" -u zeus -p "P4ss-W0rd!"
```

### Delete Command

- 🔒 roles: `admin`, `browser`, `writer`

🎯 Delete the specified BLOB item.

```bash
./blob delete <ITEM_ID> [-c] [-u <USER>] [-p <PASSWORD>]
```

- `<ITEM_ID>` is the ID of the item to delete.
- `-c` skips the confirmation prompt.
- `-u <USER>` the user name. If not specified, you will be prompted for it.
- `-p <PASSWORD>` the password. If not specified, you will be prompted for it.

Example:

```bash
./blob delete "samples|fam-ge-tro-ric711-000000_01" -c -u zeus -p "P4ss-W0rd!"
```

### Upload Command

- 🔒 roles: `admin`, `browser`, `writer`

🎯 Upload a set of files, as defined from an input folder and a files mask. The mask can be a regular file system mask, or a regular expression. Also, files can be recursively searched starting from the input folder.

You might also want to specify the MIME type for the files to upload. If you don't specify any, the type will be automatically derived from the file extension, when possible. This follows the mapping of MIME types defined in `blob/Assets/MimeTypes.csv` (as derived from this [list of common MIME types](https://gist.github.com/jimschubert/94894c938d8f9f64c6863b28c70a22cc)). You can direct the tool to use another file, as far as it has the same structure: a CSV file with a header row and at least 2 columns with name `extension` and `type`.

```bash
./blob upload <INPUT_DIR> <FILE_MASK> [-x] [-r] [-t <MIME_TYPE>] [-e <TYPES_FILE_PATH>] [-m <EXTENSION>] [--metapfx <PREFIX>] [--metasfx <SUFFIX>] [--metasep <SEPARATOR>] [-idsep <SEPARATOR>][-c] [-d] [-u <USER>] [-p <PASSWORD>]
```

- `INPUT_DIR` is the input directory.
- `FILE_MASK` is the file mask. It can be a regular expression if `-x` is specified.
- `-x` specifies that `FILE_MASK` is a regular expression pattern.
- `-r` recurses subdirectories.
- `-t <MIME_TYPE>` specifies the MIME type for _all_ the files matched. Do not specify this option if you want the type to be derived (when possible) from the file's extension.
- `-e <TYPES_FILE_PATH>` the optional CSV MIME types file path, when you want to override the default list of MIME types.
- `-m <EXTENSION>` or `--meta`: the extension to replace to that of the content filename to build the correspondent metadata filename.
- `--metapfx <PREFIX>`: the prefix inserted before the content filename's extension to build the correspondent metadata filename.
- `--metasfx <SUFFIX>`: the suffix appended after the content filename's extension to build the correspondent metadata filename.
- `--metasep <SEPARATOR>` the separator used for the metadata file. The default is comma (`,`).
- `--idsep <SEPARATOR>` the separator used in BLOB IDs in a file-system like convention. The default is pipe (`|`). Slashes (`/` or `\`) automatically get converted into this separator when using file paths as IDs.
- `--noext <EXTENSION>` (repeatable): the extension(s) to exclude from upload (e.g. `.ini`).
- `-c` to theck the file before uploading it. If the file size and CRC32C are the same, its metadata and properties are uploaded, but its content is not. This speeds up the process when some of the files have not changed.
- `-d` dry (preflight) run (do not write data).
- `-u <USER>` the user name. If not specified, you will be prompted for it.
- `-p <PASSWORD>` the password. If not specified, you will be prompted for it.

Note that you can variously combine the `meta` options to build the metadata filename starting from the content filename.

Example:

```bash
./blob upload c:/users/dfusi/desktop/demo/up/ *.* -u zeus -p "P4ss-W0rd!" -c
```

### Download Command

- 🔒 roles: all

🎯 Download all the files matching the specified filters into a root directory, together with their metadata companion file. If the file IDs include a path separator character (which by default is `|`), the same directory structure gets created under the output root folder.

```bash
./blob download <OUTPUT_DIR> [-n <NUMBER>] [-z <SIZE] [-i <ITEM_ID] [-t <MIME_TYPE>] [--datemin <DATE>] [--datemax <DATE>] [--szmin <SIZE>] [--szmax <SIZE>] [-l <USER_NAME>] [--props <PROPERTIES>] [--pages <LIMIT>] [-e <EXTENSION>] [--metasep <SEPARATOR>] [--idsep <SEPARATOR>] [-u <USER>] [-p <PASSWORD>]
```

- `<OUTPUT_DIR>` the output directory.
- `-n <NUMBER>` the page number (1-N). Default=1.
- `-z <SIZE>` the page size. Default=20.
- `-i <ITEM_ID>` the BLOB ID filter. You can use wildcards `*` and `?`.
- `-t <MIME_TYPE>` the MIME type filter.
- `--datemin <DATE>` the minimum date filter: each date has format `YYYY-MM-DD`.
- `--datemax <DATE>` the maximum date filter: each date has format `YYYY-MM-DD`.
- `--szmin <SIZE>` the minimum size filter (in bytes).
- `--szmax <SIZE>` the maximum size filter (in bytes).
- `-l <USER_NAME>` the user filter. This is the user who last modified the item.
- `-props` the property filter. This is a comma-delimited string, where each property is expressed as name`=`value. Any of these properties must match for the item to match.
- `--pages` the maximum count of pages to retrieve. Default=0, i.e. get all the pages.
- `-e` The metadata file extension. Default is `.meta`.
- `--metasep` the separator used for the metadata file. The default is comma (`,`).
- `--idsep` the virtual path separator used in item IDs. The default is pipe (`|`).
- `-u <USER>` the user name. If not specified, you will be prompted for it.
- `-p <PASSWORD>` the password. If not specified, you will be prompted for it.

Example:

```bash
./blob download c:/users/dfusi/desktop/demo/down/ -u zeus -p "P4ss-W0rd!"
```

### Add Properties Command

- 🔒 roles: `admin`, `browser`, `writer`

🎯 Add the specified properties to a BLOB item. The properties can be just added, or can replace all the existing properties of the item, according to the option chosen. Thus, you can also use this command to remove all the properties from an item.

```bash
./blob add-props <ITEM_ID> <PROPERTY>+ [-f <METADATA_PATH>] [--metasep <SEPARATOR>] [-r] [-u <USER>] [-p <PASSWORD>]
```

- `<ITEM_ID>` the item ID to add properties to.
- `<PROPERTY>` the property to set, expressed as name`=`value. You can repeat this argument for each property you want to set.
- `-f <METADATA_PATH>` the optional metadata file path. If specified, properties will be loaded from that file. This is a delimited file without header.
- `--metasep <SEPARATOR>` the separator used for the metadata file. The default is comma (`,`).
- `-r` remove all the existing properties before adding the new ones (if any).
- `-u <USER>` the user name. If not specified, you will be prompted for it.
- `-p <PASSWORD>` the password. If not specified, you will be prompted for it.

>If both the metadata file and arguments are used to specify properties, these will be combined together.

Example:

```bash
./blob add-props aeneis.txt author=Vergilius -u zeus -p "P4ss-W0rd!"
```

### List Users Command

- 🔒 roles: all

🎯 List the registered users.

```bash
./blob list-users [-n <NUMBER>] [-z <SIZE>] [-m <NAME>] [-f <FILE_PATH>] [-u <USER>] [-p <PASSWORD>]
```

- `-n <NUMBER>` the page number (1-N). Default=1.
- `-z <SIZE>` the page size. Default=20.
- `-m <NAME>` the user name or ID filter. Any portion of the name/ID must match the filter.
- `-f <FILE_PATH>` the output file path. If not specified, the output will be displayed.
- `-u <USER>` the user name. If not specified, you will be prompted for it.
- `-p <PASSWORD>` the password. If not specified, you will be prompted for it.

Example:

```bash
./blob list-users -u zeus -p "P4ss-W0rd!"
```

### Add User Command

- 🔒 roles: `admin`

🎯 Add a new user account to the BLOB service.

```bash
./blob add-user <USER_NAME> <USER_PWD> <USER_EMAIL> <FIRST_NAME> <LAST_NAME> [-u <USER>] [-p <PASSWORD>]
```

where:

- `<USER_NAME>` the name of the user to add. This is the username and must be unique in the service.
- `<USER_PWD>` the password for the user being added.
- `<USER_EMAIL>` the email address of the user being added.
- `<FIRST_NAME>` the first name of the user being added.
- `<LAST_NAME>` the last name of the user being added.
- `-u <USER>` the user name. If not specified, you will be prompted for it.
- `-p <PASSWORD>` the password. If not specified, you will be prompted for it.

Example:

```bash
./blob add-user tester "P4ss-W0rd!" tester@somewhere.org Mario Rossi -u zeus -p "P4ss-W0rd!"
```

### Delete User Command

- 🔒 roles: `admin`

🎯 Delete a user account from the BLOB service.

```bash
./blob delete-user <USER_NAME> [-c] [-u <USER>] [-p <PASSWORD>]
```

- `<USER_NAME>` the name of the user to delete.
- `-c` skips the confirmation prompt.
- `-u <USER>` the user name. If not specified, you will be prompted for it.
- `-p <PASSWORD>` the password. If not specified, you will be prompted for it.

Example:

```bash
./blob delete-user tester -c -u zeus -p "P4ss-W0rd!"
```

### Add User Roles Command

- 🔒 roles: `admin`

🎯 Add the specified roles to a user.  Available roles are:

- `admin`: administrator: can do everything and manage accounts.
- `browser`: is a `writer` and a `reader`, with the added ability of _browsing_ the BLOB store.
- `writer`: is a `reader`, with the added ability of _writing_ (upload/delete files) to the BLOB store.
- `reader`: can only read a known file from the BLOB store.

```bash
./blob add-user-roles <USER_NAME> <USER_ROLE>+ [-u <USER>] [-p <PASSWORD>]
```

- `<USER_NAME>` the name of the user to add roles to.
- `<USER_ROLE>` the name of the role to add. Repeat this option for all the roles you want to add.
- `-u <USER>` the user name. If not specified, you will be prompted for it.
- `-p <PASSWORD>` the password. If not specified, you will be prompted for it.

Example:

```bash
./blob add-user-roles tester admin browser -u zeus -p "P4ss-W0rd!"
```

### Delete User Roles Command

- 🔒 roles: `admin`

Delete the specified role(s) of a user.

```bash
./blob delete-user-roles <USER_NAME> <USER_ROLE>+ [-u <USER>] [-p <PASSWORD>]
```

- `<USER_NAME>` the name of the user to add roles to.
- `<USER_ROLE>` the name of the role to add. Repeat this option for all the roles you want to delete.
- `-u <USER>` the user name. If not specified, you will be prompted for it.
- `-p <PASSWORD>` the password. If not specified, you will be prompted for it.

Example:

```bash
./blob delete-user-roles tester admin -u zeus -p "P4ss-W0rd!"
```

### Update User Command

- 🔒 roles: `admin`

🎯 Update the editable properties of a user. Only the properties explicitly specified by options will be updated.

```bash
./blob update-user <USER_NAME> [-e <EMAIL>] [-c <VALUE>] [-f <NAME>] [-l <NAME>] [-k <STATE>] [-u <USER>] [-p <PASSWORD>]
```

- `USER_NAME` the name of the user to update.
- `-e` the user's email address.
- `-c <VALUE>` set (1) or revoke (0) the user's email address confirmation.
- `-f <NAME>` set first name.
- `-l <NAME>` set last name.
- `-k <STATE>` set lockout enabled on (`1`) / off (`0`).
- `-u <USER>` the user name. If not specified, you will be prompted for it.
- `-p <PASSWORD>` the password. If not specified, you will be prompted for it.

Example:

```bash
./blob update-user tester -c -u zeus -p "P4ss-W0rd!"
```

### Show Settings Command

- 🔒 roles: any

🎯 Show relevant tool's settings. This just displays the root URI of the BLOB API service used by this client tool.

```bash
./blob settings
```

## History

### 3.0.2

- 2024-05-13: updated packages.

### 3.0.1

- 2024-02-22: fix to upload command metadata.

### 3.0.0

- 2024-02-22:
  - upgraded to .NET 8.
  - updated packages.
  - better error handling in CLI.

### 2.0.6

- 2023-07-01: default `meta` option to empty in upload command.

### 2.0.5

- 2023-06-28: updated packages.
- 2023-03-31: added `--noext` to upload command.
- 2023-03-30: updated packages.
- 2023-01-25: added `-r` option to list command.

### 2.0.4

- 2023-01-16:
  - updated backend packages.
  - refactored CLI moving auth commands into new `Fusi.Cli.Auth` library.

### 2.0.3

- 2023-01-09:
  - updated packages.
  - refactored CLI infrastructure.
- 2022-12-08: updated to .NET 7.
- 2022-07-01: added page count limit in download command.
- 2022-06-24: updated packages.

### 2.0.2

- 2022-06-15: updated packages. Ensured DateTime has kind when writing it (see [here](https://stackoverflow.com/questions/69961449/net6-and-datetime-problem-cannot-write-datetime-with-kind-utc-to-postgresql-ty)).

### 2.0.1

- 2022-04-22: added update-user, upgraded packages, set confirmed email in add-user, added options for metadata filename building in upload.
- 2022-04-21: fixed metadata extension in upload command.

### 2.0.0

- 2022-04-18: updated packages.
- 2021-11-09: migrated to .NET 6.
