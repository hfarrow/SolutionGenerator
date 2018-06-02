using System.Collections.Generic;
using System.Linq;

namespace SolutionGen.Parser.Model
{
    public class ConfigurationGroupElement : CommandElement
    {
        public string ConfigurationGroupName { get; }
        public IReadOnlyDictionary<string, HashSet<string>> Configurations { get; }

        public ConfigurationGroupElement(string configurationGroupName, IEnumerable<KeyValuePair> values)
            : base("configuration", "true")
        {
            ConfigurationGroupName = configurationGroupName;

            Configurations = values.ToDictionary(
                kvp => kvp.PairKey,
                kvp => new HashSet<string>(kvp.PairValue.Value.ToString().Split(',')));

            foreach (HashSet<string> defineConstants in Configurations.Values)
            {
                defineConstants.Add(configurationGroupName);
            }
        }

        public override string ToString()
        {
            return $"Configuration{{{ConfigurationGroupName}:{string.Join(',', Configurations.Keys)}}}";
        }
    }
}