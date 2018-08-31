using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SolutionGen.Generator.Model;
using SolutionGen.Utils;

namespace SolutionGen.Console.Commands
{
    [Command("open",
        Description = "Open the solution with the default association for .sln. Solution will be generated if it does not exist.")]
    public class OpenCommand : GenerateCommand
    {
        [Option("-g|--gen|--generate", CommandOptionType.NoValue)]
        public bool Generate { get; set; }

        protected override int OnExecute(CommandLineApplication app, IConsole console)
        {
            // normally logging in initialized in base.OnExecute but we need it before that runs.
            InitLogging();
            
            ErrorCode foundConfig = CheckGenerateSolution(Generate);
            if (foundConfig != ErrorCode.Success)
            {
                return foundConfig;
            }

            return new Func<ErrorCode>[]
                {
                    () => base.OnExecute(app, console),
                    OpenSolution,
                    () => LogDuration(typeof(BuildCommand).Name),
                }
                .Select(step => step())
                .Any(errorCode => errorCode != ErrorCode.Success)
                ? ErrorCode.CliError
                : ErrorCode.Success;
        }
        
        private ErrorCode OpenSolution()
        {
            Solution solution = GetGenerator().Solution;
            var file = new FileInfo(Path.Combine(solution.OutputDir, solution.Name + ".sln"));
            if (!file.Exists)
            {
                Log.Error("Could not find a solution file to open. Was it generated?");
                return ErrorCode.CliError;
            }

            try
            {
                string command = ExpandableVar.ExpandAllInString(solution.OpenCommand);
                string process = command;
                string args = "";
                int argsIndex = command.IndexOf(' ') + 1;
                if (argsIndex > 1)
                {
                    process = command.Substring(0, argsIndex - 1);
                    args = command.Substring(argsIndex);
                }

                var psi = new ProcessStartInfo(process, args)
                {
                    WorkingDirectory = Directory.GetCurrentDirectory()
                };
                ShellUtil.StartProcess(psi, ConsoleOutputHandler, ConsoleErrorHandler, true, true, 1);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to execute open command. See exception below.");
                Log.Error(ex.ToString());
                return ErrorCode.CliError;
            }

            return ErrorCode.Success;
        }
        
        private static void ConsoleOutputHandler(object sendingProcess, DataReceivedEventArgs line)
        {
            Log.Info(line.Data.TrimEnd().TrimEnd('\r', '\n'));
        }

        private static void ConsoleErrorHandler(object sendingProcess, DataReceivedEventArgs line)
        {
            Log.Error(line.Data.TrimEnd().TrimEnd('\r', '\n'));
        }
    }
}