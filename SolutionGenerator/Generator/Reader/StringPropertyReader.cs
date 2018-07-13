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
                    values.AddRange(arrayValue.Values
                        .Where(arrayElement => arrayElement.Value != null)
                        .Select(arrayElement => arrayElement.Value.ToString()));
                    break;
                
                // Ensure single line property was not 'none'
                case ValueElement valueElement when valueElement.Value != null:
                    values.Add(valueElement.Value.ToString());
                    break;
            }
            
            return new Result<IEnumerable<string>>(false, values);
        }
    }
}