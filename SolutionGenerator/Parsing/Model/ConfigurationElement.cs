using System.Collections.Generic;
using System.Linq;

namespace SolutionGen.Parsing.Model
{
    public class ConfigurationElement : CommandElement
    {
        public string ConfigurationName { get; }
        public IReadOnlyDictionary<string, HashSet<string>> Configurations { get; }

        public ConfigurationElement(string configurationName, IEnumerable<KeyValuePair> values)
            : base("configuration", "true")
        {
            ConfigurationName = configurationName;

            Configurations = values.ToDictionary(
                kvp => kvp.PairKey,
                kvp => new HashSet<string>(kvp.PairValue.Value.ToString().Split(',')));

            foreach (HashSet<string> defineConstants in Configurations.Values)
            {
                defineConstants.Add(configurationName);
            }
        }

        public override string ToString()
        {
            return $"Configuration{{{ConfigurationName}}}";
        }
    }
}