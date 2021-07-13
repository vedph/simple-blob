using Fusi.Tools;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShellProgressBar;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleBlob.Cli.Commands
{
    public sealed class UploadCommand : ICommand
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        private readonly string _user;
        private readonly string _password;
        private readonly string _inputDir;
        private readonly string _fileMask;
        private readonly bool _regexMask;
        private readonly bool _recursive;

        public UploadCommand(string user, string password,
            string inputDir, string fileMask, bool regexMask,
            bool recursive)
        {
            _user = user;
            _password = password;
            _inputDir = inputDir;
            _fileMask = fileMask;
            _regexMask = regexMask;
            _recursive = recursive;
        }
    }
}
