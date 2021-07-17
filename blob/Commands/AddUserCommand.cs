using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using SimpleBlobApi.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    public sealed class AddUserCommand : ICommand
    {
        private readonly AddUserCommandOptions _options;
        private ApiLogin _login;

        public AddUserCommand(AddUserCommandOptions options)
        {
            _options = options;
        }

        public static void Configure(CommandLineApplication app,
            AppOptions options)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            app.Description = "Add a new user.";
            app.HelpOption("-?|-h|--help");

            CommandArgument nameArgument = app.Argument("[name]", "The user name");
            CommandArgument pwdArgument = app.Argument("[password]",
                "The user password");
            CommandArgument emailArgument = app.Argument("[email]", "The user email");
            CommandOption firstOption = app.Option("--first|-f",
                "The user first name", CommandOptionType.SingleValue);
            CommandOption lastOption = app.Option("--last|-l",
                "The user last name", CommandOptionType.SingleValue);

            // credentials
            CommandHelper.AddCredentialsOptions(app);

            app.OnExecute(() =>
            {
                AddUserCommandOptions co = new AddUserCommandOptions
                {
                    Configuration = options.Configuration,
                    Logger = options.Logger,
                    UserName = nameArgument.Value,
                    UserPassword = pwdArgument.Value,
                    UserEmail = emailArgument.Value,
                    FirstName = firstOption.Value() ?? "",
                    LastName = lastOption.Value() ?? ""
                };
                // credentials
                CommandHelper.SetCredentialsOptions(app, co);

                options.Command = new AddUserCommand(co);
                return 0;
            });
        }

        public async Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("Add User");
            _options.Logger.LogInformation("---ADD USER---");

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

            Console.Write($"Adding user {_options.UserName}... ");
            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"accounts/register", new RegisterBindingModel
                {
                    Name = _options.UserName,
                    Password = _options.UserPassword,
                    Email = _options.UserEmail,
                    FirstName = _options.FirstName,
                    LastName = _options.LastName
                });

            if (!response.IsSuccessStatusCode)
            {
                string error = $"Error adding user {_options.UserName}";
                _options.Logger?.LogError(error);
                ColorConsole.WriteError(error);
                return 2;
            }
            Console.WriteLine("done");
            return 0;
        }
    }

    public sealed class AddUserCommandOptions : CommandOptions
    {
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public string UserEmail { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
