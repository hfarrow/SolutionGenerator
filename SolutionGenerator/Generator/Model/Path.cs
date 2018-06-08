using System;

namespace SolutionGen.Generator.Model
{
    public interface IPath
    {
        string Value { get; }
    }

    [Serializable]
    public class LiteralPath : IPath
    {
        public string Value { get; }
        
        public LiteralPath(string value)
        {
            Value = value;
        }
    }

    [Serializable]
    public class GlobPath : IPath
    {
        public string Value { get; }
        
        public GlobPath(string value)
        {
            Value = value;
        }
    }
}