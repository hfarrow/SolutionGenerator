using SolutionGen.Parsing.Model;

namespace SolutionGen.Compiling.Model
{
    public class Solution
    {
        public ObjectElement SolutionElement { get; }
        
        public Solution(ObjectElement solutionElement)
        {
            SolutionElement = solutionElement;
        }

    }
}