using System.Collections.Generic;

namespace SolutionGen.Generator.Model
{
    public class Module
    {
        private Solution Solution { get; }
        public string Name { get; }
        public IReadOnlyDictionary<Configuration, ModuleConfiguration> Configurations { get; }
        public IReadOnlyDictionary<string, Project.Identifier> ProjectIdLookup { get; }
        public string SourcePath { get; }

        public Module(Solution solution, string name,
            IReadOnlyDictionary<Configuration, ModuleConfiguration> configurations,
            IReadOnlyDictionary<string, Project.Identifier> projectIdLookup,
                string sourcePath)
        {
            Solution = solution;
            Name = name;
            Configurations = configurations;
            ProjectIdLookup = projectIdLookup;
            SourcePath = sourcePath;
        }
    }

    public class ModuleConfiguration
    {
        private readonly Dictionary<string, Project> projects;
        public IReadOnlyDictionary<string, Project> Projects => projects;
        
        public ModuleConfiguration(Dictionary<string, Project> projects)
        {
            this.projects = projects;
        }
    }
}