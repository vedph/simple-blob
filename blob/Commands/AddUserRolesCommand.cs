using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands;

internal sealed class AddUserRolesCommand : AsyncCommand<UserRolesCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context,
        UserRolesCommandSettings settings)
    {
        if (settings.Roles.Length == 0) return 0;

        AnsiConsole.MarkupLine("[yellow underline]ADD USER ROLES[/]");
        CliAppContext.Logger?.LogInformation("---ADD USER ROLES---");

        string? apiRootUri = CommandHelper.GetApiRootUriAndNotify();
        if (apiRootUri == null) return 2;

        // prompt for userID/password if required
        LoginCredentials credentials = new(
            settings.User,
            settings.Password);
        credentials.PromptIfRequired();

        // login
        ApiLogin? login =
            await CommandHelper.LoginAndNotify(apiRootUri, credentials);
        if (login == null) return 2;

        // setup client
        using HttpClient client = ClientHelper.GetClient(apiRootUri,
            login.Token);

        await AnsiConsole.Status().Start("Adding roles...", async ctx =>
        {
            ctx.Status(settings.UserName!);
            ctx.Spinner(Spinner.Known.Star);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"users/{settings.UserName}/roles", settings.Roles);
            response.EnsureSuccessStatusCode();
        });

        AnsiConsole.MarkupLine($"[green] Added {settings.Roles.Length} role(s) " +
            $"to user {settings.UserName}[/]");

        return 0;
    }
}

internal sealed class UserRolesCommandSettings : AuthCommandSettings
{
    [CommandArgument(0, "<USER_NAME>")]
    [Description("The name of the user to add roles to")]
    public string? UserName { get; set; }

    [CommandArgument(1, "<USER_ROLE>")]
    [Description("The user role(s) (repeatable)")]
    public string[] Roles { get; set; }

    public UserRolesCommandSettings()
    {
        Roles = Array.Empty<string>();
    }
}
