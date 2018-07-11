namespace SolutionGen.Generator.Model
{
    public interface IExpandable
    {
        void ExpandVariableInPlace(string varName, string varExpansion);
        IExpandable ExpandVairableInCopy(string varName, string varExpansion);
    }
}