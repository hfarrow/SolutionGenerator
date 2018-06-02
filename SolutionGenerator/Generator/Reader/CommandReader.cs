using System.Collections.Generic;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Reader
{
    public class CommandReader
        : ElementReader<CommandElement, Model.CommandDefinition>
    {
        protected override IResult<IEnumerable<object>> Read(CommandElement element,
            Model.CommandDefinition definition)
        {
            return new Result(definition.Command());
        }
    }
}