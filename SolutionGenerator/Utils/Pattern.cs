using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SolutionGen.Generator.Model;
using GLOB = Glob.Glob;

namespace SolutionGen.Utils
{
    public interface IPattern : IExpandable
    {
        string Value { get; }
        bool Negated { get; }

        IEnumerable<string> FilterMatches(IEnumerable<string> candidates);
        bool IsMatch(string candidate);
    }

    public abstract class Pattern : IPattern
    {
        public string Value { get; private set; }
        public bool Negated { get; }
        protected abstract bool CheckMatch(string candidate);
        
        protected Pattern(string value, bool negated)
        {
            Value = value;
            Negated = negated;
        }

        public IEnumerable<string> FilterMatches(IEnumerable<string> candidates)
        {
            return candidates.Where(IsMatch);
        }

        public bool IsMatch(string candidate)
        {
            return Negated ^ CheckMatch((candidate));
        }

        public void ExpandVariableInPlace(string varName, string varExpansion)
        {
            ExpandableVars.ExpandInCopy(Value, varName, varExpansion, out object value);
            Value = (string) value;
        }

        public bool ExpandVairableInCopy(string varName, string varExpansion, out IExpandable outCopy)
        {
            bool didCopy = false;
            Pattern newCopy = Copy();
            if (ExpandableVars.ExpandInCopy(Value, varName, varExpansion, out object value))
            {
                didCopy = true;
                newCopy.Value = (string) value;
            }
            outCopy = newCopy;
            return didCopy;
        }

        public bool StripEscapedVariablesInCopy(out IExpandable outCopy)
        {
            bool didStrip = false;
            Pattern newCopy = Copy();
            if (ExpandableVars.StripEscapedVariablesInCopy(Value, out object value))
            {
                didStrip = true;
                newCopy.Value = (string) value;
            }

            outCopy = newCopy;
            return didStrip;

        }

        protected abstract Pattern Copy();

        public override string ToString()
        {
            return (Negated ? "!" : "") + Value;
        }
    }

    [Serializable]
    public class LiteralPattern : Pattern
    {
        public LiteralPattern(string value, bool negated)
            : base(value, negated)
        {
        }

        protected override bool CheckMatch(string candidate)
        {
            return Value == candidate;
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
        public GLOB Glob { get; }
        
        public GlobPattern(string value, bool negated)
            : base(value, negated)
        {
            Glob = new GLOB(value);
        }

        protected override bool CheckMatch(string candidate)
        {
            return Glob.IsMatch(candidate);
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
        
        public RegexPattern(string regexPattern, bool negated)
            : base(regexPattern, negated)
        {
            Regex = new Regex(regexPattern);
        }

        protected override bool CheckMatch(string candidate)
        {
            return Regex.IsMatch(candidate);
        }

        protected override Pattern Copy()
        {
            return new RegexPattern(Value, Negated);
        }
        
        public override string ToString()
        {
            return $"{(Negated ? "!" : "")}regex \"{Value}\"";
        }
    }
}