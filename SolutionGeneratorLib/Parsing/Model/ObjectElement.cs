using System.Collections.Generic;

namespace SolutionGenerator.Parsing.Model
{
    public abstract class ObjectElement
    {
        public string ConditionalExpression { get; }
        
        protected ObjectElement(string conditionalExpression)
        {
            ConditionalExpression = conditionalExpression;
        }
    }
}