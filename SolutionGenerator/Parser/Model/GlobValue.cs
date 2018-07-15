namespace SolutionGen.Parser.Model
{
    public class GlobValue : ValueElement
    {
        public string GlobStr { get; }
        public bool Negated { get; }
        
        public GlobValue(string value, bool negated) : base(value)
        {
            GlobStr = value;
            Negated = negated;
        }
        
        public override string ToString()
        {
            return $"{(Negated ? "!" : "")}glob {Value}";
        }
    }
}