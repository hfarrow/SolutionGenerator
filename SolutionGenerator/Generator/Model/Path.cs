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
            Value = (string) ExpandableVar.ExpandInCopy(Value, varName, varExpansion);
        }

        public IExpandable ExpandVairableInCopy(string varName, string varExpansion)
        {
            Path copy = Copy();
            copy.Value = (string) ExpandableVar.ExpandInCopy(Value, varName, varExpansion);
            return copy;
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