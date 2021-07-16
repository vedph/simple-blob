using Force.Crc32;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using SimpleBlob.Api.Models;
using SimpleBlob.Cli.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    public sealed class UploadCommand : ICommand
    {
        private readonly UploadCommandOptions _options;
        private readonly MimeTypeMap _typeMap;
        private ApiLogin _login;

        public UploadCommand(UploadCommandOptions options)
        {
            _options = options;
            _typeMap = new MimeTypeMap();
        }

        public static void Configure(CommandLineApplication app,
            AppOptions options)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            app.Description = "Upload all the files matching " +
                "the specified mask from the specified directory.";
            app.HelpOption("-?|-h|--help");

            CommandArgument dirArgument = app.Argument("[inputDir]",
                "The input directory");
            CommandArgument maskArgument = app.Argument("[fileMask]",
                "The files mask");

            CommandHelper.AddCredentialsOptions(app);

            CommandOption regexOption = app.Option("--regex|-x",
                "Use a regular expression pattern for the files mask",
                CommandOptionType.NoValue);
            CommandOption recurseOption = app.Option("--recurse|-r",
                "Recurse subdirectories",
                CommandOptionType.NoValue);

            CommandOption mimeTypeOption = app.Option("--type|-t",
                "The MIME type for the files to upload",
                CommandOptionType.SingleValue);
            CommandOption mimeTypeListOption = app.Option("--ext-list|-e",
                "The list of common file extensions with their MIME types. " +
                "This is used when no MIME type is specified with -t.",
                CommandOptionType.SingleValue);

            CommandOption metaExtOption = app.Option("--meta|-m",
                "The extension appended to the content filename " +
                "to represent its metadata in a correspondent file",
                CommandOptionType.SingleValue);
            CommandOption metaDelimOption = app.Option("--metasep|-s",
                "The separator used in delimited metadata files",
                CommandOptionType.SingleValue);

            CommandOption idDelimOption = app.Option("--idsep|-l",
                "The conventional separator used in BLOB IDs",
                CommandOptionType.SingleValue);

            CommandOption checkOption = app.Option("--check|-c",
                "Check for file change before uploading. " +
                "If no change occurred, nothing is done.",
                CommandOptionType.NoValue);

            CommandOption dryOption = app.Option("--dry|-d",
                "Dry run (do not write data)", CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                UploadCommandOptions co = new UploadCommandOptions
                {
                    Configuration = options.Configuration,
                    Logger = options.Logger,
                    InputDir = dirArgument.Value,
                    FileMask = maskArgument.Value,
                    IsRegexMask = regexOption.HasValue(),
                    IsRecursive = recurseOption.HasValue(),
                    MetaExtension = metaExtOption.HasValue()
                        ? metaExtOption.Value() : ".meta",
                    MetaDelimiter = metaDelimOption.HasValue()
                        ? metaDelimOption.Value() : ",",
                    IdDelimiter = idDelimOption.HasValue()
                        ? idDelimOption.Value() : "|",
                    IsDryRun = dryOption.HasValue(),
                    IsCheckEnabled = checkOption.HasValue(),
                    MimeType = mimeTypeOption.Value(),
                    MimeTypeList = mimeTypeListOption.Value()
                };
                CommandHelper.SetCredentialsOptions(app, co);
                options.Command = new UploadCommand(co);

                return 0;
            });
        }

        private async Task<string> AddItemAsync(string id, HttpClient client)
        {
            if (_options.IsDryRun) return null;

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "items", new { id });
            return response.IsSuccessStatusCode
                ? null
                : $"Error adding item {id}: {response.ReasonPhrase}";
        }

        private async Task<string> SetItemPropertiesAsync(string id,
            HttpClient client, IList<Tuple<string, string>> metadata)
        {
            if (_options.IsDryRun || metadata == null || metadata.Count == 0)
                return null;

            BlobItemPropertiesModel model = new BlobItemPropertiesModel
            {
                ItemId = id,
                Properties = metadata.Select(t => new BlobItemPropertyModel
                {
                    Name = t.Item1,
                    Value = t.Item2
                }).ToArray()
            };

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"properties/{id}/set", model);
            return response.IsSuccessStatusCode
                ? null
                : $"Error adding item {id}: {response.ReasonPhrase}";
        }

        private static long GetCrc(string path)
        {
            using FileStream stream = new FileStream(path, FileMode.Open,
                FileAccess.Read, FileShare.Read);
            BinaryReader reader = new BinaryReader(stream);
            uint crc = 0;
            while (true)
            {
                byte[] buf = reader.ReadBytes(8192);
                crc = Crc32Algorithm.Append(crc, buf);
                if (buf.Length < 8192) break;
            }
            return crc;
        }

        private static async Task<Tuple<bool, string>> AreContentEqualAsync(
            string id, HttpClient client, string path)
        {
            HttpResponseMessage r = await client.GetAsync(
                $"contents/{id}/meta");
            if (!r.IsSuccessStatusCode)
            {
                return Tuple.Create(false,
                    $"Error getting content metadata for item {id}");
            }

            FileInfo info = new FileInfo(path);
            BlobItemContentMetaModel meta = await r.Content
                .ReadFromJsonAsync<BlobItemContentMetaModel>();

            // size must be equal
            if (info.Length != meta.Size)
                return Tuple.Create(false, (string)null);

            // CRC32C must be equal
            long crc = GetCrc(path);
            return Tuple.Create(crc == meta.Hash, (string)null);
        }

        private async Task<string> SetItemContentAsync(string id,
            HttpClient client, string path, string apiRootUri)
        {
            if (_options.IsDryRun) return null;

            string uri = apiRootUri + $"contents/{id}";
            string mimeType = _options.MimeType;
            if (string.IsNullOrEmpty(mimeType))
            {
                mimeType = _typeMap.GetType(Path.GetExtension(path));
                if (mimeType == null) return "Unknown extension: " + path;
            }

            // check if required
            if (_options.IsCheckEnabled)
            {
                var t = await AreContentEqualAsync(id, client, path);
                if (t.Item2 != null) return t.Item2;
                if (t.Item1) return null;
            }

            string response = await FileUploader.UploadFile(uri, path,
                _login.Token,
                new Dictionary<string, object>
                {
                    { "mimeType", mimeType },
                    { "id", id }
                });
            // TODO response
            return null;
        }

        private static string SanitizePath(string path, string sep)
        {
            path = path.Replace("/", sep);
            return path.Replace("\\", sep);
        }

        public async Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("Upload Files");
            _options.Logger.LogInformation("---UPLOAD---");

            string apiRootUri = CommandHelper.GetApiRootUriAndNotify(_options);
            if (apiRootUri == null) return 2;

            // load types if required
            if (!string.IsNullOrEmpty(_options.MimeTypeList))
                _typeMap.Load(_options.MimeTypeList);

            // prompt for userID/password if required
            LoginCredentials credentials = new LoginCredentials(
                _options.UserId,
                _options.Password);
            credentials.PromptIfRequired();

            // login
            _login = CommandHelper.LoginAndNotify(apiRootUri, credentials);

            // setup the metadata services
            CsvMetadataReader metaReader = new CsvMetadataReader
            {
                Delimiter = _options.MetaDelimiter
            };

            // setup client
            using HttpClient client = ClientHelper.GetClient(apiRootUri,
                _login.Token);

            // process files
            int count = 0;

            foreach (string path in FileEnumerator.Enumerate(
                _options.InputDir, _options.FileMask, _options.IsRegexMask,
                _options.IsRecursive))
            {
                // skip metadata files
                if (Path.GetExtension(path) == _options.MetaExtension) continue;

                count++;
                _options.Logger.LogInformation($"{count} {path}");
                ColorConsole.WriteEmbeddedColorLine($"[green]{count:0000}[/green] {path}");

                // load metadata if any
                string metaPath = path + _options.MetaExtension;
                IList<Tuple<string, string>> metadata = null;
                if (File.Exists(metaPath)) metadata = metaReader.Read(metaPath);
                string id = metadata?.FirstOrDefault(t => t.Item1 == "id")
                    ?.Item2 ?? SanitizePath(path, _options.IdDelimiter);

                // add/update item
                string error = await AddItemAsync(id, client);
                if (error != null)
                {
                    _options.Logger.LogError(error);
                    ColorConsole.WriteError(error);
                    return 2;
                }

                // set properties
                error = await SetItemPropertiesAsync(id, client, metadata);
                if (error != null)
                {
                    _options.Logger.LogError(error);
                    ColorConsole.WriteError(error);
                    return 2;
                }

                // set content
                error = await SetItemContentAsync(id, client, path, apiRootUri);
                if (error != null)
                {
                    _options.Logger.LogError(error);
                    ColorConsole.WriteError(error);
                    return 2;
                }
            }

            string info = "Upload complete: " + count;
            _options.Logger.LogInformation(info);
            ColorConsole.WriteInfo(info);

            return 0;
        }
    }

    public sealed class UploadCommandOptions : CommandOptions
    {
        public string InputDir { get; set; }
        public string FileMask { get; set; }
        public string MimeType { get; set; }
        public string MimeTypeList { get; set; }
        public bool IsRegexMask { get; set; }
        public bool IsRecursive { get; set; }
        public string MetaExtension { get; set; }
        public string MetaDelimiter { get; set; }
        public string IdDelimiter { get; set; }
        public bool IsDryRun { get; set; }
        public bool IsCheckEnabled { get; set; }
    }
}
