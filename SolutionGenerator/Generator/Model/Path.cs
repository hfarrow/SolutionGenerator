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
        
        public void ExpandVariable(string varName, string varExpansion)
        {
            object expanded = ExpandableVar.Expand(Value, varName, varExpansion);
            if (expanded != null)
            {
                Value = expanded.ToString();
            }
        }
    }

    [Serializable]
    public class LiteralPath : Path
    {
        public LiteralPath(string value)
            : base(value)
        {
        }
    }

    [Serializable]
    public class GlobPath : Path
    {
        public GlobPath(string value)
            : base(value)
        {
        }
    }
}