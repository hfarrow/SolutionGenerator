using System.Collections.Generic;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;
using KeyValuePair = SolutionGen.Parser.Model.KeyValuePair;

namespace SolutionGen.Generator.Reader
{   
    public class StringPairPropertyReader
        : ElementReader<PropertyElement, PropertyDefinition>
    {
        protected override IResult<IEnumerable<object>> Read(PropertyElement element, PropertyDefinition definition)
        {
            var pairs = new List<Box<KeyValuePair<string, string>>>();
            switch (element.ValueElement)
            {
                case ArrayValue arrayValue:
                    foreach (ValueElement value in arrayValue.Values)
                    {
                        if (value is KeyValuePair arrayKvp && arrayKvp.PairValue != null)
                        {
                            pairs.Add(new Box<KeyValuePair<string, string>>(new KeyValuePair<string, string>(
                                arrayKvp.PairKey, arrayKvp.PairValue.ToString())));
                        }
                    }
                    break;
                
                default:
                    if (element.ValueElement is KeyValuePair kvp && kvp.PairValue != null)
                    {
                        pairs.Add(new Box<KeyValuePair<string, string>>(
                            new KeyValuePair<string, string>(kvp.PairKey, kvp.PairValue.ToString())));
                    }
                    break;
            }

            return new Result<IEnumerable<Box<KeyValuePair<string, string>>>>(false, pairs);
        }
    }
}