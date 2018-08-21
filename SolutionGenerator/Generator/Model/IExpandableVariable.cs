namespace SolutionGen.Generator.Model
{
    public interface IExpandable
    {
        void ExpandVariableInPlace(string varName, string varExpansion);
        bool ExpandVairableInCopy(string varName, string varExpansion, out IExpandable copy);
        bool StripEscapedVariablesInCopy(out IExpandable copy);
    }
}