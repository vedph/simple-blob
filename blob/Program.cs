using Fusi.Cli.Commands;
using Microsoft.Extensions.CommandLineUtils;
using Serilog;
using Serilog.Extensions.Logging;
using SimpleBlob.Cli.Commands;
using SimpleBlob.Cli.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleBlob.Cli;

public static class Program
{
#if DEBUG
    private static void DeleteLogs()
    {
        foreach (var path in Directory.EnumerateFiles(
            AppDomain.CurrentDomain.BaseDirectory, "Blob-log*.txt"))
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

    private static BlobCliAppContext? GetAppContext(string[] args)
    {
        return new CliAppContextBuilder<BlobCliAppContext>(args)
            .SetNames("Blob", "Blob CLI")
            .SetLogger(new SerilogLoggerProvider(Log.Logger)
                .CreateLogger(nameof(Program)))
            .SetDefaultConfiguration()
            .SetCommands(new Dictionary<string,
                Action<CommandLineApplication, ICliAppContext>>
            {
                ["list"] = ListCommand.Configure,
                ["upload"] = UploadCommand.Configure,
                ["download"] = DownloadCommand.Configure,
                ["get-info"] = GetInfoCommand.Configure,
                ["add-props"] = AddPropertiesCommand.Configure,
                ["delete"] = DeleteItemCommand.Configure,
                ["list-users"] = ListUsersCommand.Configure,
                ["add-user"] = AddUserCommand.Configure,
                ["update-user"] = UpdateUserCommand.Configure,
                ["delete-user"] = DeleteUserCommand.Configure,
                ["add-user-roles"] = AddUserRolesCommand.Configure,
                ["delete-user-roles"] = DeleteUserRolesCommand.Configure,
            })
        .Build();
    }

    public static async Task<int> Main(string[] args)
    {
        try
        {
            // https://github.com/serilog/serilog-sinks-file
            string logFilePath = Path.Combine(
                Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location) ?? "",
                    "Blob-log.txt");
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
            Console.OutputEncoding = Encoding.UTF8;
            Stopwatch stopwatch = new();
            stopwatch.Start();

            BlobCliAppContext? context = GetAppContext(args);

            if (context?.Command == null)
            {
                // RootCommand will have printed help
                return 1;
            }

            Console.Clear();
            int result = await context.Command.Run();

            Console.ResetColor();
            Console.CursorVisible = true;
            Console.WriteLine();
            Console.WriteLine();

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
            Console.CursorVisible = true;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.ToString());
            Console.ResetColor();
            return 2;
        }
    }
}
