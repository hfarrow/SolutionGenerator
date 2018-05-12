using System.Collections.Generic;
using System.Linq;
using SolutionGen.Compiling.Model;

namespace SolutionGen.Templates
{
    public partial class DotNetProject
    {
        public Solution Solution { get; set; }
        public Module Module { get; set; }
        public Project Project { get; set; }

        public string DefaultConfiguration
        {
            get
            {
                string group = Solution.ActiveConfigurationGroup;
                if (string.IsNullOrEmpty(group))
                {
                    group = Solution.ConfigurationGroups.First().Key;
                }

                return Solution.ConfigurationGroups[group].Configurations.First().Key;
            }
        }

        public string ProjectGuid => Project.guid.ToString();

        public string DefaultPlatform =>
            Solution.Settings.GetProperty<HashSet<object>>("target platforms").First().ToString();
        
        public string RootNamespace => Solution.Settings.GetProperty<string>("root namespace");

        public string TargetFrameworkVersion =>
            Project.GetConfiguration(DefaultConfiguration).GetProperty<string>("target framework");

        public string LanguageVersion =>
            Project.GetConfiguration(DefaultConfiguration).GetProperty<string>("language version");
    }
}