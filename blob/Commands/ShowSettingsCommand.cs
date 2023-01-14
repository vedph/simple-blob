using Spectre.Console.Cli;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    internal sealed class ShowSettingsCommand : AsyncCommand
    {
        public override Task<int> ExecuteAsync(CommandContext context)
        {
            CommandHelper.GetApiRootUriAndNotify();
            return Task.FromResult(0);
        }
    }
}
