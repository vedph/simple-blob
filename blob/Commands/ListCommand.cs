using Fusi.Tools.Data;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using SimpleBlob.Core;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    /// <summary>
    /// List BLOB items.
    /// </summary>
    /// <seealso cref="ICommand" />
    public sealed class ListCommand : ICommand
    {
        private readonly ListCommandOptions _options;
        private ApiLogin _login;

        public ListCommand(ListCommandOptions options)
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

            CommandHelper.AddCredentialsOptions(app);
            CommandHelper.AddItemListOptions(app);

            CommandOption fileOption = app.Option("--file|-f",
                "The path to the output file (if not set, the output will be displayed)",
                CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                CommandOption dateOption = app.Options.Find(o => o.ShortName == "n");
                CommandOption sizeOption = app.Options.Find(o => o.ShortName == "s");
                CommandOption propOption = app.Options.Find(o => o.ShortName == "o");

                ListCommandOptions co = new ListCommandOptions
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
                    OutputPath = fileOption.Value()
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

                options.Command = new ListCommand(co);

                return 0;
            });
        }

        private static void WritePage(TextWriter writer, DataPage<BlobItem> page)
        {
            writer.WriteLine($"--- {page.PageNumber}/{page.PageCount}");

            int n = (page.PageNumber - 1) * page.PageSize;
            foreach (BlobItem item in page.Items)
            {
                writer.WriteLine($"[{++n}]");
                writer.WriteLine($"  - ID: {item.Id}");
                writer.WriteLine($"  - User ID: {item.UserId}");
                writer.WriteLine($"  - Modified: {item.DateModified}");
                writer.WriteLine();
            }
        }

        public async Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("List Items");
            _options.Logger.LogInformation("---LIST ITEMS---");

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

            // get page
            var page = await client.GetFromJsonAsync<DataPage<BlobItem>>(
                "items?" + CommandHelper.BuildItemListQueryString(_options));

            // write page
            TextWriter writer;
            if (!string.IsNullOrEmpty(_options.OutputPath))
            {
                writer = new StreamWriter(new FileStream(_options.OutputPath,
                    FileMode.Create, FileAccess.Write, FileShare.Read),
                    Encoding.UTF8);
            }
            else writer = Console.Out;
            WritePage(writer, page);
            writer.Flush();

            return 0;
        }
    }

    public sealed class ListCommandOptions : ItemListOptions
    {
        public string OutputPath { get; set; }
    }
}
