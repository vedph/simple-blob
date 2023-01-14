using Microsoft.Extensions.Logging;
using SimpleBlob.Api.Models;
using SimpleBlob.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands;

internal sealed class AddPropertiesCommand : AsyncCommand<AddPropertiesCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context,
        AddPropertiesCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[yellow underline]ADD PROPERTIES[/]");
        CliAppContext.Logger?.LogInformation("---ADD PROPERTIES---");

        string? apiRootUri = CommandHelper.GetApiRootUriAndNotify();
        if (apiRootUri == null) return 2;

        // prompt for userID/password if required
        LoginCredentials credentials = new(
            settings.User,
            settings.Password);
        credentials.PromptIfRequired();

        // login
        ApiLogin? _login =
            await CommandHelper.LoginAndNotify(apiRootUri, credentials);
        if (_login == null) return 2;

        // setup client
        using HttpClient client = ClientHelper.GetClient(apiRootUri,
            _login.Token);

        // collect properties
        List<BlobItemPropertyModel> props = new();

        // get properties from file if any
        if (!string.IsNullOrEmpty(settings.MetaPath))
        {
            IList<Tuple<string, string>> metadata = new CsvMetadataFile
            {
                Delimiter = settings.MetaDelimiter!
            }.Read(settings.MetaPath);
            props.AddRange(metadata.Select(m => new BlobItemPropertyModel
                { Name = m.Item1, Value = m.Item2 }));
        }

        // get properties from command line if any
        if (settings.Properties?.Length > 0)
        {
            foreach (string pair in settings.Properties)
            {
                BlobItemPropertyModel prop = new();
                int i = pair.IndexOf('=');
                if (i == -1)
                {
                    prop.Name = pair;
                    prop.Value = "";
                }
                else
                {
                    prop.Name = pair[..i];
                    prop.Value = pair[(i + 1)..];
                }
                props.Add(prop);
            }
        }

        // no property to add is valid only when reset is true
        AnsiConsole.MarkupLine($"Properties to add: [cyan]{props.Count}[/]");
        if (props.Count == 0 && !settings.IsResetEnabled) return 0;

        string uri = $"properties/{settings.Id}/" +
            (settings.IsResetEnabled ? "set" : "add");

        await AnsiConsole.Status().Start("Setting properties...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);

            HttpResponseMessage response = await client.PostAsJsonAsync(uri,
                new BlobItemPropertiesModel
                {
                    ItemId = settings.Id,
                    Properties = props.ToArray()
                });
            response.EnsureSuccessStatusCode();
        });

        AnsiConsole.MarkupLine("[green]Properties added[/]");

        return 0;
    }
}

internal class AddPropertiesCommandSettings : AuthCommandSettings
{
    [CommandArgument(0, "<ITEM_ID>")]
    [Description("The item ID")]
    public string? Id { get; set; }

    [CommandArgument(1, "<PROPERTY>")]
    [Description("The property name=value (repeatable)")]
    public string[] Properties { get; set; }

    [CommandOption("-r|--reset")]
    [Description("Clear all the properties before adding the new ones")]
    public bool IsResetEnabled { get; set; }

    [CommandOption("-f|--file <METADATA_PATH>")]
    [Description("The delimited metadata file to load properties from")]
    public string? MetaPath { get; set; }

    [CommandOption("--metasep <SEPARATOR>")]
    [Description("The separator used in delimited metadata files")]
    [DefaultValue(",")]
    public string MetaDelimiter { get; set; }

    public AddPropertiesCommandSettings()
    {
        Properties = Array.Empty<string>();
        MetaDelimiter= ",";
    }
}
