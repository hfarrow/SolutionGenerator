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

        public string ProjectGuid => Project.Guid.ToString().ToUpper();
        public string DefaultPlatform => Solution.TargetPlatforms.First();
        public string RootNamespace => Solution.Settings.GetProperty<string>(Settings.PROP_ROOT_NAMESPACE);

        public string TargetFrameworkVersion =>
            Project.GetConfiguration(DefaultConfiguration).GetProperty<string>(Settings.PROP_TARGET_FRAMEWORK);

        public string LanguageVersion =>
            Project.GetConfiguration(DefaultConfiguration).GetProperty<string>(Settings.PROP_LANGUAGE_VERSION);
    }
}