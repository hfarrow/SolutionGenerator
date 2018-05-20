using System.Collections.Generic;

namespace SolutionGen.Parser.Model
{
    public class KeyValuePair : ValueElement
    {
        public string PairKey { get; }
        public ValueElement PairValue { get; }
        
        public KeyValuePair(string pairKey, ValueElement pairValue)
            : base(new KeyValuePair<string, object>(pairKey, pairValue))
        {
            PairKey = pairKey;
            PairValue = pairValue;
        }
    }
}