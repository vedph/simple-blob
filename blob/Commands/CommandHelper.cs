using Microsoft.Extensions.CommandLineUtils;
using SimpleBlob.Cli.Services;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

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
            if (option?.HasValue() != true) return defValue;

            return int.TryParse(option.Value(), out int n) ? n : defValue;
        }

        public static DateTime GetOptionValue(CommandOption option, DateTime defValue)
        {
            if (option?.HasValue() != true) return defValue;

            DateTime? d = ParseDate(option.Value());
            return d ?? defValue;
        }

        public static string GetApiRootUriAndNotify(CommandOptions options)
        {
            string apiRootUri = options.Configuration
                .GetSection("ApiRootUri")?.Value;
            if (string.IsNullOrEmpty(apiRootUri))
            {
                ColorConsole.WriteError("Missing ApiUri in configuration");
                return null;
            }
            ColorConsole.WriteInfo("API: " + apiRootUri);
            return apiRootUri;
        }

        public static async Task<ApiLogin> LoginAndNotify(string apiRootUri,
            LoginCredentials credentials)
        {
            Console.Write("Logging in... ");
            ApiLogin login = new(apiRootUri);
            if (!await login.Login(credentials.UserName, credentials.Password))
            {
                ColorConsole.WriteError("Unable to login");
                return null;
            }
            Console.WriteLine("done");
            return login;
        }

        public static void AddCredentialsOptions(CommandLineApplication app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            app.Option("--user|-u", "The BLOB user name",
                CommandOptionType.SingleValue);

            app.Option("--pwd|-p", "The BLOB user password",
                CommandOptionType.SingleValue);
        }

        public static void SetCredentialsOptions(CommandLineApplication app,
            CommandOptions options)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (options == null) throw new ArgumentNullException(nameof(options));

            options.UserId = app.Options.Find(o => o.ShortName == "u")?.Value();
            options.Password = app.Options.Find(o => o.ShortName == "p")?.Value();
        }

        public static void AddItemListOptions(CommandLineApplication app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            app.Option("--page-nr|-n", "The page number (1-N)",
                CommandOptionType.SingleValue);
            app.Option("--page-sz|-z", "The page size",
                CommandOptionType.SingleValue);

            app.Option("--id|-i", "The ID filter (wildcards ? and *)",
                CommandOptionType.SingleValue);

            app.Option("--mime|-m", "The mime type", CommandOptionType.SingleValue);

            app.Option("--dates|-d", "The dates range (min:max, min:, :max)",
                CommandOptionType.SingleValue);

            app.Option("--sizes|-s", "The sizes range (min:max, min:, :max)",
                CommandOptionType.SingleValue);

            app.Option("--last-user|-l", "The last user who modified the item",
                CommandOptionType.SingleValue);

            app.Option("--prop|-o", "A name=value property to match (repeatable)",
                CommandOptionType.MultipleValue);
        }

        public static void SetItemListOptions(CommandLineApplication app,
            ItemListOptions options)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (options == null) throw new ArgumentNullException(nameof(options));

            CommandOption dateOption = app.Options.Find(o => o.ShortName == "n");
            CommandOption sizeOption = app.Options.Find(o => o.ShortName == "s");
            CommandOption propOption = app.Options.Find(o => o.ShortName == "o");

            options.PageNumber = GetOptionValue(app.Options.Find(
                o => o.ShortName == "n"), 1);
            options.PageSize = GetOptionValue(app.Options.Find(
                o => o.ShortName == "z"), 20);
            options.Id = app.Options.Find(o => o.ShortName == "i")?.Value();
            options.MimeType = app.Options.Find(o => o.ShortName == "m")?.Value();
            options.LastUserId = app.Options.Find(o => o.ShortName == "l").Value();
            options.Properties = propOption.Values.Count > 0
                ? string.Join(",", propOption.Values)
                : null;

            Regex rngRegex = new("^(?<a>[^:]+)?:(?<b>.+)?");

            // dates
            if (dateOption.HasValue())
            {
                Match m = rngRegex.Match(dateOption.Value());
                if (m.Success)
                {
                    options.MinDateModified = ParseDate(m.Groups["a"].Value);
                    options.MaxDateModified = ParseDate(m.Groups["b"].Value);
                }
            }

            // sizes
            if (sizeOption.HasValue())
            {
                Match m = rngRegex.Match(dateOption.Value());
                if (m.Success)
                {
                    options.MinSize = long.TryParse(m.Groups["a"].Value,
                        out long min) ? min : 0;
                    options.MaxSize = long.TryParse(m.Groups["b"].Value,
                        out long max) ? max : 0;
                }
            }
        }

        public static string BuildItemListQueryString(ItemListOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            // https://stackoverflow.com/questions/17096201/build-query-string-for-system-net-httpclient-get
            NameValueCollection query = HttpUtility.ParseQueryString("");
            query["PageNumber"] = options.PageNumber.ToString();
            query["PageSize"] = options.PageSize.ToString();

            if (!string.IsNullOrEmpty(options.Id)) query["Id"] = options.Id;

            if (!string.IsNullOrEmpty(options.MimeType))
                query["MimeType"] = options.Id;

            if (options.MinDateModified != null)
            {
                query["MinDateModified"] = options.MinDateModified
                    .Value.ToString("yyyy-MM-dd");
            }
            if (options.MaxDateModified != null)
            {
                query["MaxDateModified"] = options.MaxDateModified
                    .Value.ToString("yyyy-MM-dd");
            }

            if (options.MinSize > 0) query["MinSize"] = options.MinSize.ToString();
            if (options.MaxSize > 0) query["MaxSize"] = options.MaxSize.ToString();

            if (!string.IsNullOrEmpty(options.LastUserId))
                query["UserId"] = options.LastUserId;

            if (!string.IsNullOrEmpty(options.Properties))
                query["Properties"] = options.Properties;

            return query.ToString();
        }
    }
}
