using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands;

internal sealed class DeleteItemCommand : AsyncCommand<DeleteItemCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context,
        DeleteItemCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[red underline]DELETE ITEM[/]");
        CliAppContext.Logger?.LogInformation("---DELETE ITEM---");

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
            !AnsiConsole.Confirm($"Delete item {settings.Id}? ", false))
        {
            return 0;
        }

        // setup client
        using HttpClient client = ClientHelper.GetClient(apiRootUri,
            login.Token);

        // delete
        await AnsiConsole.Status().Start("Deleting item...", async ctx =>
        {
            ctx.Status(settings.Id!);
            ctx.Spinner(Spinner.Known.Star);

            HttpResponseMessage response =
                await client.DeleteAsync($"items/{settings.Id}");
            response.EnsureSuccessStatusCode();
        });

        AnsiConsole.MarkupLine($"[green]Deleted item {settings.Id}[/]");

        return 0;
    }
}

internal sealed class DeleteItemCommandSettings : AuthCommandSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("The ID of the item to delete")]
    public string? Id { get; set; }

    [CommandOption("-c|-y|--yes")]
    [Description("Automatically confirm operation (no prompt)")]
    public bool IsConfirmed { get; set; }
}
