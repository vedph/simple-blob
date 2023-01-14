using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Collections.Specialized;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SimpleBlob.Cli.Commands;

internal sealed class DeleteUserRolesCommand : AsyncCommand<UserRolesCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context,
        UserRolesCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[red underline]DELETE USER ROLES[/]");
        CliAppContext.Logger?.LogInformation("---DELETE USER ROLES---");

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

        // https://stackoverflow.com/questions/17096201/build-query-string-for-system-net-httpclient-get
        NameValueCollection query = HttpUtility.ParseQueryString("");
        query["roles"] = string.Join(",", settings.Roles);

        await AnsiConsole.Status().Start("Deleting roles...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);

            HttpResponseMessage response = await client.DeleteAsync(
                $"users/{settings.UserName}/roles?" + query.ToString());
            response.EnsureSuccessStatusCode();
        });

        AnsiConsole.MarkupLine($"[green] Removed {settings.Roles.Length} role(s) " +
            $"from user {settings.UserName}.[/]");

        return 0;
    }
}
