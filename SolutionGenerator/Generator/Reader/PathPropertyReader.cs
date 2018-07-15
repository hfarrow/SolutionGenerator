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
                    values.Add(new GlobPath(glob.GlobStr, glob.Negated));
                    break;
                
                case RegexValue regex:
                    values.Add(new RegexPath(regex.RegexStr, regex.Regex, regex.Negated));
                    break;
                
                case ArrayValue arrayValue:
                    foreach (ValueElement arrayElement in arrayValue.Values)
                    {
                        switch (arrayElement)
                        {
                            case GlobValue glob:
                                values.Add(new GlobPath(glob.GlobStr, glob.Negated));
                                break;
                            case RegexValue regex:
                                values.Add(new RegexPath(regex.RegexStr, regex.Regex, regex.Negated));
                                break;
                            case ValueElement strElement when strElement.Value is string str:
                                values.Add(MakeLiteralPath(str));
                                break;
                        }
                    }
                    break;
                
                case ValueElement strElement when strElement.Value is string str:
                    values.Add(MakeLiteralPath(str));
                    break;
            }
            
            return new Result<IEnumerable<IPath>>(false, values);
        }

        private static LiteralPath MakeLiteralPath(string str)
        {
            bool negated = str.Trim().StartsWith('!');
            str = negated ? str.Substring(1) : str;
            return new LiteralPath(str, negated);
        }
    }
}