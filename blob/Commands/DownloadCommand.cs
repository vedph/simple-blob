using Fusi.Tools.Data;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using SimpleBlob.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    public sealed class DownloadCommand : ICommand
    {
        private readonly DownloadCommandOptions _options;
        private ApiLogin _login;

        public DownloadCommand(DownloadCommandOptions options)
        {
            _options = options;
        }

        public static void Configure(CommandLineApplication app,
            AppOptions options)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            app.Description = "List the BLOB items matching " +
                "the specified filters.";
            app.HelpOption("-?|-h|--help");

            CommandArgument outputDirArgument = app.Argument("[output-dir]",
                "The output directory");

            // credentials
            CommandHelper.AddCredentialsOptions(app);
            // items list
            CommandHelper.AddItemListOptions(app);

            CommandOption metaExtOption = app.Option("--meta|-e",
                "The extension appended to the content filename " +
                "to represent its metadata in a correspondent file",
                CommandOptionType.SingleValue);
            CommandOption metaDelimOption = app.Option("--meta-sep",
                "The separator used in delimited metadata files",
                CommandOptionType.SingleValue);

            CommandOption idDelimOption = app.Option("--id-sep",
                "The conventional separator used in BLOB IDs",
                CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                DownloadCommandOptions co = new DownloadCommandOptions
                {
                    Configuration = options.Configuration,
                    Logger = options.Logger,
                    OutputDir = outputDirArgument.Value,
                    MetaExtension = metaExtOption.HasValue()
                        ? metaExtOption.Value() : ".meta",
                    IdDelimiter = idDelimOption.HasValue()
                        ? idDelimOption.Value() : "|",
                    MetaDelimiter = metaDelimOption.HasValue()
                        ? metaDelimOption.Value() : ","
                };
                // credentials
                CommandHelper.SetCredentialsOptions(app, co);
                // items list
                CommandHelper.SetItemListOptions(app, co);

                options.Command = new DownloadCommand(co);

                return 0;
            });
        }

        public async Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("Download Items");
            _options.Logger.LogInformation("---DOWNLOAD ITEMS---");

            string apiRootUri = CommandHelper.GetApiRootUriAndNotify(_options);
            if (apiRootUri == null) return 2;

            // prompt for userID/password if required
            LoginCredentials credentials = new LoginCredentials(
                _options.UserId,
                _options.Password);
            credentials.PromptIfRequired();

            // login
            _login = await CommandHelper.LoginAndNotify(apiRootUri, credentials);

            // setup client
            using HttpClient client = ClientHelper.GetClient(apiRootUri,
                _login.Token);

            // get 1st page
            var page = await client.GetFromJsonAsync<DataPage<BlobItem>>(
                "items?" + CommandHelper.BuildItemListQueryString(_options));

            ColorConsole.WriteInfo("Items to download: " + page.Total);
            if (page.Total == 0) return 0;

            if (!Directory.Exists(_options.OutputDir))
                Directory.CreateDirectory(_options.OutputDir);

            int itemNr = 0;
            while (true)
            {
                ColorConsole.WriteInfo($"Page {_options.PageNumber} of {page.PageCount}");

                foreach (BlobItem item in page.Items)
                {
                    itemNr++;
                    _options.Logger?.LogInformation($"{itemNr} {item}");
                    ColorConsole.WriteEmbeddedColorLine(
                        $"[green]{itemNr}[/green] {item}");

                    // get stream
                    HttpResponseMessage response =
                        await client.GetAsync($"contents/{item.Id}");
                    using Stream input = await response.Content.ReadAsStreamAsync();

                    // save to file
                    string itemPath = item.Id.Replace(_options.IdDelimiter,
                        new string(Path.DirectorySeparatorChar, 1));
                    string dir = Path.Combine(_options.OutputDir,
                        Path.GetDirectoryName(itemPath));
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    string path = Path.Combine(dir, Path.GetFileName(itemPath));
                    Console.WriteLine(" => " + path);

                    using FileStream output = new FileStream(path, FileMode.Create,
                        FileAccess.Write, FileShare.Read);
                    input.CopyTo(output);
                    output.Flush();

                    // load metadata
                    List<Tuple<string, string>> metadata =
                        new List<Tuple<string, string>>();

                    var props = await client.GetFromJsonAsync<BlobItemProperty[]>(
                        $"properties/{item.Id}");
                    bool hasId = false;
                    foreach (var p in props)
                    {
                        if (p.Name == "id") hasId = true;
                        metadata.Add(Tuple.Create(p.Name, p.Value));
                    }
                    if (!hasId) metadata.Add(Tuple.Create("id", item.Id));

                    // save metadata to path
                    path += _options.MetaExtension;
                    Console.WriteLine(" => " + path);
                    CsvMetadataFile metaFile = new CsvMetadataFile
                    {
                        Delimiter = _options.MetaDelimiter
                    };
                    metaFile.Write(metadata, path);
                }

                // next page
                if (++_options.PageNumber > page.PageCount) break;
                page = await client.GetFromJsonAsync<DataPage<BlobItem>>(
                    "items?" + CommandHelper.BuildItemListQueryString(_options));
            }

            return 0;
        }
    }

    public sealed class DownloadCommandOptions : ItemListOptions
    {
        public string OutputDir { get; set; }
        public string MetaExtension { get; set; }
        public string MetaDelimiter { get; set; }
        public string IdDelimiter { get; set; }
    }
}
