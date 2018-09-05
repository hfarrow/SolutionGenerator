using System;
using System.IO;
using System.Linq;
using System.Reflection;
using McMaster.Extensions.CommandLineUtils;
using SolutionGen.Console.Commands;
using SolutionGen.Utils;

namespace SolutionGen.Console
{
    [Command(Description = "A C# solution generator tool"),
     Subcommand("gen", typeof(GenerateCommand)),
     Subcommand("build", typeof(BuildCommand)),
     Subcommand("open", typeof(OpenCommand)),
     VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    internal class Console : Command
    {
        private static int Main(string[] args)
        {
            int code = CommandLineApplication.Execute<Console>(args);
            Log.ScopedTimer.LogResults(Log.Level.Debug);
            Log.ScopedTimer.ClearResults();
            if (code != ErrorCode.Success)
            {
                Log.Error("Terminating with error code: {0}", code);
            }
            return code;
        }

        private static string GetVersion() => typeof(Console).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            .InformationalVersion;

        protected override int OnExecute(CommandLineApplication app, IConsole console)
        {
            Log.Error("You must specify a subcommand.");
            app.ShowHelp();
            return 1;
        }
    }
}