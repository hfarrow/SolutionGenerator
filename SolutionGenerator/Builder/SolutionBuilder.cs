using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Utils;

namespace SolutionGen.Builder
{
    public class SolutionBuilder
    {
        private readonly Solution solution;
        private readonly string masterConfiguration;

        public SolutionBuilder(Solution solution, string masterConfiguration)
        {
            this.solution = solution;
            this.masterConfiguration = masterConfiguration;
        }

        public void BuildAllConfigurations()
        {
            Log.Info("Building all solution configurations");
            using (new Disposable(
                new Log.ScopedIndent(),
                new Log.ScopedTimer(Log.Level.Info, "Build All Configurations")))
            {
                foreach (Configuration configuration in
                    solution.ConfigurationGroups[masterConfiguration].Configurations.Values)
                {
                    BuildConfiguration(configuration);
                }
            }
        }

        public void BuildDefaultConfiguration()
        {
            BuildConfiguration(solution.ConfigurationGroups[masterConfiguration].Configurations.Values.First());
        }

        public void BuildConfiguration(Configuration configuration)
        {
            Log.Info("Building solution configuration '{0} - {1}'", configuration.GroupName, configuration.Name);
            using (new Disposable(
                new Log.ScopedIndent(),
                new Log.ScopedTimer(Log.Level.Info, string.Format("Build Configuration '{0} - {1}'",
                    configuration.GroupName, configuration.Name)),
                new ExpandableVar.ScopedState()))
            {
                ExpandableVar.SetExpandableVariable(ExpandableVar.VAR_CONFIGURATION, configuration.Name);
                // TODO: execute command
            }
        }
    }
}