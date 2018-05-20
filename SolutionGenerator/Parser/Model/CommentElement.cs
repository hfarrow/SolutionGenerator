namespace SolutionGen.Parser.Model
{
    public class CommentElement : ConfigElement
    {
        public string Comment { get; }

        public CommentElement(string comment)
            : base("true")
        {
            Comment = comment;
        }

        public override string ToString()
        {
            return $"Comment{{{Comment}}}";
        }
    }
}