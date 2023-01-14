using Spectre.Console.Cli;
using System;
using System.ComponentModel;

namespace SimpleBlob.Cli.Commands;

internal class ItemListSettings : AuthCommandSettings
{
    [CommandOption("-n|--pagenr <NUMBER>")]
    [Description("The page number (1-N)")]
    [DefaultValue(1)]
    public int PageNumber { get; set; }

    [CommandOption("-z|--pagesz <SIZE>")]
    [Description("The page size")]
    [DefaultValue(20)]
    public int PageSize { get; set; }

    [CommandOption("-i|--id <ITEM_ID>")]
    [Description("The ID filter (wildcards ? and *)")]
    public string? Id { get; set; }

    [CommandOption("-t|-m|--mime <MIME_TYPE>")]
    [Description("The MIME type")]
    public string? MimeType { get; set; }

    [CommandOption("--datemin <DATE>")]
    [Description("The minimum last-modified date")]
    public DateTime? MinDateModified { get; set; }

    [CommandOption("--datemax <DATE>")]
    [Description("The maximum last-modified date")]
    public DateTime? MaxDateModified { get; set; }

    [CommandOption("--szmin <SIZE>")]
    [Description("The minimum size")]
    public long MinSize { get; set; }

    [CommandOption("--szmax <SIZE>")]
    [Description("The maximum size")]
    public long MaxSize { get; set; }

    [CommandOption("-l|--lastuser <USER_NAME>")]
    [Description("The last user who modified the item")]
    public string? LastUserId { get; set; }

    [CommandOption("--props <PROPERTIES>")]
    [Description("The properties to match (CSV name=value)")]
    public string? Properties { get; set; }

    public ItemListSettings()
    {
        PageNumber = 1;
        PageSize = 20;
    }
}
