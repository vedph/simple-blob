using Fusi.Cli;
using Fusi.Cli.Commands;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    public sealed class AddUserRolesCommand : ICommand
    {
        private readonly UserRolesCommandOptions _options;

        public AddUserRolesCommand(UserRolesCommandOptions options)
        {
            _options = options;
        }

        public static void Configure(CommandLineApplication app,
            ICliAppContext context)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            app.Description = "Add the specified roles to a user.";
            app.HelpOption("-?|-h|--help");

            CommandArgument nameArgument = app.Argument("[name]", "The user name");
            CommandOption roleOption = app.Option("--role|-r",
                "The role (repeatable)", CommandOptionType.MultipleValue);

            // credentials
            CommandHelper.AddCredentialsOptions(app);

            app.OnExecute(() =>
            {
                UserRolesCommandOptions co = new(context)
                {
                    UserName = nameArgument.Value,
                    Roles = roleOption.Values.ToArray()
                };
                // credentials
                CommandHelper.SetCredentialsOptions(app, co);

                context.Command = new AddUserRolesCommand(co);
                return 0;
            });
        }

        public async Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("Add User Roles");
            _options.Logger?.LogInformation("---ADD USER ROLES---");

            string? apiRootUri = CommandHelper.GetApiRootUriAndNotify(_options);
            if (apiRootUri == null) return 2;

            // prompt for userID/password if required
            LoginCredentials credentials = new(
                _options.UserId,
                _options.Password);
            credentials.PromptIfRequired();

            // login
            ApiLogin? login =
                await CommandHelper.LoginAndNotify(apiRootUri, credentials);
            if (login == null) return 2;

            // setup client
            using HttpClient client = ClientHelper.GetClient(apiRootUri,
                login.Token);

            Console.Write($"Adding roles to user {_options.UserName}... ");
            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"users/{_options.UserName}/roles", _options.Roles);

            if (!response.IsSuccessStatusCode)
            {
                string error = $"Error adding roles to user {_options.UserName}";
                _options.Logger?.LogError(error);
                ColorConsole.WriteError(error);
                return 2;
            }
            Console.WriteLine("done");

            return 0;
        }
    }

    public sealed class UserRolesCommandOptions : AppCommandOptions
    {
        public string? UserName { get; set; }
        public IList<string>? Roles { get; set; }

        public UserRolesCommandOptions(ICliAppContext context) : base(context)
        {
        }
    }
}
