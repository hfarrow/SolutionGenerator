using System;
using SolutionGenerator.Compiling.Model;
using SolutionGenerator.Parsing.Model;

namespace SolutionGenerator.Compiling
{
    public class CommandDefinition
    {
        public string Name { get; }
        public ElementCompiler<CommandElement, CommandDefinition> Compiler { get; }
        public Func<Settings, ElementCompiler.Result> CommandAction { get; }

        public CommandDefinition(string name, ElementCompiler<CommandElement, CommandDefinition> compiler,
            Func<Settings, ElementCompiler.Result> commandAction)
        {
            Name = name;
            Compiler = compiler;
            CommandAction = commandAction;
        }
    }

    public class CommandDefinition<TCompiler> : CommandDefinition
        where TCompiler : ElementCompiler<CommandElement, CommandDefinition>, new()
    {
        public CommandDefinition(string name, Func<Settings, ElementCompiler.Result> commandAction)
            : base(name, new TCompiler(), commandAction)
        {
        }
    }
}
