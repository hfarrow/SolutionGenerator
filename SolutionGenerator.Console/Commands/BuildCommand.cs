﻿using System;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SolutionGen.Builder;
using SolutionGen.Generator.Model;

namespace SolutionGen.Console.Commands
{
    [Command("build",
        Description = "Build the solution. Solution will be generated if it does not exists.")]
    public class BuildCommand : GenerateCommand
    {
        [Option("-g|--gen|--generate", CommandOptionType.NoValue)]
        public bool Generate { get; set; }

        protected override int OnExecute(CommandLineApplication app, IConsole console)
        {
            // normally logging in initialized in base.OnExecute but we need it before that runs.
            InitLogging();
            
            ErrorCode foundConfig = CheckGenerateSolution();
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

        private ErrorCode CheckGenerateSolution()
        {
            ErrorCode foundConfig = FindSolutionConfigFile();
            if(!Generate &&
               foundConfig == ErrorCode.Success &&
               File.Exists(GetGenerator().Solution.Name + ".sln"))
            {
                SkipGenerateCommand = true;
            }

            return foundConfig;
        }

        private ErrorCode BuildSolution()
        {
            Solution solution = GetGenerator().Solution;
            var builder = new SolutionBuilder(solution, MasterConfiguration);
            
            builder.BuildDefaultConfiguration();
            // TODO: add cli arg for configaration and then find the configuration
            // in GetSolution()
            
            return ErrorCode.Success;
        }
    }
}