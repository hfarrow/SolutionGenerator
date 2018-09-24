using System.Collections.Generic;

namespace SolutionGen.Parser.Model
{
    public class BlockElement : ContainerElement
    {
        public BlockElement(string conditionalExpression, IEnumerable<ConfigElement> children)
            : base(children, conditionalExpression)
        {
        }
        
        public override string ToString()
        {
            return $"Block{{{ConditionalExpression}}}";
        }
    }
}