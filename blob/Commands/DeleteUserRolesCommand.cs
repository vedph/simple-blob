using Fusi.Cli;
using Fusi.Cli.Commands;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using SimpleBlob.Cli.Services;
using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SimpleBlob.Cli.Commands;

public sealed class DeleteUserRolesCommand : ICommand
{
    private readonly UserRolesCommandOptions _options;

    public DeleteUserRolesCommand(UserRolesCommandOptions options)
    {
        _options = options;
    }

    public static void Configure(CommandLineApplication app,
        ICliAppContext context)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));

        app.Description = "Delete the specified roles from a user.";
        app.HelpOption("-?|-h|--help");

        CommandArgument nameArgument = app.Argument("[name]", "The user name");
        CommandOption roleOption = app.Option("--role|-r",
            "The role (repeatable)", CommandOptionType.MultipleValue);

        // credentials
        CommandHelper.AddCredentialsOptions(app);

        app.OnExecute(() =>
        {
            UserRolesCommandOptions co = new(context)
            {
                UserName = nameArgument.Value,
                Roles = roleOption.Values.ToArray()
            };
            // credentials
            CommandHelper.SetCredentialsOptions(app, co);

            context.Command = new DeleteUserRolesCommand(co);
            return 0;
        });
    }

    public async Task<int> Run()
    {
        ColorConsole.WriteWrappedHeader("Delete User Roles");
        _options.Logger?.LogInformation("---DELETE USER ROLES---");

        string? apiRootUri = CommandHelper.GetApiRootUriAndNotify(_options);
        if (apiRootUri == null) return 2;

        // prompt for userID/password if required
        LoginCredentials credentials = new(
            _options.UserId,
            _options.Password);
        credentials.PromptIfRequired();

        // login
        ApiLogin? login =
            await CommandHelper.LoginAndNotify(apiRootUri, credentials);
        if (login == null) return 2;

        // setup client
        using HttpClient client = ClientHelper.GetClient(apiRootUri,
            login.Token);

        Console.Write($"Adding roles to user {_options.UserName}... ");

        // https://stackoverflow.com/questions/17096201/build-query-string-for-system-net-httpclient-get
        NameValueCollection query = HttpUtility.ParseQueryString("");
        query["roles"] = string.Join(",", _options.Roles!);

        HttpResponseMessage response = await client.DeleteAsync(
            $"users/{_options.UserName}/roles?" + query.ToString());

        if (!response.IsSuccessStatusCode)
        {
            string error = $"Error deleting roles from user {_options.UserName}";
            _options.Logger?.LogError(error);
            ColorConsole.WriteError(error);
            return 2;
        }
        Console.WriteLine("done");

        return 0;
    }
}
