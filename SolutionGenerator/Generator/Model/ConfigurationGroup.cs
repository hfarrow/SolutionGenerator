using System.Collections.Generic;
using System.Linq;

namespace SolutionGen.Generator.Model
{
    public class ConfigurationGroup
    {
        public string Name { get; }
        public readonly IReadOnlyDictionary<string, Configuration> Configurations;

        public ConfigurationGroup(string name, IReadOnlyDictionary<string, Configuration> configurations)
        {
            Name = name;
            Configurations = configurations;
        }

        public override string ToString()
        {
            // Format: Configuration{GROUP_NAME_N:CONFIGURATION_N{CONDITIONALS}}
            return
                $"Configuration{{{Name}:{string.Join(',', Configurations.Select(kvp => $"{kvp.Key}{{{string.Join(',', kvp.Value.Conditionals)}}}"))}}}";
        }
    }
}