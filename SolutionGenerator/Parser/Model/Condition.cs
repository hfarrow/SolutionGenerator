namespace SolutionGen.Parser.Model
{
    public class Condition
    {
        public string Expression { get; }
        
        public Condition(string expression)
        {
            Expression = expression;
        }

        public bool Evaluate()
        {
            return true;
        }
    }
}