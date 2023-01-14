using SimpleBlob.Cli.Services;
using Spectre.Console;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace SimpleBlob.Cli.Commands;

static internal class CommandHelper
{
    public static DateTime? ParseDate(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        Match m = Regex.Match(text,
            @"^(?<y>\d{4})-(?<m>\d{1,2})-(?<d>\d{1,2})", RegexOptions.Compiled);
        return m.Success
            ? new DateTime(
                int.Parse(m.Groups["y"].Value, CultureInfo.InvariantCulture),
                int.Parse(m.Groups["m"].Value, CultureInfo.InvariantCulture),
                int.Parse(m.Groups["d"].Value, CultureInfo.InvariantCulture))
            : null;
    }

    public static string? GetApiRootUriAndNotify()
    {
        string? apiRootUri = CliAppContext.Configuration?.GetSection("ApiRootUri")?.Value;
        if (string.IsNullOrEmpty(apiRootUri))
        {
            AnsiConsole.MarkupLine("[red]Missing ApiUri in configuration[/]");
            return null;
        }
        AnsiConsole.MarkupLine($"API: [cyan]{apiRootUri}[/]");
        return apiRootUri;
    }

    public static async Task<ApiLogin?> LoginAndNotify(string apiRootUri,
        LoginCredentials credentials)
    {
        if (credentials.UserName == null || credentials.Password == null)
        {
            AnsiConsole.MarkupLine("[red]Credentials not specified[/]");
            return null;
        }

        return await AnsiConsole.Status().Start("Logging in...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);

            ApiLogin login = new(apiRootUri);
            if (!await login.Login(credentials.UserName, credentials.Password))
            {
                AnsiConsole.MarkupLine("[red]Unable to login[/]");
                return null;
            }
            return login;
        });
    }

    public static string BuildItemListQueryString(ItemListSettings options)
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

        return query.ToString() ?? "";
    }
}
