using System.Collections.Generic;
using System.Linq;

namespace SolutionGen.Parser.Model
{
    public class ArrayValue : ValueElement
    {
        public IEnumerable<ValueElement> Values { get; }
        public ArrayValue(IEnumerable<ValueElement> values) : base(values.ToArray())
        {
            Values = (IEnumerable<ValueElement>) Value;
        }

        public override string ToString()
        {
            return $"[{string.Join(", ", Values)}]";
        }
    }
}