using Fusi.Cli.Commands;
using SimpleBlob.Cli.Services;

namespace SimpleBlob.Cli.Commands
{
    /// <summary>
    /// Options shared across all the commands.
    /// </summary>
    public class AppCommandOptions : CommandOptions<BlobCliAppContext>
    {
        public string? UserId { get; set; }
        public string? Password { get; set; }

        protected AppCommandOptions(ICliAppContext context) :
            base((BlobCliAppContext)context)
        {
        }
    }
}
