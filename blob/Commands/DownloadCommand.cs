﻿using Fusi.Tools.Data;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using SimpleBlob.Core;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands;

internal sealed class DownloadCommand : AsyncCommand<DownloadCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context,
        DownloadCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]DOWNLOAD ITEMS[/]");
        CliAppContext.Logger?.LogInformation("---DOWNLOAD ITEMS---");

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

        // get 1st page
        var page = await client.GetFromJsonAsync<DataPage<BlobItem>>(
            "items?" + CommandHelper.BuildItemListQueryString(settings));

        AnsiConsole.MarkupLine($"Matching items: [yellow]{page!.Total}[/]");
        if (page.Total == 0) return 0;

        if (!Directory.Exists(settings.OutputDir))
            Directory.CreateDirectory(settings.OutputDir ?? "");

        int itemNr = 0;
        int pageCount = settings.PageCount > 0
            ? Math.Min(settings.PageCount, page.PageCount)
            : page.PageCount;

        while (true)
        {
            AnsiConsole.MarkupLine("[cyan]Page[/] " +
                $"[yellow]{settings.PageNumber}[/][cyan] of [/][yellow]{pageCount}[/]");

            foreach (BlobItem item in page!.Items)
            {
                itemNr++;
                CliAppContext.Logger?.LogInformation($"{itemNr} {item}");
                AnsiConsole.MarkupLine($"[yellow]{itemNr}[/] [cyan]{item}[/]");

                // get stream
                HttpResponseMessage response =
                    await client.GetAsync($"contents/{item.Id}");
                using Stream input = await response.Content.ReadAsStreamAsync();

                // save to file
                string itemPath = item.Id.Replace(settings.IdDelimiter,
                    new string(Path.DirectorySeparatorChar, 1));
                string dir = Path.Combine(settings.OutputDir ?? "",
                    Path.GetDirectoryName(itemPath) ?? "");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string path = Path.Combine(dir, Path.GetFileName(itemPath));
                AnsiConsole.MarkupLine($"[yellow] => [/][green]{path}[/]");

                using FileStream output = new(path, FileMode.Create,
                    FileAccess.Write, FileShare.Read);
                input.CopyTo(output);
                output.Flush();

                // load metadata
                List<Tuple<string, string>> metadata = new();

                var props = await client.GetFromJsonAsync<BlobItemProperty[]>(
                    $"properties/{item.Id}");
                bool hasId = false;
                foreach (var p in props!)
                {
                    if (p.Name == "id") hasId = true;
                    metadata.Add(Tuple.Create(p.Name, p.Value));
                }
                if (!hasId) metadata.Add(Tuple.Create("id", item.Id));

                // save metadata to path
                path += settings.MetaExtension;
                AnsiConsole.MarkupLine($"[yellow] => [/][green]{path}[/]");
                CsvMetadataFile metaFile = new()
                {
                    Delimiter = settings.MetaDelimiter
                };
                metaFile.Write(metadata, path);
            }

            // next page
            if (++settings.PageNumber > pageCount) break;
            page = await client.GetFromJsonAsync<DataPage<BlobItem>>(
                "items?" + CommandHelper.BuildItemListQueryString(settings));
        }
        return 0;
    }
}

internal sealed class DownloadCommandSettings : ItemListSettings
{
    [CommandArgument(0, "<OUTPUT_DIR>")]
    [Description("The output directory")]
    public string? OutputDir { get; set; }

    [CommandOption("--pages <LIMIT>")]
    [Description("The maximum count of pages to fetch")]
    public int PageCount { get; set; }

    [CommandOption("-e|--meta <EXTENSION>")]
    [Description("The extension appended to the content filename " +
        "to represent its metadata in a correspondent file")]
    [DefaultValue(".meta")]
    public string MetaExtension { get; set; }

    [CommandOption("--metasep <SEPARATOR>")]
    [Description("The separator used in delimited metadata files")]
    [DefaultValue(",")]
    public string MetaDelimiter { get; set; }

    [CommandOption("--idsep <SEPARATOR>")]
    [Description("The conventional separator used in BLOB IDs " +
        "for virtual folders")]
    [DefaultValue("|")]
    public string IdDelimiter { get; set; }

    public DownloadCommandSettings()
    {
        MetaExtension = ".meta";
        MetaDelimiter = ",";
        IdDelimiter = "|";
    }
}
