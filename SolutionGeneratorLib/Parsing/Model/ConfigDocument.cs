using System.Collections.Generic;
using System.Linq;

namespace SolutionGenerator.Parsing.Model
{
    public class ConfigDocument
    {
        public Dictionary<string, ConfigObject> Objects { get; }

        public ConfigDocument(IEnumerable<ConfigObject> objects)
        {
            Objects = objects.ToDictionary(o => o.Heading.Name, o => o);
        }
    }
}