﻿using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    public sealed class RootCommand : ICommand
    {
        private readonly CommandLineApplication _app;

        public RootCommand(CommandLineApplication app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public static void Configure(CommandLineApplication app, AppOptions options)
        {
            // configure all the app commands here
            app.Command("list", c => ListCommand.Configure(c, options));
            app.Command("upload", c => UploadCommand.Configure(c, options));
            app.Command("download", c => DownloadCommand.Configure(c, options));
            app.Command("get-info", c => GetInfoCommand.Configure(c, options));

            app.OnExecute(() =>
            {
                options.Command = new RootCommand(app);
                return 0;
            });
        }

        public Task<int> Run()
        {
            _app.ShowHelp();
            return Task.FromResult(0);
        }
    }
}
