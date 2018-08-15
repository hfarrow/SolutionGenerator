using System.Collections.Generic;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Reader
{
    public class CommandReader
        : ElementReader<SimpleCommandElement, CommandDefinition>
    {
        protected override IResult<IEnumerable<object>> Read(SimpleCommandElement element, CommandDefinition definition)
        {
            return new Result(definition.Command(element));
        }
    }
}