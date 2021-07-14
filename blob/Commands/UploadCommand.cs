using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    public sealed class UploadCommand : ICommand
    {
        private readonly UploadCommandOptions _options;

        public UploadCommand(UploadCommandOptions options)
        {
            _options = options;
        }

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.Description = "Upload all the files matching " +
                "the specified mask from the specified directory.";
            command.HelpOption("-?|-h|--help");

            CommandArgument dirArgument = command.Argument("[inputDir]",
                "The input directory");
            CommandArgument maskArgument = command.Argument("[fileMask]",
                "The files mask");

            CommandOption regexOption = command.Option("--regex|-p",
                "Use a regular expression pattern for the files mask",
                CommandOptionType.NoValue);
            CommandOption recurseOption = command.Option("--recurse|-r",
                "Recurse subdirectories",
                CommandOptionType.NoValue);

            CommandOption metaExtOption = command.Option("--meta|-m",
                "The extension appended to the content filename " +
                "to represent its metadata in a correspondent file",
                CommandOptionType.SingleValue);
            CommandOption metaDelimOption = command.Option("--metasep|-s",
                "The separator used in delimited metadata files",
                CommandOptionType.SingleValue);

            CommandOption userOption = command.Option("--user|-u",
                "The BLOB user name", CommandOptionType.SingleValue);
            CommandOption pwdOption = command.Option("--pwd|-p",
                "The BLOB user password", CommandOptionType.SingleValue);

            CommandOption dryOption = command.Option("--dry|-d",
                "Dry run (do not write data)", CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                options.Command = new UploadCommand(new UploadCommandOptions
                {
                    Configuration = options.Configuration,
                    Logger = options.Logger,
                    UserId = userOption.Value(),
                    Password = pwdOption.Value(),
                    InputDir = dirArgument.Value,
                    FileMask = maskArgument.Value,
                    IsRegexMask = regexOption.HasValue(),
                    IsRecursive = recurseOption.HasValue(),
                    MetaExtension = metaExtOption.HasValue()
                        ? metaExtOption.Value() : ".meta",
                    MetaDelimiter = metaDelimOption.HasValue()
                        ? metaDelimOption.Value() : ",",
                    IsDryRun = dryOption.HasValue()
                });
                return 0;
            });
        }

        private string BuildMetaPath(string path)
            => Path.Combine(
                Path.GetFileNameWithoutExtension(path),
                _options.MetaExtension);

        private async Task<string> AddItemAsync(string id, HttpClient client)
        {
            if (_options.IsDryRun) return null;

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/items", new { id });
            return response.IsSuccessStatusCode
                ? null
                : $"Error adding item {id}: {response.ReasonPhrase}";
        }

        private async Task<string> SetItemPropertiesAsync(string id,
            HttpClient client)
        {
            if (_options.IsDryRun) return null;

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"api/items/{id}/properties/set", new {todo });
            return response.IsSuccessStatusCode
                ? null
                : $"Error adding item {id}: {response.ReasonPhrase}";
        }

        public async Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("Upload Files");
            _options.Logger.LogInformation("---UPLOAD---");

            string apiRootUri = _options.Configuration
                .GetSection("ApiRootUri")?.Value;
            if (string.IsNullOrEmpty(apiRootUri))
            {
                ColorConsole.WriteError("Missing ApiUri in configuration");
                return 2;
            }

            // prompt for userID/password if required
            LoginCredentials credentials = new LoginCredentials(
                _options.UserId,
                _options.Password);
            credentials.PromptIfRequired();

            // login
            Console.WriteLine("Logging in...");
            ApiLogin login = new ApiLogin(apiRootUri);
            if (!login.Login(credentials.UserName, credentials.Password))
            {
                ColorConsole.WriteError("Unable to login");
                return 2;
            }

            // setup the metadata services
            CsvMetadataReader metaReader = new CsvMetadataReader
            {
                Delimiter = _options.MetaDelimiter
            };

            // setup client
            HttpClient client = ClientHelper.GetClient(apiRootUri, login.Token);

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
                string metaPath = BuildMetaPath(path);
                IList<Tuple<string, string>> metadata = null;
                if (File.Exists(metaPath)) metadata = metaReader.Read(metaPath);
                string id = metadata?.FirstOrDefault(t => t.Item1 == "path")
                    ?.Item2 ?? path;

                // add item
                string error = await AddItemAsync(id, client);
                if (error != null)
                {
                    _options.Logger.LogError(error);
                    ColorConsole.WriteError(error);
                    return 2;
                }

                // set properties
                error = await SetItemPropertiesAsync(id, client);
                if (error != null)
                {
                    _options.Logger.LogError(error);
                    ColorConsole.WriteError(error);
                    return 2;
                }

                // set content

                await FileUploader.UploadFile(apiRootUri, path, login.Token);
                // TODO
            }

            return 0;
        }
    }

    public sealed class UploadCommandOptions : CommandOptions
    {
        public string InputDir { get; set; }
        public string FileMask { get; set; }
        public bool IsRegexMask { get; set; }
        public bool IsRecursive { get; set; }
        public string MetaExtension { get; set; }
        public string MetaDelimiter { get; set; }
        public bool IsDryRun { get; set; }
    }
}
