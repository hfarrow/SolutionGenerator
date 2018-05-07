
namespace SolutionGen.Parsing.Model
{
    public abstract class ConfigElement
    {
        public string ConditionalExpression { get; }
        
        protected ConfigElement(string conditionalExpression)
        {
            ConditionalExpression = conditionalExpression;
        }
    }
}