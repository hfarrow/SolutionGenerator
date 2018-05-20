namespace SolutionGen.Parser.Model
{
    public class GlobValue : ValueElement
    {
        public string GlobStr { get; }
        
        public GlobValue(string value) : base(value)
        {
            GlobStr = value;
        }
    }
}