using System;
using SolutionGen.Generator.Reader;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Model
{
    public class CommandDefinition
    {
        public string Name { get; }
        public Func<SimpleCommandElement, bool> Command { get; }
        public ElementReader<SimpleCommandElement, CommandDefinition> Reader { get; }

        public CommandDefinition(string name, Func<SimpleCommandElement, bool> command,
            ElementReader<SimpleCommandElement, CommandDefinition> reader)
        {
            Name = name;
            Command = command;
            Reader = reader;
        }
    }
    
    public class CommandDefinition<TReader> : CommandDefinition
        where TReader : ElementReader<SimpleCommandElement, CommandDefinition>, new()
    {
        public CommandDefinition(string name, Func<SimpleCommandElement, bool> command)
            : base(name, command, new TReader())
        {
        }
    }
}