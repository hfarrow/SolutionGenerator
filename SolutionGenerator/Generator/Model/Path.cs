using System;
using System.Text.RegularExpressions;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Model
{
    public interface IPath : IExpandable
    {
        string Value { get; }
        bool Negated { get; }
    }

    public abstract class Path : IPath
    {
        public string Value { get; private set; }
        public bool Negated { get; }

        protected Path(string value, bool negated)
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
            Path newCopy = Copy();
            if (ExpandableVar.ExpandInCopy(Value, varName, varExpansion, out object value))
            {
                didCopy = true;
                newCopy.Value = (string) value;
            }
            outCopy = newCopy;
            return didCopy;
        }

        protected abstract Path Copy();
    }

    [Serializable]
    public class LiteralPath : Path
    {
        public LiteralPath(string value, bool negated)
            : base(value, negated)
        {
        }

        protected override Path Copy()
        {
            return new LiteralPath(Value, Negated);
        }

        public override string ToString()
        {
            return $"{(Negated ? "!" : "")}{Value}";
        }
    }

    [Serializable]
    public class GlobPath : Path
    {
        public GlobPath(string value, bool negated)
            : base(value, negated)
        {
        }

        protected override Path Copy()
        {
            return new GlobPath(Value, Negated);
        }
        
        public override string ToString()
        {
            return $"{(Negated ? "!" : "")}glob \"{Value}\"";
        }
    }
    
    [Serializable]
    public class RegexPath : Path
    {
        public Regex Regex { get; }
        
        public RegexPath(string regexStr, Regex regex, bool negated)
            : base(regexStr, negated)
        {
            Regex = regex;
        }

        protected override Path Copy()
        {
            return new RegexPath(Value, Regex, Negated);
        }
        
        public override string ToString()
        {
            return $"{(Negated ? "!" : "")}regex \"{Value}\"";
        }
    }
}