using Fusi.Api.Auth.Controllers;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands;

internal sealed class UpdateUserCommand : AsyncCommand<UpdateUserCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context,
        UpdateUserCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[yellow underline]UPDATE USER[/]");
        CliAppContext.Logger?.LogInformation("---UPDATE USER---");

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

        // setup client
        using HttpClient client = ClientHelper.GetClient(apiRootUri,
            login.Token);

        // get user
        return await AnsiConsole.Status().Start("Updating user...", async ctx =>
        {
            ctx.Status($"Getting user {settings.UserName}");
            ctx.Spinner(Spinner.Known.Star);

            NamedUserModel? user = await client.GetFromJsonAsync<NamedUserModel>(
                "users/" + settings.UserName);
            if (user == null)
            {
                AnsiConsole.MarkupLine(
                    $"[red]User not found: {settings.UserName}[/]");
                return 2;
            }
            ctx.Status($"Updating user {settings.UserName}... ");
            NamedUserBindingModel newUser = new()
            {
                UserName = settings.UserName,
                Email = settings.UserEmail ?? user.Email,
                EmailConfirmed = settings.EmailConfirmed ?? user.EmailConfirmed,
                FirstName = settings.FirstName ?? user.FirstName,
                LastName = settings.LastName ?? user.LastName,
                LockoutEnabled = settings.LockoutEnabled == -1
                    ? user.LockoutEnabled : settings.LockoutEnabled == 1
            };
            HttpResponseMessage response = await client.PutAsJsonAsync(
                "users", newUser);

            response.EnsureSuccessStatusCode();
            AnsiConsole.MarkupLine($"[green]User {settings.UserName} updated.[/]");
            return 0;
        });
    }
}

internal sealed class UpdateUserCommandSettings : AuthCommandSettings
{
    [CommandArgument(0, "<USER_NAME>")]
    [Description("The name of the user to update")]
    public string? UserName { get; set; }

    [CommandOption("-e|--email <EMAIL>")]
    [Description("The user's email address")]
    public string? UserEmail { get; set; }

    [CommandOption("-c|--conf")]
    [Description("Confirm user's email address")]
    public bool? EmailConfirmed { get; set; }

    [CommandOption("-f|--first <NAME>")]
    [Description("The user first name")]
    [DefaultValue("")]
    public string? FirstName { get; set; }

    [CommandOption("-l|--last <NAME>")]
    [Description("The user last name")]
    [DefaultValue("")]
    public string? LastName { get; set; }

    [CommandOption("-k|--lock <STATE>")]
    [Description("Enable (1) or disable (0) lockout")]
    [DefaultValue(-1)]
    public int LockoutEnabled { get; set; }

    public UpdateUserCommandSettings()
    {
        LockoutEnabled = -1;
    }
}
