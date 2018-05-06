namespace SolutionGenerator.Parsing.Model
{
    public class CommandElement : ConfigElement
    {
        public string CommandName { get; }
        
        public CommandElement(string commandName, string conditionalExpression)
            : base(conditionalExpression)
        {
            CommandName = commandName;
        }

        public override string ToString()
        {
            return $"Command{{{CommandName}}}";
        }
    }
}