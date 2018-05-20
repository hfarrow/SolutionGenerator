using System.Collections.Generic;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.ModelOld
{
    public class Module
    {
        public Solution Solution { get; }
        public ObjectElement ModuleElement { get; }
        public string Name => ModuleElement.Heading.Name;
        public void AddProject(Project project) => projects[project.Name] = project;
        public Project GetProject(string name) => projects[name];
        public IReadOnlyCollection<Project> Projects => projects.Values;
        public string RootPath { get; }
        
        private readonly Dictionary<string, Project> projects = new Dictionary<string, Project>();
        
        public Module(Solution solution, ObjectElement moduleElement, string rootPath)
        {
            Solution = solution;
            ModuleElement = moduleElement;
            RootPath = rootPath;
        }

        public void Clear()
        {
            projects.Clear();
        }

    }
}