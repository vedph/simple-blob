using Spectre.Console;
using System;

namespace SimpleBlob.Cli.Services;

internal static class CliHelper
{
    public static void ShowError(Exception ex)
    {
        AnsiConsole.MarkupLineInterpolated($"[red]{ex.Message}[/]");

        if (ex.InnerException != null)
        {
            Exception? inner = ex.InnerException;
            do
            {
                AnsiConsole.MarkupLineInterpolated($"  - [red]{inner.Message}[/]");
                inner = inner.InnerException;
            } while (inner != null);
        }

        AnsiConsole.MarkupLineInterpolated($"[yellow]{ex.StackTrace}[/]");
    }
}
