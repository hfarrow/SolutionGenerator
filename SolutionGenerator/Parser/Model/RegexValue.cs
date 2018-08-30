using System.Text.RegularExpressions;

namespace SolutionGen.Parser.Model
{
    public class RegexValue : ValueElement
    {
        public string RegexPattern { get; }
        public Regex Regex { get; }
        public bool Negated { get; }
        
        public RegexValue(string value, bool negated) : base(value)
        {
            RegexPattern = value;
            Regex = new Regex(RegexPattern, RegexOptions.Compiled);
            Negated = negated;
        }
        
        public override string ToString()
        {
            return $"{(Negated ? "!" : "")}regex {RegexPattern}";
        }
    }
}