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

            // credentials
            CommandHelper.AddCredentialsOptions(app);
            // items list
            CommandHelper.AddItemListOptions(app);

            CommandOption fileOption = app.Option("--file|-f",
                "The path to the output file (if not set, the output will be displayed)",
                CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                ListCommandOptions co = new ListCommandOptions
                {
                    Configuration = options.Configuration,
                    Logger = options.Logger,
                    OutputPath = fileOption.Value()
                };
                // credentials
                CommandHelper.SetCredentialsOptions(app, co);
                // items list
                CommandHelper.SetItemListOptions(app, co);

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
