using System.Collections.Generic;
using SolutionGenerator.Parsing.Model;

namespace SolutionGenerator.Compiling.Model
{
    public class Module
    {
        public ObjectElement ModuleElement { get; }
        
        private readonly Dictionary<string, object> properties = new Dictionary<string, object>();
        public T GetProperty<T>(string name) => (T) properties[name];
        public void SetProperty<T>(string name, T value) => properties[name] = value;
        
        private Dictionary<string, Project> Projects = new Dictionary<string, Project>();
        
        public Module(ObjectElement moduleElement)
        {
            ModuleElement = moduleElement;
        }

        public void Clear()
        {
            properties.Clear();
            Projects.Clear();
        }

        public void AddProject(Project project)
        {
            Projects[project.Name] = project;
        }
    }
}