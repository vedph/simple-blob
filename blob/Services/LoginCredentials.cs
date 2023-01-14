using Spectre.Console;

namespace SimpleBlob.Cli.Services;

/// <summary>
/// Login credentials. This is used to prompt for user ID and/or password
/// when they are not specified.
/// </summary>
public class LoginCredentials
{
    public string? UserName { get; set; }
    public string? Password { get; set; }

    public LoginCredentials(string? userId, string? password)
    {
        UserName = userId;
        Password = password;
    }

    public void PromptIfRequired()
    {
        if (string.IsNullOrEmpty(UserName))
            UserName = AnsiConsole.Ask<string>("User name");

        if (string.IsNullOrEmpty(Password))
        {
            Password = AnsiConsole.Prompt(new TextPrompt<string>(
                "Enter [green]password[/]?").Secret());
        }
    }
}
