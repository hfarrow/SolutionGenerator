namespace SolutionGenerator.Parsing.Model
{
    public class CommentElement : ObjectElement
    {
        public string Comment { get; }

        public CommentElement(string comment)
            : base("true")
        {
            Comment = comment;
        }
    }
}