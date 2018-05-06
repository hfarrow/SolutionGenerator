using SolutionGenerator.Parsing.Model;

namespace SolutionGenerator.Compiling.Model
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