using Fusi.Cli;
using Fusi.Cli.Commands;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands;

public sealed class DeleteUserCommand : ICommand
{
    private readonly DeleteUserCommandOptions _options;

    public DeleteUserCommand(DeleteUserCommandOptions options)
    {
        _options = options;
    }

    public static void Configure(CommandLineApplication app,
        ICliAppContext context)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));

        app.Description = "Delete the specified user.";
        app.HelpOption("-?|-h|--help");

        CommandArgument nameArgument = app.Argument("[name]", "The user name");
        CommandOption confirmOption = app.Option("--confirm|-c",
            "Confirm the operation without prompt",
            CommandOptionType.NoValue);
        CommandHelper.AddCredentialsOptions(app);

        app.OnExecute(() =>
        {
            DeleteUserCommandOptions co = new(context)
            {
                UserName = nameArgument.Value,
                IsConfirmed = confirmOption.HasValue()
            };
            // credentials
            CommandHelper.SetCredentialsOptions(app, co);

            context.Command = new DeleteUserCommand(co);

            return 0;
        });
    }

    public async Task<int> Run()
    {
        ColorConsole.WriteWrappedHeader("Delete User");
        _options.Logger?.LogInformation("---DELETE USER---");

        string? apiRootUri = CommandHelper.GetApiRootUriAndNotify(_options);
        if (apiRootUri == null) return 2;

        // prompt for userID/password if required
        LoginCredentials credentials = new(
            _options.UserId,
            _options.Password);
        credentials.PromptIfRequired();

        // login
        ApiLogin? login = await CommandHelper.LoginAndNotify(apiRootUri, credentials);
        if (login == null) return 2;

        // prompt for confirmation if required
        if (!_options.IsConfirmed &&
            !Prompt.ForBool("Delete user? ", false))
        {
            return 0;
        }

        // setup client
        using HttpClient client = ClientHelper.GetClient(apiRootUri,
            login.Token);

        // delete
        HttpResponseMessage response =
            await client.DeleteAsync($"accounts/{_options.UserName}");

        if (!response.IsSuccessStatusCode)
        {
            string error = "Error deleting " + _options.UserName;
            _options.Logger?.LogError(error);
            ColorConsole.WriteError(error);
            return 2;
        }
        else ColorConsole.WriteSuccess("Deleted user " + _options.UserName);

        return 0;
    }
}

public sealed class DeleteUserCommandOptions : AppCommandOptions
{
    public string? UserName { get; set; }
    public bool IsConfirmed { get; set; }

    public DeleteUserCommandOptions(ICliAppContext context) : base(context)
    {
    }
}
