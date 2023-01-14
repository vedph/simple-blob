using Fusi.Api.Auth.Controllers;
using Fusi.Tools.Data;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SimpleBlob.Cli.Commands;

internal sealed class ListUsersCommand : AsyncCommand<ListUsersCommandSettings>
{
    private static void WriteRichPage(DataPage<NamedUserModel> page)
    {
        Table table = new();

        table.AddColumns("name", "email", "conf.", "roles", "first", "last", "lock");
        foreach (NamedUserModel user in page.Items)
        {
            table.AddRow(user.UserName ?? "",
                user.Email ?? "",
                user.EmailConfirmed? "1":"0",
                user.Roles?.Count > 0 ? string.Join(" ", user.Roles) : "",
                user.FirstName ?? "",
                user.LastName ?? "",
                user.LockoutEnabled ? "1" : "0");
        }

        AnsiConsole.Write(table);
    }

    private static void WritePlainPage(DataPage<NamedUserModel> page,
        TextWriter writer)
    {
        writer.WriteLine($"--- {page.PageNumber}/{page.PageCount}");

        int n = (page.PageNumber - 1) * page.PageSize;
        foreach (var item in page.Items)
        {
            writer.WriteLine($"[{++n}]");
            writer.WriteLine($"  - User name: {item.UserName}");
            writer.WriteLine($"  - Email: {item.Email}");
            writer.WriteLine($"  - Conf.email: {item.EmailConfirmed}");
            if (item.Roles?.Count > 0)
            {
                writer.WriteLine("  - Roles: " + string.Join(
                    ", ", item.Roles));
            }
            writer.WriteLine($"  - First name: {item.FirstName}");
            writer.WriteLine($"  - Last name: {item.LastName}");
            writer.WriteLine($"  - Lock enabled: {item.LockoutEnabled}");
            if (item.LockoutEnd != null)
                writer.WriteLine($"  - Lock end: {item.LockoutEnd}");

            writer.WriteLine();
        }
    }

    private static string BuildQueryString(ListUsersCommandSettings settings)
    {
        // https://stackoverflow.com/questions/17096201/build-query-string-for-system-net-httpclient-get
        NameValueCollection query = HttpUtility.ParseQueryString("");
        query["PageNumber"] = settings.PageNumber.ToString();
        query["PageSize"] = settings.PageSize.ToString();

        if (!string.IsNullOrEmpty(settings.Name)) query["Name"] = settings.Name;

        return query.ToString() ?? "";
    }

    public override async Task<int> ExecuteAsync(CommandContext context,
        ListUsersCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[green underline]LIST USERS[/]");
        CliAppContext.Logger?.LogInformation("---LIST USERS---");

        string? apiRootUri = CommandHelper.GetApiRootUriAndNotify();
        if (apiRootUri == null) return 2;

        // prompt for userID/password if required
        LoginCredentials credentials = new(
            settings.User,
            settings.Password);
        credentials.PromptIfRequired();

        // login
        ApiLogin? login =
            await CommandHelper.LoginAndNotify(apiRootUri, credentials);
        if (login == null) return 1;

        // setup client
        using HttpClient client = ClientHelper.GetClient(apiRootUri,
            login.Token);

        // get page
        DataPage<NamedUserModel>? page = null;
        await AnsiConsole.Status().Start("Fetching users...", async ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);

            page = (await client.GetFromJsonAsync<DataPage<NamedUserModel>>(
                $"users?{BuildQueryString(settings)}"));
        });

        // write page
        if (page != null)
        {
            if (!string.IsNullOrEmpty(settings.OutputPath))
            {
                TextWriter writer = new StreamWriter(
                    new FileStream(settings.OutputPath,
                        FileMode.Create, FileAccess.Write, FileShare.Read),
                        Encoding.UTF8);
                WritePlainPage(page!, writer);
                writer.Flush();
            }
            else
            {
                WriteRichPage(page!);
            }
        }

        return 0;
    }
}

internal sealed class ListUsersCommandSettings : AuthCommandSettings
{
    [CommandOption("-n|--pagenr <NUMBER>")]
    [Description("The page number")]
    [DefaultValue(1)]
    public int PageNumber { get; set; }

    [CommandOption("-z|--pagesz <SIZE>")]
    [Description("The page size (1-N)")]
    [DefaultValue(20)]
    public int PageSize { get; set; }

    [CommandOption("-m|--name <NAME>")]
    [Description("Any portion of the user name or ID to match")]
    public string? Name { get; set; }

    [CommandOption("-f|--file <FILE_PATH>")]
    [Description("The path to the output file (if not set, the output will be displayed)")]
    public string? OutputPath { get; set; }

    public ListUsersCommandSettings()
    {
        PageNumber = 1;
        PageSize = 20;
    }
}
