namespace SolutionGen.Parser.Model
{
    public class SimpleCommandElement : CommandElement
    {
        public string ArgumentStr { get; }
        
        public SimpleCommandElement(string commandName, string argumentStr, string conditionalExpression)
            : base(commandName, conditionalExpression)
        {
            ArgumentStr = argumentStr;
        }
    }
}