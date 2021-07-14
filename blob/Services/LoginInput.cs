using System;

namespace SimpleBlob.Cli.Services
{
    /// <summary>
    /// Login data input. This is used to prompt for user ID and/or password
    /// when they are not specified.
    /// </summary>
    public class LoginInput
    {
        public string UserId { get; set; }
        public string Password { get; set; }

        public LoginInput()
        {
        }

        public LoginInput(string userId, string password)
        {
            UserId = userId;
            Password = password;
        }

        private static string PromptRequired(string message)
        {
            ColorConsole.Write(message, ConsoleColor.Yellow);
            string s;
            do
            {
                s = Console.ReadLine();
            } while (string.IsNullOrEmpty(s));
            return s;
        }

        public void PromptIfRequired()
        {
            if (string.IsNullOrEmpty(UserId))
                UserId = PromptRequired("User ID:");

            if (string.IsNullOrEmpty(Password))
                Password = PromptRequired("Password:");
        }
    }
}
