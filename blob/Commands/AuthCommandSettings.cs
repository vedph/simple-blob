using Spectre.Console.Cli;
using System.ComponentModel;

namespace SimpleBlob.Cli.Commands;

internal class AuthCommandSettings : CommandSettings
{
    [CommandOption("-u|--user <USER>")]
    [Description("The BLOB user name")]
    public string? User { get; set; }

    [CommandOption("-p|--pwd <PASSWORD>")]
    [Description("The BLOB user password")]
    public string? Password { get; set; }
}
