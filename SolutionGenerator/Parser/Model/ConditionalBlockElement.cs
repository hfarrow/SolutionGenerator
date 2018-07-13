using System.Collections.Generic;

namespace SolutionGen.Parser.Model
{
    public class ConditionalBlockElement : ConfigElement
    {
        public IEnumerable<ConfigElement> Elements { get; }
        
        public ConditionalBlockElement(string conditionalExpression, IEnumerable<ConfigElement> elements)
            : base(conditionalExpression)
        {
            Elements = elements;
        }
        
        public override string ToString()
        {
            return $"ConditionalBlock{{{ConditionalExpression}}}";
        }
    }
}