using Fusi.Cli.Commands;
using System;

namespace SimpleBlob.Cli.Commands
{
    public class ItemListOptions : AppCommandOptions
    {
        protected ItemListOptions(ICliAppContext context) : base(context)
        {
        }

        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? Id { get; set; }
        public string? MimeType { get; set; }
        public DateTime? MinDateModified { get; set; }
        public DateTime? MaxDateModified { get; set; }
        public long MinSize { get; set; }
        public long MaxSize { get; set; }
        public string? LastUserId { get; set; }
        public string? Properties { get; set; }
    }
}
