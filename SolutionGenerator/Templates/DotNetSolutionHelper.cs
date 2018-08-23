using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;

namespace SolutionGen.Templates
{
    public partial class DotNetSolution
    {
        public Solution Solution { get; set; }
        public Dictionary<string, Module> Modules { get; set; }
        public SolutionGenerator Generator { get; set; }
        public ConfigurationGroup ActiveConfigurationGroup { get; set; }
        public string ProjectNamePostfix { get; set; }
        public HashSet<string> ProjectWhitelist { get; set; }

        public IReadOnlyCollection<string> ActiveConfigurations =>
            Solution.ConfigurationGroups[Generator.MasterConfiguration].Configurations.Keys
                .ToArray();

        public IEnumerable<Project> GetProjects()
        {
            Configuration config = ActiveConfigurationGroup.Configurations.Values.First();
            return Modules.Values
                .SelectMany(m => m.Configurations[config].Projects.Values)
                .Where(p => ProjectWhitelist.Contains(p.Name));
        }
    }
}