using System.Collections.Generic;

namespace SolutionGenerator.Parsing.Model
{
    public class ArrayValue : PropertyValue
    {
        public IEnumerable<PropertyValue> Values { get; }
        public ArrayValue(IEnumerable<PropertyValue> values) : base(values)
        {
            Values = values;
        }
    }
}