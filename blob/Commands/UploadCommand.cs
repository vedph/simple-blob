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
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        private readonly string _user;
        private readonly string _password;
        private readonly string _inputDir;
        private readonly string _fileMask;
        private readonly bool _regexMask;
        private readonly bool _recursive;
        private readonly string _metaExt;
        private readonly string _metaSep;

        public UploadCommand(AppOptions options, string user, string password,
            string inputDir, string fileMask, bool regexMask, bool recursive,
            string metaExt, string metaSep)
        {
            _config = options.Configuration;
            _logger = options.Logger;
            _user = user;
            _password = password;
            _inputDir = inputDir;
            _fileMask = fileMask;
            _regexMask = regexMask;
            _recursive = recursive;
            _metaExt = metaExt;
            _metaSep = metaSep;
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
            CommandOption metaSepOption = command.Option("--metasep|-s",
                "The separator used in delimited metadata files",
                CommandOptionType.SingleValue);

            CommandOption userOption = command.Option("--user|-u",
                "The BLOB user ID", CommandOptionType.SingleValue);
            CommandOption pwdOption = command.Option("--pwd|-p",
                "The BLOB user password", CommandOptionType.SingleValue);

            command.OnExecute(() =>
            {
                options.Command = new UploadCommand(
                    options,
                    userOption.Value(), pwdOption.Value(),
                    dirArgument.Value, maskArgument.Value, regexOption.HasValue(),
                    recurseOption.HasValue(),
                    metaExtOption.HasValue() ? metaExtOption.Value() : ".meta",
                    metaSepOption.HasValue() ? metaSepOption.Value() : ",");
                return 0;
            });
        }

        private string BuildMetaPath(string path)
            => Path.Combine(Path.GetFileNameWithoutExtension(path), _metaExt);

        public async Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("Upload Files");
            _logger.LogInformation("---UPLOAD---");

            string apiRootUri = _config.GetSection("ApiRootUri")?.Value;
            if (string.IsNullOrEmpty(apiRootUri))
            {
                ColorConsole.WriteError("Missing ApiUri in configuration");
                return 2;
            }

            // prompt for userID/password if required
            LoginCredentials credentials = new LoginCredentials(_user, _password);
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
                Delimiter = _metaSep
            };

            // setup client
            HttpClient client = ClientHelper.GetClient(apiRootUri, login.Token);

            // process files
            int count = 0;
            foreach (string path in FileEnumerator.Enumerate(
                _inputDir, _fileMask, _regexMask, _recursive))
            {
                // skip metadata files
                if (Path.GetExtension(path) == _metaExt) continue;

                count++;
                _logger.LogInformation($"{count} {path}");
                ColorConsole.WriteEmbeddedColorLine($"[green]{count:0000}[/green] {path}");

                // load metadata if any
                string metaPath = BuildMetaPath(path);
                IList<Tuple<string, string>> metadata = null;
                if (File.Exists(metaPath)) metadata = metaReader.Read(metaPath);

                // add item
                string id = metadata?.FirstOrDefault(t => t.Item1 == "path")
                    ?.Item2 ?? path;
                HttpResponseMessage response = await client.PostAsJsonAsync(
                    "api/items", new { id });
                if (!response.IsSuccessStatusCode)
                {
                    string error = $"Error adding item {id}: {response.ReasonPhrase}";
                    _logger.LogError(error);
                    ColorConsole.WriteError(error);
                    return 2;
                }

                // set properties
                // set content

                await FileUploader.UploadFile(apiRootUri, path, login.Token);
                // TODO
            }

            return 0;
        }
    }
}
