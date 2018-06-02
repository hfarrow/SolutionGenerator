using System.Collections.Generic;

namespace SolutionGen.Generator.Model
{
    public class ConfigurationGroup
    {
        public string Name { get; }
        public readonly IReadOnlyDictionary<string, Configuration> Configurations;

        public ConfigurationGroup(string name, Dictionary<string, Configuration> configurations)
        {
            Name = name;
            Configurations = configurations;
        }

        public override string ToString()
        {
            return $"Configuration{{{Name}:{string.Join(',', Configurations.Keys)}}}";
        }
    }
}