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

            CommandHelper.AddCredentialsOptions(app);
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
                CommandOption dateOption = app.Options.Find(o => o.ShortName == "n");
                CommandOption sizeOption = app.Options.Find(o => o.ShortName == "s");
                CommandOption propOption = app.Options.Find(o => o.ShortName == "o");

                DownloadCommandOptions co = new DownloadCommandOptions
                {
                    Configuration = options.Configuration,
                    Logger = options.Logger,
                    PageNumber = CommandHelper.GetOptionValue(
                        app.Options.Find(o => o.ShortName == "n"), 1),
                    PageSize = CommandHelper.GetOptionValue(
                        app.Options.Find(o => o.ShortName == "z"), 20),
                    Id = app.Options.Find(o => o.ShortName == "i").Value(),
                    MimeType = app.Options.Find(o => o.ShortName == "m").Value(),
                    LastUserId = app.Options.Find(o => o.ShortName == "l").Value(),
                    Properties = propOption.Values.Count > 0
                        ? string.Join(",", propOption.Values)
                        : null,
                    OutputDir = outputDirArgument.Value,
                    MetaExtension = metaExtOption.HasValue()
                        ? metaExtOption.Value() : ".meta",
                    IdDelimiter = idDelimOption.HasValue()
                        ? idDelimOption.Value() : "|",
                    MetaDelimiter = metaDelimOption.HasValue()
                        ? metaDelimOption.Value() : ","
                };
                CommandHelper.SetCredentialsOptions(app, co);

                Regex rngRegex = new Regex("^(?<a>[^:]+)?:(?<b>.+)?");

                // dates
                if (dateOption.HasValue())
                {
                    Match m = rngRegex.Match(dateOption.Value());
                    if (m.Success)
                    {
                        co.MinDateModified = CommandHelper.ParseDate(
                            m.Groups["a"].Value);
                        co.MaxDateModified = CommandHelper.ParseDate(
                            m.Groups["b"].Value);
                    }
                }

                // sizes
                if (sizeOption.HasValue())
                {
                    Match m = rngRegex.Match(dateOption.Value());
                    if (m.Success)
                    {
                        co.MinSize = long.TryParse(m.Groups["a"].Value,
                            out long min) ? min : 0;
                        co.MaxSize = long.TryParse(m.Groups["b"].Value,
                            out long max) ? max : 0;
                    }
                }

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
            _login = CommandHelper.LoginAndNotify(apiRootUri, credentials);

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
                        await client.GetAsync("contents/{id}");
                    using Stream input = await response.Content.ReadAsStreamAsync();

                    // save to file
                    string file = item.Id.Replace(_options.IdDelimiter,
                        new string(Path.DirectorySeparatorChar, 1));
                    string dir = Path.GetDirectoryName(file);
                    string path = Path.Combine(dir, file);
                    Console.WriteLine(" => " + path);

                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    using FileStream output = new FileStream(path, FileMode.Create,
                        FileAccess.Write, FileShare.Read);
                    input.CopyTo(output);
                    output.Flush();

                    // load metadata
                    List<Tuple<string, string>> metadata =
                        new List<Tuple<string, string>>();
                    metadata.Add(Tuple.Create("id", item.Id));

                    var props = await client.GetFromJsonAsync<BlobItemProperty[]>(
                        $"properties/{item.Id}");
                    foreach (var p in props)
                        metadata.Add(Tuple.Create(p.Name, p.Value));

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
