using System.Collections.Generic;

namespace SolutionGen.Generator.Model
{
    public class Configuration
    {
        public string Name { get; }
        public IReadOnlyCollection<string> Conditionals { get; }

        private readonly HashSet<string> conditionals;
        
        public Configuration(string name, HashSet<string> conditionals)
        {
            Name = name;
            this.conditionals = conditionals;
        }
    }
}