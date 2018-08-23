﻿using System;
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
     VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    internal class Console : Command
    {
        private static void Main(string[] args) => CommandLineApplication.Execute<Console>(args);

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