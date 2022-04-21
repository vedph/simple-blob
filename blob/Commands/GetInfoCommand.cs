using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using SimpleBlob.Api.Models;
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
    public sealed class GetInfoCommand : ICommand
    {
        private readonly GetInfoCommandOptions _options;
        private ApiLogin _login;

        public GetInfoCommand(GetInfoCommandOptions options)
        {
            _options = options;
        }

        public static void Configure(CommandLineApplication app,
            AppOptions options)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            app.Description = "Get information about the specified BLOB item.";
            app.HelpOption("-?|-h|--help");

            CommandArgument idArgument = app.Argument("[id]", "The BLOB item's ID");

            // credentials
            CommandHelper.AddCredentialsOptions(app);

            CommandOption fileOption = app.Option("--file|-f",
                "The path to the output file (if not set, the output will be displayed)",
                CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                GetInfoCommandOptions co = new()
                {
                    Configuration = options.Configuration,
                    Logger = options.Logger,
                    Id = idArgument.Value,
                    OutputPath = fileOption.Value()
                };
                // credentials
                CommandHelper.SetCredentialsOptions(app, co);

                options.Command = new GetInfoCommand(co);

                return 0;
            });
        }

        private static void WriteItemInfo(BlobItem item,
            BlobItemProperty[] properties,
            BlobItemContentMetaModel contentMeta,
            TextWriter writer)
        {
            writer.WriteLine(item.ToString());

            if (properties.Length > 0)
            {
                writer.WriteLine("  - properties:");
                int n = 0;
                foreach (BlobItemProperty p in properties)
                    writer.WriteLine($"  {++n}. {p.Name}={p.Value}");
            }

            if (contentMeta != null)
            {
                writer.WriteLine("  - content:");
                writer.WriteLine($"    - MIME type: {contentMeta.MimeType}");
                writer.WriteLine($"    - size: {contentMeta.Size}");
                writer.WriteLine($"    - hash: {contentMeta.Hash}");
                writer.WriteLine($"    - user: {contentMeta.UserId}");
                writer.WriteLine($"    - modified: {contentMeta.DateModified}");
            }
        }

        public async Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("Get Item Info");
            _options.Logger.LogInformation("---GET ITEM INFO---");

            string apiRootUri = CommandHelper.GetApiRootUriAndNotify(_options);
            if (apiRootUri == null) return 2;

            // prompt for userID/password if required
            LoginCredentials credentials = new(
                _options.UserId,
                _options.Password);
            credentials.PromptIfRequired();

            // login
            _login = await CommandHelper.LoginAndNotify(apiRootUri, credentials);

            // setup client
            using HttpClient client = ClientHelper.GetClient(apiRootUri,
                _login.Token);

            // load item
            BlobItem item = await client.GetFromJsonAsync<BlobItem>(
                $"items/{_options.Id}");

            // load its properties
            BlobItemProperty[] props =
                await client.GetFromJsonAsync<BlobItemProperty[]>
                ($"properties/{item.Id}");

            // load its content metadata
            BlobItemContentMetaModel contentMeta =
                await client.GetFromJsonAsync<BlobItemContentMetaModel>(
                    $"contents/{item.Id}/meta");

            TextWriter writer;
            if (!string.IsNullOrEmpty(_options.OutputPath))
            {
                writer = new StreamWriter(new FileStream(_options.OutputPath,
                    FileMode.Create, FileAccess.Write, FileShare.Read),
                    Encoding.UTF8);
            }
            else writer = Console.Out;

            WriteItemInfo(item, props, contentMeta, writer);
            writer.Flush();

            return 0;
        }
    }

    public sealed class GetInfoCommandOptions : CommandOptions
    {
        public string Id { get; set; }
        public string OutputPath { get; set; }
    }
}
