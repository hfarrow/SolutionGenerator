using System.Collections.Generic;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Reader
{
    public class PathPropertyReader
        : ElementReader<PropertyElement, PropertyDefinition>
    {
        protected override IResult<IEnumerable<object>> Read(
            PropertyElement element,
            PropertyDefinition definition)
        {
            var values = new List<IPath>();
            switch (element.ValueElement)
            {
                case GlobValue glob:
                    values.Add(new GlobPath(glob.GlobStr));
                    break;
                
                case ArrayValue arrayValue:
                    foreach (ValueElement arrayElement in arrayValue.Values)
                    {
                        if (arrayElement is GlobValue glob)
                        {
                            values.Add(new GlobPath(glob.GlobStr));
                        }
                        else
                        {
                            values.Add(new LiteralPath(arrayElement.Value.ToString()));
                        }
                    }
                    break;
            }
            
            return new Result<IEnumerable<IPath>>(false, values);
        }
    }
}