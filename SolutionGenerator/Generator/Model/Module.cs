using System.Collections.Generic;

namespace SolutionGen.Generator.Model
{
    public class Module
    {
        public string Name { get; }

        private readonly Dictionary<string, Project> projects;
        public IReadOnlyDictionary<string, Project> Projects => projects;

        public Module(string name, Dictionary<string, Project> projects)
        {
            Name = name;
            this.projects = projects;
        }
    }
}