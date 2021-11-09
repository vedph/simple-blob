using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    public sealed class DeleteItemCommand : ICommand
    {
        private readonly DeleteItemCommandOptions _options;
        private ApiLogin _login;

        public DeleteItemCommand(DeleteItemCommandOptions options)
        {
            _options = options;
        }

        public static void Configure(CommandLineApplication app,
            AppOptions options)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            app.Description = "Delete the specified BLOB item.";
            app.HelpOption("-?|-h|--help");

            CommandArgument idArgument = app.Argument("[id]", "The BLOB item's ID");

            // credentials
            CommandHelper.AddCredentialsOptions(app);

            CommandOption confirmOption = app.Option("--confirm|-c",
                "Confirm the operation without prompt",
                CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                DeleteItemCommandOptions co = new DeleteItemCommandOptions
                {
                    Configuration = options.Configuration,
                    Logger = options.Logger,
                    Id = idArgument.Value,
                    IsConfirmed = confirmOption.HasValue()
                };
                // credentials
                CommandHelper.SetCredentialsOptions(app, co);

                options.Command = new DeleteItemCommand(co);

                return 0;
            });
        }

        public async Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("Delete Item");
            _options.Logger.LogInformation("---DELETE ITEM---");

            string apiRootUri = CommandHelper.GetApiRootUriAndNotify(_options);
            if (apiRootUri == null) return 2;

            // prompt for userID/password if required
            LoginCredentials credentials = new LoginCredentials(
                _options.UserId,
                _options.Password);
            credentials.PromptIfRequired();

            // login
            _login = await CommandHelper.LoginAndNotify(apiRootUri, credentials);

            // prompt for confirmation if required
            if (!_options.IsConfirmed &&
                !Prompt.ForBool("Delete item? ", false))
            {
                return 0;
            }

            // setup client
            using HttpClient client = ClientHelper.GetClient(apiRootUri,
                _login.Token);

            // delete
            HttpResponseMessage response =
                await client.DeleteAsync($"items/{_options.Id}");

            if (!response.IsSuccessStatusCode)
            {
                string error = "Error deleting " + _options.Id;
                _options.Logger?.LogError(error);
                ColorConsole.WriteError(error);
                return 2;
            }
            else ColorConsole.WriteSuccess("Deleted item " + _options.Id);

            return 0;
        }
    }

    public sealed class DeleteItemCommandOptions : CommandOptions
    {
        public string Id { get; set; }
        public bool IsConfirmed { get; set; }
    }
}
