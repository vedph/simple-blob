using Force.Crc32;
using Fusi.Cli.Auth.Commands;
using Fusi.Cli.Auth.Services;
using Microsoft.Extensions.Logging;
using SimpleBlob.Api.Models;
using SimpleBlob.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands;

internal sealed class UploadCommand : AsyncCommand<UploadCommandSettings>
{
    private readonly MimeTypeMap _typeMap;
    private readonly ICliAuthSettings _settings;

    public UploadCommand(ICliAuthSettings settings)
    {
        _settings = settings
            ?? throw new ArgumentNullException(nameof(settings));
        _typeMap = new();
    }

    private static async Task AddItemAsync(string id, HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "items", new { id });
        response.EnsureSuccessStatusCode();
    }

    private static async Task SetItemPropertiesAsync(string id,
        HttpClient client, IList<Tuple<string, string>>? metadata)
    {
        if (metadata == null || metadata.Count == 0) return;

        BlobItemPropertiesModel model = new()
        {
            ItemId = id,
            Properties = metadata.Select(t => new BlobItemPropertyModel
            {
                Name = t.Item1,
                Value = t.Item2
            }).ToArray()
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"properties/{id}/set", model);
        response.EnsureSuccessStatusCode();
    }

    private static long GetCrc(string path)
    {
        using FileStream stream = new(path, FileMode.Open,
            FileAccess.Read, FileShare.Read);
        BinaryReader reader = new(stream);
        uint crc = 0;
        while (true)
        {
            byte[] buf = reader.ReadBytes(8192);
            crc = Crc32Algorithm.Append(crc, buf);
            if (buf.Length < 8192) break;
        }
        return crc;
    }

    private static async Task<Tuple<bool, string?>> AreContentEqualAsync(
        string id, HttpClient client, string path)
    {
        HttpResponseMessage r = await client.GetAsync(
            $"contents/{id}/meta");
        if (!r.IsSuccessStatusCode)
        {
            return Tuple.Create(false,
                (string?)$"Error getting content metadata for item {id}");
        }

        FileInfo info = new(path);
        BlobItemContentMetaModel meta = (await r.Content
            .ReadFromJsonAsync<BlobItemContentMetaModel>())!;

        // size must be equal
        if (info.Length != meta.Size)
            return Tuple.Create(false, (string?)null);

        // CRC32C must be equal
        long crc = GetCrc(path);
        return Tuple.Create(crc == meta.Hash, (string?)null);
    }

    private async Task<string?> SetItemContentAsync(string id,
        HttpClient client, string path, string apiRootUri,
        UploadCommandSettings settings, ApiLogin login)
    {
        string uri = apiRootUri + $"contents/{id}";
        string? mimeType = settings.MimeType;
        if (string.IsNullOrEmpty(mimeType))
        {
            mimeType = _typeMap.GetType(Path.GetExtension(path));
            if (mimeType == null) return "Unknown extension: " + path;
        }

        // check if required
        if (settings.IsCheckEnabled)
        {
            var t = await AreContentEqualAsync(id, client, path);
            if (t.Item2 != null) return t.Item2;
            if (t.Item1) return null;
        }

        string? response = await FileUploader.UploadFile(uri, path,
            login.Token, id, mimeType);
        Debug.WriteLine(response);
        // TODO response
        return null;
    }

    private static string SanitizePath(string path, string sep)
    {
        path = path.Replace("/", sep);
        return path.Replace("\\", sep);
    }

    private static string GetMetadataPath(string path,
        UploadCommandSettings settings)
    {
        string result = path;

        if (!string.IsNullOrEmpty(settings.MetaExtension))
            result = Path.ChangeExtension(result, settings.MetaExtension);

        if (!string.IsNullOrEmpty(settings.MetaPrefix))
        {
            result = Path.Combine(
                Path.GetDirectoryName(result) ?? "",
                Path.GetFileNameWithoutExtension(result) +
                settings.MetaPrefix +
                Path.GetExtension(result));
        }

        if (!string.IsNullOrEmpty(settings.MetaSuffix))
            result += settings.MetaSuffix;

        return result;
    }

    public override async Task<int> ExecuteAsync(CommandContext context,
        UploadCommandSettings settings)
    {
        AnsiConsole.MarkupLine("[yellow underline]UPLOAD FILES[/]");
        CliAppContext.Logger?.LogInformation("---UPLOAD---");

        // load types if required
        if (!string.IsNullOrEmpty(settings.MimeTypeList))
            _typeMap.Load(settings.MimeTypeList);

        // prompt for userID/password if required
        LoginCredentials credentials = new(
            settings.User,
            settings.Password);
        credentials.PromptIfRequired();

        // login
        ApiLogin? login =
            await CommandHelper.LoginAndNotify(_settings.ApiRootUri, credentials);
        if (login == null) return 2;

        // setup the metadata services
        CsvMetadataFile metaFile = new()
        {
            Delimiter = settings.MetaDelimiter ?? ""
        };

        // setup client
        using HttpClient client = CommandHelper.GetClient(_settings.ApiRootUri,
            login.Token);

        // process files
        int count = 0;

        foreach (string path in FileEnumerator.Enumerate(
            settings.InputDir ?? "", settings.FileMask ?? "",
            settings.IsRegexMask, settings.IsRecursive))
        {
            // skip metadata files
            if (Path.GetExtension(path) == settings.MetaExtension) continue;

            count++;
            CliAppContext.Logger?.LogInformation($"{count} {path}");
            AnsiConsole.MarkupLine($"[yellow]{count:0000}[/] [cyan]{path}[/]");

            // load metadata if any
            string metaPath = GetMetadataPath(path, settings);

            IList<Tuple<string, string>>? metadata = null;
            if (File.Exists(metaPath))
            {
                AnsiConsole.MarkupLine($"- [cyan]{metaPath}[/]");
                metadata = metaFile.Read(metaPath);
            }
            string id = metadata?.FirstOrDefault(t => t.Item1 == "id")
                ?.Item2
                ?? SanitizePath(Path.GetRelativePath(
                    settings.InputDir ?? "", path),
                    settings.IdDelimiter ?? "");

            // add/update item
            if (!settings.IsDryRun)
            {
                await AddItemAsync(id, client);

                // set properties
                await SetItemPropertiesAsync(id, client, metadata);

                // set content
                string? error = await SetItemContentAsync(
                    id, client, path, _settings.ApiRootUri, settings, login);

                if (error != null)
                {
                    CliAppContext.Logger?.LogError(error);
                    AnsiConsole.MarkupLine($"[red]{error}[/]");
                    return 2;
                }
            }
        }

        string info = "Upload complete: " + count;
        CliAppContext.Logger?.LogInformation(info);
        AnsiConsole.MarkupLine($"[green]{info}.[/]");

        return 0;
    }
}

internal sealed class UploadCommandSettings : AuthCommandSettings
{
    [CommandArgument(0, "<INPUT_DIR>")]
    [Description("The input directory")]
    public string? InputDir { get; set; }

    [CommandArgument(1, "<FILE_MASK>")]
    [Description("The input files mask")]
    public string? FileMask { get; set; }

    [CommandOption("-x|--regex")]
    [Description("Use a regular expression pattern for the files mask")]
    public bool IsRegexMask { get; set; }

    [CommandOption("-r|--recurse")]
    [Description("Recurse subdirectories")]
    public bool IsRecursive { get; set; }

    [CommandOption("-t|--type <MIME_TYPE>")]
    [Description("The MIME type for all the files to upload")]
    public string? MimeType { get; set; }

    [CommandOption("-e|--extlist <TYPES_FILE_PATH>")]
    [Description("The list of common file extensions with their MIME types; " +
        "used when no MIME type is specified with -t")]
    public string? MimeTypeList { get; set; }

    [CommandOption("-m|--meta <EXTENSION>")]
    [Description("The extension to replace to that of the content filename " +
        "to build the correspondent metadata filename")]
    [DefaultValue(".meta")]
    public string MetaExtension { get; set; }

    [CommandOption("--metapfx <PREFIX>")]
    [Description("The prefix inserted before the content filename's extension " +
        "to build the correspondent metadata filename")]
    public string? MetaPrefix { get; set; }

    [CommandOption("--metasfx <SUFFIX>")]
    [Description("The suffix appended after the content filename's extension " +
        "to represent its metadata in a correspondent file")]
    public string? MetaSuffix { get; set; }

    [CommandOption("--metasep <SEPARATOR>")]
    [Description("The separator used in delimited metadata files")]
    [DefaultValue(",")]
    public string MetaDelimiter { get; set; }

    [CommandOption("--idsep <SEPARATOR>")]
    [Description("The conventional separator used in BLOB IDs for virtual folders.")]
    [DefaultValue("|")]
    public string IdDelimiter { get; set; }

    [CommandOption("-c|--check")]
    [Description("Check for file change before uploading")]
    public bool IsCheckEnabled { get; set; }

    [CommandOption("-d|--preflight|--dry")]
    [Description("Preflight mode: do not write to database")]
    public bool IsDryRun { get; set; }

    public UploadCommandSettings()
    {
        MetaExtension = ".meta";
        MetaDelimiter = ",";
        IdDelimiter = "|";
    }
}
