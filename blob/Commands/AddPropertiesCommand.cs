using Fusi.Cli;
using Fusi.Cli.Commands;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using SimpleBlob.Api.Models;
using SimpleBlob.Cli.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands;

internal sealed class AddPropertiesCommand : ICommand
{
    private readonly AddPropertiesCommandOptions _options;

    private AddPropertiesCommand(AddPropertiesCommandOptions options)
    {
        _options = options;
    }

    public static void Configure(CommandLineApplication app,
        ICliAppContext context)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));

        app.Description = "Add the specified properties to a BLOB item.";
        app.HelpOption("-?|-h|--help");

        CommandArgument idArgument = app.Argument("[id]", "The item ID.");

        // credentials
        CommandHelper.AddCredentialsOptions(app);

        CommandOption resetOption = app.Option("--reset|-r",
            "Clear all the properties before adding the new ones.",
            CommandOptionType.NoValue);

        CommandOption propOption = app.Option("--prop|-o",
            "The property name=value pair. Repeatable.",
            CommandOptionType.MultipleValue);

        CommandOption metaPathOption = app.Option("--file|-f",
            "The delimited metadata file to load properties from",
            CommandOptionType.SingleValue);

        CommandOption metaDelimOption = app.Option("--meta-sep",
            "The separator used in delimited metadata files",
            CommandOptionType.SingleValue);

        app.OnExecute(() =>
        {
            AddPropertiesCommandOptions co = new(context)
            {
                Id = idArgument.Value,
                IsResetEnabled = resetOption.HasValue(),
                Properties = propOption.Values.Count > 0
                    ? propOption.Values.ToArray()
                    : null,
                MetaPath = metaPathOption.Value(),
                MetaDelimiter = metaDelimOption.HasValue()
                    ? metaDelimOption.Value() : ",",
            };
            // credentials
            CommandHelper.SetCredentialsOptions(app, co);

            context.Command = new AddPropertiesCommand(co);
            return 0;
        });
    }

    public async Task<int> Run()
    {
        ColorConsole.WriteWrappedHeader("Add Properties");
        _options.Logger?.LogInformation("---ADD PROPERTIES---");

        string? apiRootUri = CommandHelper.GetApiRootUriAndNotify(_options);
        if (apiRootUri == null) return 2;

        // prompt for userID/password if required
        LoginCredentials credentials = new(
            _options.UserId,
            _options.Password);
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
        if (!string.IsNullOrEmpty(_options.MetaPath))
        {
            IList<Tuple<string, string>> metadata = new CsvMetadataFile
            {
                Delimiter = _options.MetaDelimiter!
            }.Read(_options.MetaPath);
            props.AddRange(metadata.Select(m => new BlobItemPropertyModel
                { Name = m.Item1, Value = m.Item2 }));
        }

        // get properties from command line if any
        if (_options.Properties?.Count > 0)
        {
            foreach (string pair in _options.Properties)
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
                    prop.Name = pair.Substring(0, i);
                    prop.Value = pair.Substring(i + 1);
                }
                props.Add(prop);
            }
        }

        // no property to add is valid only when reset is true
        ColorConsole.WriteInfo("Properties to add: " + props.Count);
        if (props.Count == 0 && !_options.IsResetEnabled) return 0;

        string uri = $"properties/{_options.Id}/" +
            (_options.IsResetEnabled ? "set" : "add");

        HttpResponseMessage response = await client.PostAsJsonAsync(uri,
            new BlobItemPropertiesModel
            {
                ItemId = _options.Id,
                Properties = props.ToArray()
            });

        if (!response.IsSuccessStatusCode)
        {
            string error = $"Error adding properties to item {_options.Id}: " +
                string.Join(", ", props.Select(p => $"{p.Name}={p.Value}"));
            _options.Logger?.LogError(error);
            ColorConsole.WriteError(error);

            return 2;
        }

        ColorConsole.WriteSuccess("Properties added");
        return 0;
    }
}

internal class AddPropertiesCommandOptions : AppCommandOptions
{
    public AddPropertiesCommandOptions(ICliAppContext options)
        : base(options)
    {
    }

    public string? Id { get; set; }
    public bool IsResetEnabled { get; set; }
    public IList<string>? Properties { get; set; }
    public string? MetaPath { get; set; }
    public string? MetaDelimiter { get; set; }
}
