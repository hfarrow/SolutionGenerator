using System.Collections.Generic;

namespace SolutionGen.Generator.Model
{
    public interface IPath
    {
        string Value { get; }
    }

    public class LiteralPath : IPath
    {
        public string Value { get; }
        
        public LiteralPath(string value)
        {
            Value = value;
        }
    }

    public class GlobPath : IPath
    {
        public string Value { get; }
        
        public GlobPath(string value)
        {
            Value = value;
        }
    }
}