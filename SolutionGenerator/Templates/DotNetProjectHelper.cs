using SolutionGen.Compiling.Model;

namespace SolutionGen.Templates
{
    public partial class DotNetProject
    {
        public Solution Solution { get; set; }
        public Module Module { get; set; }
        public Project Project { get; set; }
    }
}