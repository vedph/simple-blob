using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    public sealed class ListCommand : ICommand
    {
        private readonly ListCommandOptions _options;
        private ApiLogin _login;

        public ListCommand(ListCommandOptions options)
        {
            _options = options;
        }

        public static void Configure(CommandLineApplication app,
            AppOptions options)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            app.Description = "List the BLOB items matching " +
                "the specified filters.";
            app.HelpOption("-?|-h|--help");

            CommandHelper.AddCredentialsOptions(app);

            CommandOption pageNrOption = app.Option("--page-nr|-n",
                "The page number (1-N)", CommandOptionType.SingleValue);
            CommandOption pageSzOption = app.Option("--page-sz|-z",
                "The page size", CommandOptionType.SingleValue);
            CommandOption idOption = app.Option("--id|-i",
                "The ID filter (wildcards ? and *)", CommandOptionType.SingleValue);
            CommandOption mimeOption = app.Option("--mime|-m",
                "The mime type", CommandOptionType.SingleValue);
            CommandOption dateOption = app.Option("--dates|-d",
                "The dates range (min:max, min:, :max)",
                CommandOptionType.SingleValue);
            CommandOption sizeOption = app.Option("--sizes|-s",
                "The sizes range (min:max, min:, :max)",
                CommandOptionType.SingleValue);
            CommandOption lastUserOption = app.Option("--last-user|-l",
                "The last user who modified the item",
                CommandOptionType.SingleValue);
            CommandOption propOption = app.Option("--prop|-o",
                "A name=value property to match (repeatable)",
                CommandOptionType.MultipleValue);
            CommandOption fileOption = app.Option("--file|-f",
                "The path to the output file (if not set, output will be displayed)",
                CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                ListCommandOptions co = new ListCommandOptions
                {
                    Configuration = options.Configuration,
                    Logger = options.Logger,
                    PageNumber = CommandHelper.GetOptionValue(pageNrOption, 1),
                    PageSize = CommandHelper.GetOptionValue(pageSzOption, 20),
                    Id = idOption.Value(),
                    MimeType = mimeOption.Value(),
                    LastUserId = lastUserOption.Value(),
                    Properties = propOption.Values.Count > 0
                        ? string.Join(",", propOption.Values)
                        : null,
                    OutputPath = fileOption.Value()
                };
                CommandHelper.SetCredentialsOptions(app, co);

                Regex rngRegex = new Regex("^(?<a>[^:]+)?:(?<b>.+)?");

                // dates
                if (dateOption.HasValue())
                {
                    Match m = rngRegex.Match(dateOption.Value());
                    if (m.Success)
                    {
                        co.MinDateModified = CommandHelper.ParseDate(
                            m.Groups["a"].Value);
                        co.MaxDateModified = CommandHelper.ParseDate(
                            m.Groups["b"].Value);
                    }
                }

                // sizes
                if (sizeOption.HasValue())
                {
                    Match m = rngRegex.Match(dateOption.Value());
                    if (m.Success)
                    {
                        co.MinSize = long.TryParse(m.Groups["a"].Value,
                            out long min) ? min : 0;
                        co.MaxSize = long.TryParse(m.Groups["b"].Value,
                            out long max) ? max : 0;
                    }
                }

                options.Command = new ListCommand(co);

                return 0;
            });
        }

        public Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("List Items");
            _options.Logger.LogInformation("---LIST ITEMS---");

            string apiRootUri = CommandHelper.GetAndNotifyApiRootUri(_options);

            throw new NotImplementedException();
        }
    }

    public sealed class ListCommandOptions : CommandOptions
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string Id { get; set; }
        public string MimeType { get; set; }
        public DateTime? MinDateModified { get; set; }
        public DateTime? MaxDateModified { get; set; }
        public long MinSize { get; set; }
        public long MaxSize { get; set; }
        public string LastUserId { get; set; }
        public string Properties { get; set; }
        public string OutputPath { get; set; }
    }
}
