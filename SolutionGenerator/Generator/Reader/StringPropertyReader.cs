using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Reader
{
    public class StringPropertyReader
        : ElementReader<PropertyElement, PropertyDefinition>
    {
        protected override IResult<IEnumerable<object>> Read(PropertyElement element, PropertyDefinition definition)
        {
            var values = new List<string>();
            switch (element.ValueElement)
            {
                case ArrayValue arrayValue:
                    values.AddRange(arrayValue.Values.Select(arrayElement => arrayElement.Value.ToString()));
                    break;
                
                default:
                    values.Add(element.ValueElement.Value.ToString());
                    break;
            }
            
            return new Result<IEnumerable<string>>(false, values);
        }
    }
}