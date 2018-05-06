using System.Collections.Generic;
using SolutionGenerator.Parsing.Model;

namespace SolutionGenerator.Compiling.Model
{
    public class Module
    {
        public ObjectElement ModuleElement { get; }
        public void AddProject(Project project) => projects[project.Name] = project;
        public Project GetProject(string name) => projects[name];
        public IReadOnlyCollection<Project> Projects => projects.Values;
        
        private readonly Dictionary<string, Project> projects = new Dictionary<string, Project>();
        
        public Module(ObjectElement moduleElement)
        {
            ModuleElement = moduleElement;
        }

        public void Clear()
        {
            projects.Clear();
        }

    }
}