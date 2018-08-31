using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SolutionGen.Builder;
using SolutionGen.Generator.Model;
using SolutionGen.Utils;

namespace SolutionGen.Console.Commands
{
    [Command("build",
        Description = "Build the solution. Solution will be generated if it does not exist.")]
    public class BuildCommand : GenerateCommand
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
                    SetExpandableVars,
                    BuildSolution,
                    ClearExpandableVars,
                    () => LogDuration(typeof(BuildCommand).Name),
                }
                .Select(step => step())
                .Any(errorCode => errorCode != ErrorCode.Success)
                ? ErrorCode.CliError
                : ErrorCode.Success;
        }
        
        private ErrorCode BuildSolution()
        {
            Solution solution = GetGenerator().Solution;

            try
            {
                string[] properties =
                    File.ReadAllLines(Path.Combine(solution.OutputDir, solution.Name + ".sln.config"));
                Dictionary<string, string> propertiesLookup = properties
                    .Select(s => s.Split('='))
                    .ToDictionary(arr => arr[0], arr => arr[1]);
                
                Log.Debug("Loaded solution properties:");
                Log.IndentedCollection(propertiesLookup, kvp => $"{kvp.Key} = {kvp.Value}", Log.Debug);

                if (!propertiesLookup.TryGetValue("MasterConfiguration", out string cfg))
                {
                    Log.Warn("Failed to determine what mast configuration was used to generate the solution. " +
                             "Default master configuration will be used for build.");
                    cfg = MasterConfiguration;
                }
                
                var builder = new SolutionBuilder(solution, cfg);
                if (string.IsNullOrEmpty(BuildConfiguration))
                {
                    builder.BuildDefaultConfiguration();
                }
                else
                {
                    builder.BuildConfiguration(BuildConfiguration);
                }
            }
            catch (Exception ex)
            {
                // Error message should have been logged by builder already.
                // Just in case, log it at debug level.
                Log.Debug("Builder Exception: {0}", ex);
                return ErrorCode.GeneratorException;
            }

            return ErrorCode.Success;
        }
    }
}