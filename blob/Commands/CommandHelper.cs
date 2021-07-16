using Microsoft.Extensions.CommandLineUtils;
using SimpleBlob.Cli.Services;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SimpleBlob.Cli.Commands
{
    static internal class CommandHelper
    {
        public static DateTime? ParseDate(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            Match m = Regex.Match(text,
                @"^(?<y>\d{4})-(?<m>\d{1,2})-(?<d>\d{1,2})");
            return m.Success
                ? new DateTime(
                    int.Parse(m.Groups["y"].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups["m"].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups["d"].Value, CultureInfo.InvariantCulture))
                : null;
        }

        public static int GetOptionValue(CommandOption option, int defValue)
        {
            if (!option.HasValue()) return defValue;
            return int.TryParse(option.Value(), out int n) ? n : defValue;
        }

        public static DateTime GetOptionValue(CommandOption option, DateTime defValue)
        {
            if (!option.HasValue()) return defValue;

            DateTime? d = ParseDate(option.Value());
            return d ?? defValue;
        }

        public static string GetAndNotifyApiRootUri(CommandOptions options)
        {
            string apiRootUri = options.Configuration
                .GetSection("ApiRootUri")?.Value;
            if (string.IsNullOrEmpty(apiRootUri))
            {
                ColorConsole.WriteError("Missing ApiUri in configuration");
                return null;
            }
            ColorConsole.WriteInfo("Target: " + apiRootUri);
            return apiRootUri;
        }

        public static void AddCredentialsOptions(CommandLineApplication app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            CommandOption userOption = app.Option("--user|-u",
                "The BLOB user name", CommandOptionType.SingleValue);

            CommandOption pwdOption = app.Option("--pwd|-p",
                "The BLOB user password", CommandOptionType.SingleValue);
        }

        public static void SetCredentialsOptions(CommandLineApplication app,
            CommandOptions options)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (options == null) throw new ArgumentNullException(nameof(options));

            options.UserId = app.Options.Find(o => o.ShortName == "u")?.Value();
            options.Password = app.Options.Find(o => o.ShortName == "p")?.Value();
        }
    }
}
