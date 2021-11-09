using Fusi.Api.Auth.Controllers;
using Fusi.Tools.Data;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SimpleBlob.Cli.Commands
{
    public sealed class ListUsersCommand : ICommand
    {
        private readonly ListUsersCommandOptions _options;
        private ApiLogin _login;

        public ListUsersCommand(ListUsersCommandOptions options)
        {
            _options = options;
        }

        public static void Configure(CommandLineApplication app,
            AppOptions options)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            app.Description = "List the users matching " +
                "the specified filters.";
            app.HelpOption("-?|-h|--help");

            // credentials
            CommandHelper.AddCredentialsOptions(app);

            CommandOption pageNrOption = app.Option("--page-nr|-n",
                "The page number (1-N)",
                CommandOptionType.SingleValue);

            CommandOption pageSzOption = app.Option("--page-sz|-z",
                "The page size",
                CommandOptionType.SingleValue);

            CommandOption nameOption = app.Option("--name|-m",
                "A portion of the user name or ID",
                CommandOptionType.SingleValue);

            CommandOption fileOption = app.Option("--file|-f",
                "The path to the output file (if not set, the output will be displayed)",
                CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                ListUsersCommandOptions co = new ListUsersCommandOptions
                {
                    Configuration = options.Configuration,
                    Logger = options.Logger,
                    PageNumber = CommandHelper.GetOptionValue(pageNrOption, 1),
                    PageSize = CommandHelper.GetOptionValue(pageSzOption, 20),
                    Name = nameOption.Value(),
                    OutputPath = fileOption.Value()
                };
                // credentials
                CommandHelper.SetCredentialsOptions(app, co);

                options.Command = new ListUsersCommand(co);
                return 0;
            });
        }

        private static void WritePage(TextWriter writer, DataPage<NamedUserModel> page)
        {
            writer.WriteLine($"--- {page.PageNumber}/{page.PageCount}");

            int n = (page.PageNumber - 1) * page.PageSize;
            foreach (var item in page.Items)
            {
                writer.WriteLine($"[{++n}]");
                writer.WriteLine($"  - User name: {item.UserName}");
                writer.WriteLine($"  - Email: {item.Email}");
                writer.WriteLine($"  - Conf.email: {item.EmailConfirmed}");
                if (item.Roles?.Length > 0)
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

        private string BuildQueryString()
        {
            // https://stackoverflow.com/questions/17096201/build-query-string-for-system-net-httpclient-get
            NameValueCollection query = HttpUtility.ParseQueryString("");
            query["PageNumber"] = _options.PageNumber.ToString();
            query["PageSize"] = _options.PageSize.ToString();

            if (!string.IsNullOrEmpty(_options.Name)) query["Name"] = _options.Name;

            return query.ToString();
        }

        public async Task<int> Run()
        {
            ColorConsole.WriteWrappedHeader("List Users");
            _options.Logger.LogInformation("---LIST USERS---");

            string apiRootUri = CommandHelper.GetApiRootUriAndNotify(_options);
            if (apiRootUri == null) return 2;

            // prompt for userID/password if required
            LoginCredentials credentials = new LoginCredentials(
                _options.UserId,
                _options.Password);
            credentials.PromptIfRequired();

            // login
            _login = await CommandHelper.LoginAndNotify(apiRootUri, credentials);

            // setup client
            using HttpClient client = ClientHelper.GetClient(apiRootUri,
                _login.Token);

            // get page
            var page = await client.GetFromJsonAsync<DataPage<NamedUserModel>>(
                "users?" + BuildQueryString());

            // write page
            TextWriter writer;
            if (!string.IsNullOrEmpty(_options.OutputPath))
            {
                writer = new StreamWriter(new FileStream(_options.OutputPath,
                    FileMode.Create, FileAccess.Write, FileShare.Read),
                    Encoding.UTF8);
            }
            else writer = Console.Out;
            WritePage(writer, page);
            writer.Flush();

            return 0;
        }
    }

    public sealed class ListUsersCommandOptions : CommandOptions
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string Name { get; set; }
        public string OutputPath { get; set; }
    }
}
