using System.Collections.Generic;

namespace SolutionGenerator.Parsing.Model
{
    public class KeyValuePair : PropertyValue
    {
        public string PairKey { get; }
        public PropertyValue PairValue { get; }
        
        public KeyValuePair(string pairKey, PropertyValue pairValue)
            : base(new KeyValuePair<string, object>(pairKey, pairValue))
        {
            PairKey = pairKey;
            PairValue = pairValue;
        }
    }
}