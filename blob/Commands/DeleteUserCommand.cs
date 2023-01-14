using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands;

internal sealed class DeleteUserCommand : AsyncCommand<DeleteUserCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context,
        DeleteUserCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[red underline]DELETE USER[/]");
        CliAppContext.Logger?.LogInformation("---DELETE USER---");

        string? apiRootUri = CommandHelper.GetApiRootUriAndNotify();
        if (apiRootUri == null) return 2;

        // prompt for userID/password if required
        LoginCredentials credentials = new(
            settings.User,
            settings.Password);
        credentials.PromptIfRequired();

        // login
        ApiLogin? login = await CommandHelper.LoginAndNotify(apiRootUri, credentials);
        if (login == null) return 2;

        // prompt for confirmation if required
        if (!settings.IsConfirmed &&
            !AnsiConsole.Confirm($"Delete user {settings.UserName}? ", false))
        {
            return 0;
        }

        // setup client
        using HttpClient client = ClientHelper.GetClient(apiRootUri,
            login.Token);

        // delete
        await AnsiConsole.Status().Start("Deleting user...", async ctx =>
        {
            ctx.Status(settings.UserName!);
            ctx.Spinner(Spinner.Known.Star);

            HttpResponseMessage response =
                await client.DeleteAsync($"accounts/{settings.UserName}");
            response.EnsureSuccessStatusCode();
        });

        AnsiConsole.MarkupLine($"[green]Deleted user {settings.UserName}[/]");

        return 0;
    }
}

internal sealed class DeleteUserCommandSettings : AuthCommandSettings
{
    [CommandArgument(0, "<USER_NAME>")]
    [Description("The name of the user to delete")]
    public string? UserName { get; set; }

    [CommandOption("-c|-y|--yes")]
    [Description("Automatically confirm operation (no prompt)")]
    public bool IsConfirmed { get; set; }
}
