using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    public sealed class AddPropertiesCommand : ICommand
    {
        private readonly AddPropertiesCommandOptions _options;
        private ApiLogin _login;

        public AddPropertiesCommand(AddPropertiesCommandOptions options)
        {
            _options = options;
        }

        public static void Configure(CommandLineApplication app,
            AppOptions options)
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
                AddPropertiesCommandOptions co = new AddPropertiesCommandOptions
                {
                    Configuration = options.Configuration,
                    Logger = options.Logger,
                    Id = idArgument.Value,
                    IsResetEnabled = resetOption.HasValue(),
                    Properties = propOption.Values.Count > 0
                        ? string.Join(",", propOption.Values)
                        : null,
                    MetaPath = metaPathOption.Value(),
                    MetaDelimiter = metaDelimOption.HasValue()
                        ? metaDelimOption.Value() : ",",
                };
                // credentials
                CommandHelper.SetCredentialsOptions(app, co);

                options.Command = new AddPropertiesCommand(co);
                return 0;
            });
        }

        public async Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("Add Properties");
            _options.Logger.LogInformation("---ADD PROPERTIES---");

            string apiRootUri = CommandHelper.GetApiRootUriAndNotify(_options);
            if (apiRootUri == null) return 2;

            // prompt for userID/password if required
            LoginCredentials credentials = new LoginCredentials(
                _options.UserId,
                _options.Password);
            credentials.PromptIfRequired();

            // login
            _login = CommandHelper.LoginAndNotify(apiRootUri, credentials);

            throw new NotImplementedException();
        }
    }

    public sealed class AddPropertiesCommandOptions : CommandOptions
    {
        public string Id { get; set; }
        public bool IsResetEnabled { get; set; }
        public string Properties { get; set; }
        public string MetaPath { get; set; }
        public string MetaDelimiter { get; set; }
    }
}
