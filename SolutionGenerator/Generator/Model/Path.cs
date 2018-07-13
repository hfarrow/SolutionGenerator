using System;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Model
{
    public interface IPath : IExpandable
    {
        string Value { get; }
    }

    public abstract class Path : IPath
    {
        public string Value { get; private set; }

        protected Path(string value)
        {
            Value = value;
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
        public LiteralPath(string value)
            : base(value)
        {
        }

        public override string ToString()
        {
            return Value;
        }

        protected override Path Copy()
        {
            return new LiteralPath(Value);
        }
    }

    [Serializable]
    public class GlobPath : Path
    {
        public GlobPath(string value)
            : base(value)
        {
        }

        public override string ToString()
        {
            return $"glob \"{Value}\"";
        }

        protected override Path Copy()
        {
            return new GlobPath(Value);
        }
    }
}