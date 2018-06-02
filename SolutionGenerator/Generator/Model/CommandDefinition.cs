using System;
using SolutionGen.Generator.Reader;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Model
{
    public class CommandDefinition
    {
        public string Name { get; }
        public Func<bool> Command { get; }
        public ElementReader<CommandElement, CommandDefinition> Reader { get; }

        public CommandDefinition(string name, Func<bool> command,
            ElementReader<CommandElement, CommandDefinition> reader)
        {
            Name = name;
            Command = command;
            Reader = reader;
        }
    }
    
    public class CommandDefinition<TReader> : CommandDefinition
        where TReader : ElementReader<CommandElement, CommandDefinition>, new()
    {
        public CommandDefinition(string name, Func<bool> command)
            : base(name, command, new TReader())
        {
        }
    }
}