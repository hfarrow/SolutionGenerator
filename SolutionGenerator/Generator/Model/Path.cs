using System;
using System.Text.RegularExpressions;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Model
{
    public interface IPattern : IExpandable
    {
        string Value { get; }
        bool Negated { get; }
    }

    public abstract class Pattern : IPattern
    {
        public string Value { get; private set; }
        public bool Negated { get; }

        protected Pattern(string value, bool negated)
        {
            Value = value;
            Negated = negated;
        }

        public void ExpandVariableInPlace(string varName, string varExpansion)
        {
            ExpandableVar.ExpandInCopy(Value, varName, varExpansion, out object value);
            Value = (string) value;
        }

        public bool ExpandVairableInCopy(string varName, string varExpansion, out IExpandable outCopy)
        {
            bool didCopy = false;
            Pattern newCopy = Copy();
            if (ExpandableVar.ExpandInCopy(Value, varName, varExpansion, out object value))
            {
                didCopy = true;
                newCopy.Value = (string) value;
            }
            outCopy = newCopy;
            return didCopy;
        }

        protected abstract Pattern Copy();
    }

    [Serializable]
    public class LiteralPattern : Pattern
    {
        public LiteralPattern(string value, bool negated)
            : base(value, negated)
        {
        }

        protected override Pattern Copy()
        {
            return new LiteralPattern(Value, Negated);
        }

        public override string ToString()
        {
            return $"{(Negated ? "!" : "")}{Value}";
        }
    }

    [Serializable]
    public class GlobPattern : Pattern
    {
        public GlobPattern(string value, bool negated)
            : base(value, negated)
        {
        }

        protected override Pattern Copy()
        {
            return new GlobPattern(Value, Negated);
        }
        
        public override string ToString()
        {
            return $"{(Negated ? "!" : "")}glob \"{Value}\"";
        }
    }
    
    [Serializable]
    public class RegexPattern : Pattern
    {
        public Regex Regex { get; }
        
        public RegexPattern(string regexStr, Regex regex, bool negated)
            : base(regexStr, negated)
        {
            Regex = regex;
        }

        protected override Pattern Copy()
        {
            return new RegexPattern(Value, Regex, Negated);
        }
        
        public override string ToString()
        {
            return $"{(Negated ? "!" : "")}regex \"{Value}\"";
        }
    }
}