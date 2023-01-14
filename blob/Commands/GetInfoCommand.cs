using CsvHelper;
using Microsoft.Extensions.Logging;
using SimpleBlob.Api.Models;
using SimpleBlob.Cli.Services;
using SimpleBlob.Core;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands;

internal sealed class GetInfoCommand : AsyncCommand<GetInfoCommandSettings>
{
    private static void WriteRichItemInfo(BlobItem item,
        BlobItemProperty[]? properties,
        BlobItemContentMetaModel? contentMeta)
    {
        AnsiConsole.MarkupLine($"[yellow]{item}[/]");

        if (properties?.Length > 0)
        {
            StringBuilder sb = new();
            int n = 0;
            foreach (BlobItemProperty p in properties)
            {
                if (++n > 1) sb.AppendLine();
                sb.Append("  [yellow]").Append(n)
                    .Append("[/]. [cyan]").Append(p.Name)
                    .Append("[/]=[yellow]").Append(p.Value).Append("[/]");
            }
            Panel panel = new(sb.ToString())
            {
                Header = new PanelHeader("properties")
            };
            AnsiConsole.Write(panel);
        }

        if (contentMeta != null)
        {
            StringBuilder sb = new();
            sb.Append("- [cyan]MIME type[/]: [yellow]")
                .Append(contentMeta.MimeType).AppendLine("[/]");
            sb.Append("- [cyan]size[/]: [yellow]")
                .Append(contentMeta.Size).AppendLine("[/]");
            sb.Append("- [cyan]hash[/]: [yellow]")
                .Append(contentMeta.Hash).AppendLine("[/]");
            sb.Append("- [cyan]user[/]: [yellow]")
                .Append(contentMeta.UserId).AppendLine("[/]");
            sb.Append("- [cyan]modified[/]: [yellow]")
                .Append(contentMeta.DateModified).Append("[/]");

            Panel panel = new(sb.ToString())
            {
                Header = new PanelHeader("content")
            };
            AnsiConsole.Write(panel);
        }
    }

    private static void WritePlainItemInfo(BlobItem item,
        BlobItemProperty[]? properties,
        BlobItemContentMetaModel? contentMeta,
        TextWriter writer)
    {
        writer.WriteLine(item.ToString());

        if (properties?.Length > 0)
        {
            writer.WriteLine("  - properties:");
            int n = 0;
            foreach (BlobItemProperty p in properties)
                writer.WriteLine($"  {++n}. {p.Name}={p.Value}");
        }

        if (contentMeta != null)
        {
            writer.WriteLine("  - content:");
            writer.WriteLine($"    - MIME type: {contentMeta.MimeType}");
            writer.WriteLine($"    - size: {contentMeta.Size}");
            writer.WriteLine($"    - hash: {contentMeta.Hash}");
            writer.WriteLine($"    - user: {contentMeta.UserId}");
            writer.WriteLine($"    - modified: {contentMeta.DateModified}");
        }
    }

    public override async Task<int> ExecuteAsync(CommandContext context,
        GetInfoCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]GET ITEM INFO[/]");
        CliAppContext.Logger?.LogInformation("---GET ITEM INFO---");

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

        BlobItem? item = null;
        BlobItemProperty[]? props = null;
        BlobItemContentMetaModel? contentMeta = null;

        await AnsiConsole.Status().Start("Fetching data...", async ctx =>
        {
            ctx.Status("Loading item");
            ctx.Spinner(Spinner.Known.Star);

            // load item
            item = await client.GetFromJsonAsync<BlobItem>($"items/{settings.Id}");
            if (item == null)
            {
                AnsiConsole.MarkupLine($"[red]Item not found: {settings.Id}[/]");
            }
            else
            {
                // load its properties
                ctx.Status("Loading properties");

                props = await client.GetFromJsonAsync<BlobItemProperty[]>
                    ($"properties/{item.Id}");

                // load its content metadata
                ctx.Status("Loading metadata");

                contentMeta = await client.GetFromJsonAsync<BlobItemContentMetaModel>(
                        $"contents/{item.Id}/meta");

                if (!string.IsNullOrEmpty(settings.OutputPath))
                {
                    StreamWriter writer = new(new FileStream(settings.OutputPath,
                        FileMode.Create, FileAccess.Write, FileShare.Read),
                        Encoding.UTF8);
                    WritePlainItemInfo(item, props, contentMeta, writer);
                    writer.Flush();
                }
                else
                {
                    WriteRichItemInfo(item, props, contentMeta);
                }
            }
        });

        return 0;
    }
}

internal sealed class GetInfoCommandSettings : AuthCommandSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("The ID of the item to get info for")]
    public string? Id { get; set; }

    [CommandOption("-f|--file <FILE_PATH>")]
    [Description("The path to the output file (if not set, the output will be displayed)")]
    public string? OutputPath { get; set; }
}
