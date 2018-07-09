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

        public string SolutionGuid => Solution.Guid.ToString().ToUpper();
        
        public IReadOnlyCollection<string> ActiveConfigurations => Solution.Settings
            .ConfigurationGroups[Generator.ActiveConfigurationGroup].Configurations.Keys.ToArray();
    }
}