
namespace SolutionGen.Parser.Model
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