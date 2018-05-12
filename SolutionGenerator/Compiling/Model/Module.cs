using System.Collections.Generic;
using SolutionGen.Parsing.Model;

namespace SolutionGen.Compiling.Model
{
    public class Module
    {
        public ObjectElement ModuleElement { get; }
        public string Name => ModuleElement.Heading.Name;
        public void AddProject(Project project) => projects[project.Name] = project;
        public Project GetProject(string name) => projects[name];
        public IReadOnlyCollection<Project> Projects => projects.Values;
        public string RootPath { get; }
        
        private readonly Dictionary<string, Project> projects = new Dictionary<string, Project>();
        
        public Module(ObjectElement moduleElement, string rootPath)
        {
            ModuleElement = moduleElement;
            RootPath = rootPath;
        }

        public void Clear()
        {
            projects.Clear();
        }

    }
}