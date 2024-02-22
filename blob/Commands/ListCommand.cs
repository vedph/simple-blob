using Fusi.Tools.Data;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using SimpleBlob.Core;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Collections.Specialized;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Fusi.Cli.Auth.Services;
using System.Linq;

namespace SimpleBlob.Cli.Commands;

internal sealed class ListCommand : AsyncCommand<ListCommandSettings>
{
    private readonly ICliAuthSettings _settings;

    public ListCommand(ICliAuthSettings settings)
    {
        _settings = settings
            ?? throw new ArgumentNullException(nameof(settings));
    }

    public static string BuildItemListQueryString(ItemListSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // https://stackoverflow.com/questions/17096201/build-query-string-for-system-net-httpclient-get
        NameValueCollection query = HttpUtility.ParseQueryString("");
        query["PageNumber"] = settings.PageNumber.ToString();
        query["PageSize"] = settings.PageSize.ToString();

        if (!string.IsNullOrEmpty(settings.Id)) query["Id"] = settings.Id;

        if (!string.IsNullOrEmpty(settings.MimeType))
            query["MimeType"] = settings.Id;

        if (settings.MinDateModified != null)
        {
            query["MinDateModified"] = settings.MinDateModified
                .Value.ToString("yyyy-MM-dd");
        }
        if (settings.MaxDateModified != null)
        {
            query["MaxDateModified"] = settings.MaxDateModified
                .Value.ToString("yyyy-MM-dd");
        }

        if (settings.MinSize > 0) query["MinSize"] = settings.MinSize.ToString();
        if (settings.MaxSize > 0) query["MaxSize"] = settings.MaxSize.ToString();

        if (!string.IsNullOrEmpty(settings.LastUserId))
            query["UserId"] = settings.LastUserId;

        if (!string.IsNullOrEmpty(settings.Properties))
            query["Properties"] = settings.Properties;

        return query.ToString() ?? "";
    }

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

    private static async Task<int> ListPageItems(HttpClient client,
        ListCommandSettings settings)
    {
        // get page
        DataPage<BlobItem>? page = null;
        await AnsiConsole.Status().Start("Fetching data...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);

            page = await client.GetFromJsonAsync<DataPage<BlobItem>>(
                "items?" + BuildItemListQueryString(settings))!;
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

        return page?.Total ?? 0;
    }

    private static async Task ListItemIds(HttpClient client,
        ListCommandSettings settings)
    {
        // get page
        DataPage<BlobItem>? page = null;
        await AnsiConsole.Status().Start("Fetching data...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);
            StreamWriter? writer = string.IsNullOrEmpty(settings.OutputPath)
                ? null
                : new StreamWriter(new FileStream(settings.OutputPath,
                    FileMode.Create, FileAccess.Write, FileShare.Read),
                    Encoding.UTF8);

            while (true)
            {
                page = await client.GetFromJsonAsync<DataPage<BlobItem>>(
                    "items?" + BuildItemListQueryString(settings));
                if (page == null) break;

                foreach (string id in page.Items.Select(item => item.Id))
                {
                    if (writer == null)
                        AnsiConsole.MarkupLine($"[cyan]{id}[/]");
                    else
                        writer.WriteLine(id);
                }

                if (++settings.PageNumber > page!.PageCount) break;
            }
            writer?.Flush();
        });
    }

    public override async Task<int> ExecuteAsync(CommandContext context,
        ListCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]LIST ITEMS[/]");
        CliAppContext.Logger?.LogInformation("---LIST ITEMS---");

        // prompt for userID/password if required
        LoginCredentials credentials = new(
            settings.User,
            settings.Password);
        credentials.PromptIfRequired();

        try
        {
            // login
            ApiLogin? login = await CommandHelper.LoginAndNotify(
                _settings.ApiRootUri, credentials);
            if (login == null) return 2;

            // setup client
            using HttpClient client = CommandHelper.GetClient(_settings.ApiRootUri,
                login.Token);

            if (settings.IdOnly)
            {
                await ListItemIds(client, settings);
            }
            else
            {
                int total = await ListPageItems(client, settings);
                AnsiConsole.MarkupLine(
                    $"[cyan]{settings.PageNumber}[/]×[cyan]{settings.PageSize}[/] " +
                    $"| total: [cyan]{total}[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            CliHelper.ShowError(ex);
            return 2;
        }
    }
}

internal class ListCommandSettings : ItemListSettings
{
    [CommandOption("-f|--file <FILE_PATH>")]
    [Description("The path to the output file (if not set, the output will be displayed)")]
    public string? OutputPath { get; set; }

    [CommandOption("-r|--raw")]
    [Description("List item IDs only from all the pages")]
    public bool IdOnly { get; set; }
}
