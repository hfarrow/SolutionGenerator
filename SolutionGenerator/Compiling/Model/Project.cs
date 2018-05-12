using System;
using System.Collections.Generic;

namespace SolutionGen.Compiling.Model
{
    public class Project
    {
        public class Configuration
        {
            public string Name { get; }

            private readonly HashSet<string> defineConstants;
            
            private readonly Dictionary<string, object> properties = new Dictionary<string, object>();
            public T GetProperty<T>(string name) => (T) properties[name];
            public void SetProperty<T>(string name, T value) => properties[name] = value;
            public IReadOnlyCollection<string> DefineConstants => defineConstants;

            public Configuration(string name, HashSet<string> defineConstants)
            {
                Name = name;
                this.defineConstants = defineConstants;
            }

            public override string ToString()
            {
                return $"Project.Configuration{{{Name}}}";
            }
        }
        
        public string Name { get; }
        public Guid guid { get; }
        public bool HasConfiguration(string name) => configurations.ContainsKey(name);
        public Configuration GetConfiguration(string name) => configurations[name];
        public void SetConfiguration(string name, Configuration configuration) => configurations[name] = configuration;
        public void ClearConfigurations() => configurations.Clear();
        
        private readonly Dictionary<string, Configuration> configurations = new Dictionary<string, Configuration>();

        public Project(string name)
        {
            Name = name;
            guid = Guid.NewGuid();
        }
    }
}