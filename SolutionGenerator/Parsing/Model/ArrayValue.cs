using System.Collections.Generic;

namespace SolutionGen.Parsing.Model
{
    public class ArrayValue : ValueElement
    {
        public IEnumerable<ValueElement> Values { get; }
        public ArrayValue(IEnumerable<ValueElement> values) : base(values)
        {
            Values = values;
        }

        public override string ToString()
        {
            return $"[{string.Join(", ", Values)}]";
        }
    }
}