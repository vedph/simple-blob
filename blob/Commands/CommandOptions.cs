using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SimpleBlob.Cli.Commands
{
    /// <summary>
    /// Options shared across all the commands.
    /// </summary>
    public class CommandOptions
    {
        public IConfiguration Configuration { get; set; }
        public ILogger Logger { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
    }
}
