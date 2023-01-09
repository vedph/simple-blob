using Fusi.Cli;
using System;

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

    private static string PromptRequired(string message)
    {
        ColorConsole.Write(message, ConsoleColor.Yellow);
        string? s;
        do
        {
            s = Console.ReadLine();
        } while (string.IsNullOrEmpty(s));
        return s;
    }

    public void PromptIfRequired()
    {
        if (string.IsNullOrEmpty(UserName))
            UserName = PromptRequired("Username: ");

        if (string.IsNullOrEmpty(Password))
            Password = PromptRequired("Password: ");
    }
}
