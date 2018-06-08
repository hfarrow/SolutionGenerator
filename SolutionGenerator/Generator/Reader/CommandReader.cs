using System.Collections.Generic;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Reader
{
    public class CommandReader
        : ElementReader<SimpleCommandElement, Model.CommandDefinition>
    {
        protected override IResult<IEnumerable<object>> Read(SimpleCommandElement element, Model.CommandDefinition definition)
        {
            return new Result(definition.Command(element));
        }
    }
}