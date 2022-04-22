using Fusi.Api.Auth.Controllers;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    public sealed class UpdateUserCommand : ICommand
    {
        private readonly UpdateUserCommandOptions _options;

        public UpdateUserCommand(UpdateUserCommandOptions options)
        {
            _options = options;
        }

        public static void Configure(CommandLineApplication app,
            AppOptions options)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            app.Description = "Update user editable data.";
            app.HelpOption("-?|-h|--help");

            CommandArgument nameArgument = app.Argument("[name]", "The user name");

            CommandOption emailOption = app.Option("--email|-e",
                "The user's email address", CommandOptionType.SingleValue);
            CommandOption emailConfOption = app.Option("--conf|-c",
                "Confirm user's email address", CommandOptionType.NoValue);
            CommandOption lockoutOption = app.Option("--lock|-k",
                "Enable (1) or disable (0) lockout", CommandOptionType.SingleValue);
            CommandOption firstOption = app.Option("--first|-f",
                "The user first name", CommandOptionType.SingleValue);
            CommandOption lastOption = app.Option("--last|-l",
                "The user last name", CommandOptionType.SingleValue);

            // credentials
            CommandHelper.AddCredentialsOptions(app);

            app.OnExecute(() =>
            {
                UpdateUserCommandOptions co = new()
                {
                    Configuration = options.Configuration,
                    Logger = options.Logger,
                    UserName = nameArgument.Value,
                    UserEmail = emailOption.Value(),
                    EmailConfirmed = emailConfOption.HasValue()? true : null,
                    FirstName = firstOption.Value(),
                    LastName = lastOption.Value(),
                    LockoutEnabled = lockoutOption.HasValue()
                        ? lockoutOption.Value() == "1" : null,
                };
                // credentials
                CommandHelper.SetCredentialsOptions(app, co);

                options.Command = new UpdateUserCommand(co);
                return 0;
            });
        }

        public async Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("Update User");
            _options.Logger.LogInformation("---UPDATE USER---");

            string apiRootUri = CommandHelper.GetApiRootUriAndNotify(_options);
            if (apiRootUri == null) return 2;

            // prompt for userID/password if required
            LoginCredentials credentials = new(
                _options.UserId,
                _options.Password);
            credentials.PromptIfRequired();

            // login
            ApiLogin login = await CommandHelper.LoginAndNotify(apiRootUri, credentials);

            // setup client
            using HttpClient client = ClientHelper.GetClient(apiRootUri,
                login.Token);

            // get user
            Console.WriteLine($"Getting user {_options.UserName}...");
            NamedUserModel user = await client.GetFromJsonAsync<NamedUserModel>(
                "users/" + _options.UserName);

            Console.Write($"Updating user {_options.UserName}... ");
            NamedUserBindingModel newUser = new()
            {
                UserName = _options.UserName,
                Email = _options.UserEmail ?? user.Email,
                EmailConfirmed = _options.EmailConfirmed ?? user.EmailConfirmed,
                FirstName = _options.FirstName ?? user.FirstName,
                LastName = _options.LastName ?? user.LastName,
                LockoutEnabled = _options.LockoutEnabled ?? user.LockoutEnabled,
            };
            HttpResponseMessage response = await client.PutAsJsonAsync(
                "users", newUser);

            if (!response.IsSuccessStatusCode)
            {
                string error = $"Error updating user {_options.UserName}";
                _options.Logger?.LogError(error);
                ColorConsole.WriteError(error);
                return 2;
            }
            Console.WriteLine("done");
            return 0;
        }
    }

    public sealed class UpdateUserCommandOptions : CommandOptions
    {
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public bool? EmailConfirmed { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? LockoutEnabled { get; set; }
    }
}
