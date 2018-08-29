using System.Collections.Generic;
using SolutionGen.Generator.Model;
using SolutionGen.Utils;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Reader
{
    public class PatternPropertyReader
        : ElementReader<PropertyElement, PropertyDefinition>
    {
        protected override IResult<IEnumerable<object>> Read(
            PropertyElement element,
            PropertyDefinition definition)
        {
            var values = new List<IPattern>();
            switch (element.ValueElement)
            {
                case GlobValue glob:
                    values.Add(new GlobPattern(glob.GlobStr, glob.Negated));
                    break;
                
                case RegexValue regex:
                    values.Add(new RegexPattern(regex.RegexPattern, regex.Negated));
                    break;
                
                case ArrayValue arrayValue:
                    foreach (ValueElement arrayElement in arrayValue.Values)
                    {
                        switch (arrayElement)
                        {
                            case GlobValue glob:
                                values.Add(new GlobPattern(glob.GlobStr, glob.Negated));
                                break;
                            case RegexValue regex:
                                values.Add(new RegexPattern(regex.RegexPattern, regex.Negated));
                                break;
                            case ValueElement strElement when strElement.Value is string str:
                                values.Add(MakeLiteralPattern(str));
                                break;
                        }
                    }
                    break;
                
                case ValueElement strElement when strElement.Value is string str:
                    values.Add(MakeLiteralPattern(str));
                    break;
            }
            
            return new Result<IEnumerable<IPattern>>(false, values);
        }

        public static LiteralPattern MakeLiteralPattern(string str)
        {
            bool negated = str.Trim().StartsWith('!');
            str = negated ? str.Substring(1) : str;
            return new LiteralPattern(str, negated);
        }
    }
}