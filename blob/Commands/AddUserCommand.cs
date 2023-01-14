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

internal sealed class AddUserCommand : AsyncCommand<AddUserCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context,
        AddUserCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[yellow underline]ADD USER[/]");
        CliAppContext.Logger?.LogInformation("---ADD USER---");

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

        await AnsiConsole.Status().Start("Adding user...", async ctx =>
        {
            ctx.Status(settings.UserName!);
            ctx.Spinner(Spinner.Known.Star);
            HttpResponseMessage response = await client.PostAsJsonAsync(
                "accounts/register?confirmed=true", new NamedRegisterBindingModel
            {
                Name = settings.UserName,
                Password = settings.UserPassword,
                Email = settings.UserEmail,
                FirstName = settings.FirstName,
                LastName = settings.LastName
            });
            response.EnsureSuccessStatusCode();
        });

        AnsiConsole.MarkupLine($"[green] Added user {settings.UserName}.[/]");

        return 0;
    }
}

internal sealed class AddUserCommandSettings : AuthCommandSettings
{
    [CommandArgument(0, "<USER_NAME>")]
    [Description("The user name")]
    public string? UserName { get; set; }

    [CommandArgument(1, "<USER_PWD>")]
    [Description("The user password")]
    public string? UserPassword { get; set; }

    [CommandArgument(2, "<USER_EMAIL>")]
    [Description("The user email address")]
    public string? UserEmail { get; set; }

    [CommandOption("-f|--first <NAME>")]
    [Description("The user first name")]
    [DefaultValue("")]
    public string FirstName { get; set; }

    [CommandOption("-l|--last <NAME>")]
    [Description("The user last name")]
    [DefaultValue("")]
    public string LastName { get; set; }

    public AddUserCommandSettings()
    {
        FirstName = LastName = "";
    }
}
