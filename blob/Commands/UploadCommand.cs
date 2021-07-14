using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    public sealed class UploadCommand : ICommand
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        private readonly string _user;
        private readonly string _password;
        private readonly string _inputDir;
        private readonly string _fileMask;
        private readonly bool _regexMask;
        private readonly bool _recursive;

        public UploadCommand(AppOptions options, string user, string password,
            string inputDir, string fileMask, bool regexMask,
            bool recursive)
        {
            _config = options.Configuration;
            _logger = options.Logger;
            _user = user;
            _password = password;
            _inputDir = inputDir;
            _fileMask = fileMask;
            _regexMask = regexMask;
            _recursive = recursive;
        }

        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.Description = "Upload all the files matching " +
                "the specified mask from the specified directory.";
            command.HelpOption("-?|-h|--help");

            CommandArgument dirArgument = command.Argument("[inputDir]",
                "The input directory");
            CommandArgument maskArgument = command.Argument("[fileMask]",
                "The files mask");

            CommandOption regexOption = command.Option("--regex|-p",
                "Use a regular expression pattern for the files mask",
                CommandOptionType.NoValue);
            CommandOption recurseOption = command.Option("--recurse|-r",
                "Recurse subdirectories",
                CommandOptionType.NoValue);

            CommandOption userOption = command.Option("--user|-u",
                "The BLOB user ID", CommandOptionType.SingleValue);
            CommandOption pwdOption = command.Option("--pwd|-p",
                "The BLOB user password", CommandOptionType.SingleValue);

            command.OnExecute(() =>
            {
                options.Command = new UploadCommand(
                    options,
                    userOption.Value(), pwdOption.Value(),
                    dirArgument.Value, maskArgument.Value, regexOption.HasValue(),
                    recurseOption.HasValue());
                return 0;
            });
        }

        public Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("Upload Files");
            _logger.LogInformation("---UPLOAD---");

            string uri = _config.GetSection("ApiUri")?.Value;
            if (string.IsNullOrEmpty(uri))
            {
                ColorConsole.WriteError("Missing ApiUri in configuration");
                return Task.FromResult(2);
            }

            // prompt for userID/password if required
            LoginInput login = new LoginInput(_user, _password);
            login.PromptIfRequired();

            int count = 0;
            foreach (string path in FileEnumerator.Enumerate(
                _inputDir, _fileMask, _regexMask, _recursive))
            {
                count++;
                _logger.LogInformation($"{count} {path}");
                ColorConsole.WriteEmbeddedColorLine($"[green]{count:0000}[/green] {path}");
                // TODO
            }

            return Task.FromResult(0);
        }
    }
}
