using Fusi.Cli.Auth.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    internal sealed class ShowSettingsCommand : AsyncCommand
    {
        private readonly ICliAuthSettings _settings;

        public ShowSettingsCommand(ICliAuthSettings settings)
        {
            _settings = settings
                ?? throw new System.ArgumentNullException(nameof(settings));
        }

        public override Task<int> ExecuteAsync(CommandContext context)
        {
            AnsiConsole.MarkupLine($"API root URI: [cyan]{_settings.ApiRootUri}[/]");
            return Task.FromResult(0);
        }
    }
}
