namespace SolutionGenerator.Parsing.Model
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