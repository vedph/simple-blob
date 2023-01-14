using Fusi.Tools.Data;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using SimpleBlob.Core;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands;

internal sealed class ListCommand : AsyncCommand<ListCommandSettings>
{
    private static void WriteRichPage(DataPage<BlobItem> page)
    {
        Table table = new();
        table.AddColumns("ID", "user", "modified");

        foreach (BlobItem item in page.Items)
        {
            table.AddRow(new Markup($"[cyan]{item.Id}[/]"),
                new Markup(item.UserId),
                new Markup(item.DateModified.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        AnsiConsole.Write(table);
    }

    private static void WritePlainPage(DataPage<BlobItem> page, TextWriter writer)
    {
        writer.WriteLine($"--- {page.PageNumber}/{page.PageCount}");

        int n = (page.PageNumber - 1) * page.PageSize;
        foreach (BlobItem item in page.Items)
        {
            writer.WriteLine($"[{++n}]");
            writer.WriteLine($"  - ID: {item.Id}");
            writer.WriteLine($"  - User ID: {item.UserId}");
            writer.WriteLine($"  - Modified: {item.DateModified}");
            writer.WriteLine();
        }
    }

    public override async Task<int> ExecuteAsync(CommandContext context,
        ListCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]LIST ITEMS[/]");
        CliAppContext.Logger?.LogInformation("---LIST ITEMS---");

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

        // get page
        DataPage<BlobItem>? page = null;
        await AnsiConsole.Status().Start("Fetching data...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);

            page = await client.GetFromJsonAsync<DataPage<BlobItem>>(
                "items?" + CommandHelper.BuildItemListQueryString(settings))!;
        });

        // write page
        TextWriter writer;
        if (string.IsNullOrEmpty(settings.OutputPath))
        {
            // console
            WriteRichPage(page!);
        }
        else
        {
            // file
            writer = new StreamWriter(new FileStream(settings.OutputPath,
                FileMode.Create, FileAccess.Write, FileShare.Read),
                Encoding.UTF8);
            WritePlainPage(page!, writer);
            writer.Flush();
        }

        return 0;
    }
}

internal class ListCommandSettings : ItemListSettings
{
    [CommandOption("-f|--file <FILE_PATH>")]
    [Description("The path to the output file (if not set, the output will be displayed)")]
    public string? OutputPath { get; set; }
}
