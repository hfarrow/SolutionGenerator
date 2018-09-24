
using System.Collections.Generic;

namespace SolutionGen.Parser.Model
{
    public abstract class ConfigElement
    {
        public string ConditionalExpression { get; }
        public ConfigElement ParentElement { get; internal set; }
        
        protected ConfigElement(string conditionalExpression)
        {
            ConditionalExpression = conditionalExpression;
        }
    }
}