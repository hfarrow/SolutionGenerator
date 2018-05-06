using System.Collections.Generic;

namespace SolutionGenerator.Compiling.Model
{
    public class Project
    {
        public string Name { get; }
        
        private readonly Dictionary<string, object> properties = new Dictionary<string, object>();
        public T GetProperty<T>(string name) => (T) properties[name];
        public void SetProperty<T>(string name, T value) => properties[name] = value;
        
        public Project(string name)
        {
            Name = name;
        }
    }
}