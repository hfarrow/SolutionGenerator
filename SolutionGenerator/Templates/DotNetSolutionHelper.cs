using SolutionGen.Generator.ModelOld;

namespace SolutionGen.Templates
{
    public partial class DotNetSolution
    {
        public Solution Solution { get; set; }

        public string SolutionGuid => Solution.Guid.ToString().ToUpper();
        public string GetProjectGuid(Project project) => project.Guid.ToString().ToUpper();
    }
}