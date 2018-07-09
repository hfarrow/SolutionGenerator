using System.Collections.Generic;

namespace SolutionGen.Generator.Model
{
    public class Configuration
    {
        public string GroupName { get; }
        public string Name { get; }
        public IEnumerable<string> Conditionals => conditionals;

        private readonly HashSet<string> conditionals;
        
        public Configuration(string groupName, string name, HashSet<string> conditionals)
        {
            GroupName = groupName;
            Name = name;
            this.conditionals = conditionals;
        }
    }
}