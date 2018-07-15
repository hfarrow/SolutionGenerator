using System.Text.RegularExpressions;

namespace SolutionGen.Parser.Model
{
    public class RegexValue : ValueElement
    {
        public string RegexStr { get; }
        public Regex Regex { get; }
        public bool Negated { get; }
        
        public RegexValue(string value, bool negated) : base(value)
        {
            RegexStr = value;
            Regex = new Regex(negated ? $"^(?!{RegexStr})$" : RegexStr);
            Negated = negated;
        }
        
        public override string ToString()
        {
            return $"{(Negated ? "!" : "")}regex {RegexStr}";
        }
    }
}