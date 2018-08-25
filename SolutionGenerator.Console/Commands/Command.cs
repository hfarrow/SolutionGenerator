using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SolutionGen.Utils;

namespace SolutionGen.Console.Commands
{
    public class Command
    {
        private const string DEFAULT_CONFIG_EXT = ".scfg";
        
        [Option("-l|--log-level", CommandOptionType.SingleValue)]
        public string LogLevel { get; set; }
        
        [Option("-x|--config-ext", CommandOptionType.SingleValue,
            Description = "Override the default config document extension (" + DEFAULT_CONFIG_EXT +") with the " +
                          "provided extension.")]
        public string ConfigExt { get; set; }

        [Option("-c|--config-path|--config",
             Description =
                 "Path to the main solution configruation document or a directory containing a solution config " +
                 "document (" + DEFAULT_CONFIG_EXT + ")"),
         FileOrDirectoryExists]
        public string ConfigPath { get; set; }

        protected IConsole Console { get; private set; }
        protected CommandLineApplication App { get; private set; }
        protected FileInfo SolutionConfigFile { get; private set; }
        private Stopwatch timer = Stopwatch.StartNew();
        
        protected virtual int OnExecute(CommandLineApplication app, IConsole console)
        {
            Console = console;
            App = app;
            return new Func<ErrorCode>[]
                {
                    InitLogging,
                    FindSolutionConfigFile,
                    SetWorkingDirectory,
                    () => LogDuration(typeof(Command).Name),
                }
                .Select(step => step())
                .Any(errorCode => errorCode != ErrorCode.Success) ? ErrorCode.CliError : ErrorCode.Success;
        }

        private ErrorCode InitLogging()
        {
            if (string.IsNullOrEmpty(LogLevel))
            {
                // Use current default
                LogLevel = Log.LogLevel.ToString();
            }
            
            if (!Enum.TryParse(LogLevel, out Log.Level level))
            {
                Log.Error("Failed to parse log level '{0}'. Valid levels are [{1}]",
                    LogLevel, string.Join(", ", Enum.GetNames(typeof(Log.Level))));
                return ErrorCode.CliError;
            }
            
            Log.LogLevel = level;
            return ErrorCode.Success;
        }

        private void InitConfigExt()
        {
            if (string.IsNullOrEmpty(ConfigExt))
            {
                ConfigExt = DEFAULT_CONFIG_EXT;
            }
            else if(!ConfigExt.StartsWith('.'))
            {
                ConfigExt = '.' + ConfigExt;
            }
        }
        
        protected ErrorCode FindSolutionConfigFile()
        {
            if (SolutionConfigFile != null)
            {
                // Was already found
                return ErrorCode.Success;
            }
            
            InitConfigExt();
            
            if (string.IsNullOrEmpty(ConfigPath))
            {
                ConfigPath = "./";
            }
            
            if (File.GetAttributes(ConfigPath).HasFlag(FileAttributes.Directory))
            {
                var dir = new DirectoryInfo(ConfigPath);
                FileInfo[] candidates = dir.GetFiles("*" + ConfigExt, SearchOption.TopDirectoryOnly);
                if (candidates.Length == 0)
                {
                    Log.Error("Failed to find a solution config document with extension '{0}' in directory '{1}'",
                        ConfigExt, dir.FullName);
                    Log.Error("Use the --config-ext option to change the expected file extension or provide the config file " +
                          "directly instead of a directory");

                    return ErrorCode.CliError;
                }
                
                if(candidates.Length > 1)
                {
                    Log.Warn("Found multiple solution config documents with extension '{0}' in directory '{1}':",
                        ConfigExt, dir.FullName);
                    Log.IndentedCollection(candidates, c => c.FullName, Log.Info);
                }

                SolutionConfigFile = candidates.First();
            }
            else
            {
                SolutionConfigFile = new FileInfo(ConfigPath);
            }
            
            return ErrorCode.Success;
        }
        
        private ErrorCode SetWorkingDirectory()
        {
            if (SolutionConfigFile?.Directory == null)
            {
                Log.Error(
                    "Cannot set working directory to config directory because a valid config could not be found.");
                return ErrorCode.CliError;
            }
            
            Directory.SetCurrentDirectory(SolutionConfigFile.Directory.FullName);
            return ErrorCode.Success;
        }

        protected ErrorCode LogDuration(string description)
        {
            Log.Timer(Log.Level.Info, description, timer);
            return ErrorCode.Success;
        }
    }
}