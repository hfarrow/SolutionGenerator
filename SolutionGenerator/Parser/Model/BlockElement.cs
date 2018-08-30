using System.Collections.Generic;

namespace SolutionGen.Parser.Model
{
    public class GroupElement : ConfigElement
    {
        public IEnumerable<ConfigElement> Elements { get; }
        
        public GroupElement(string conditionalExpression, IEnumerable<ConfigElement> elements)
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