using Serilog;
using SimpleBlob.Cli.Commands;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SimpleBlob.Cli;

public static class Program
{
#if DEBUG
    private static void DeleteLogs()
    {
        foreach (var path in Directory.EnumerateFiles(
            AppDomain.CurrentDomain.BaseDirectory, "blob-log*.txt"))
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
#endif

    public static async Task<int> Main(string[] args)
    {
        try
        {
            // https://github.com/serilog/serilog-sinks-file
            string logFilePath = Path.Combine(
                Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location) ?? "",
                    "blob-log.txt");
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .Enrich.FromLogContext()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
#if DEBUG
            DeleteLogs();
#endif
            Stopwatch stopwatch = new();
            stopwatch.Start();

            CommandApp app = new();
            app.Configure(config =>
            {
                config.AddCommand<AddPropertiesCommand>("add-props")
                    .WithDescription("Add or set the properties for a specified item");

                config.AddCommand<AddUserCommand>("add-user")
                    .WithDescription("Add a user account");

                config.AddCommand<AddUserRolesCommand>("add-user-roles")
                    .WithDescription("Add role(s) to a user");

                config.AddCommand<DeleteItemCommand>("delete")
                    .WithDescription("Delete the specified item");

                config.AddCommand<DeleteUserCommand>("delete-user")
                    .WithDescription("Delete the specified user account");

                config.AddCommand<DeleteUserRolesCommand>("delete-user-roles")
                    .WithDescription("Delete the specified roles from a user");

                config.AddCommand<DownloadCommand>("download")
                    .WithDescription("Download items matching the specified filters");

                config.AddCommand<GetInfoCommand>("get-info")
                    .WithDescription("Get information about an item");

                config.AddCommand<ListCommand>("list")
                    .WithDescription("Get a list of items");

                config.AddCommand<ListUsersCommand>("list-users")
                    .WithDescription("List user accounts");

                config.AddCommand<UpdateUserCommand>("update-user")
                    .WithDescription("Update metadata for the specified user");

                config.AddCommand<UploadCommand>("upload")
                    .WithDescription("Upload items from matching files");

                config.AddCommand<ShowSettingsCommand>("settings")
                    .WithDescription("Show relevant tool settings");
            });

            int result = await app.RunAsync(args);

            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                Console.WriteLine("\nTime: {0}h{1}'{2}\"",
                    stopwatch.Elapsed.Hours,
                    stopwatch.Elapsed.Minutes,
                    stopwatch.Elapsed.Seconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            AnsiConsole.WriteException(ex);
            return 2;
        }
    }
}
