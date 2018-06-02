using System;
using System.Collections.Generic;

namespace SolutionGen.Generator.Model
{
    public class Solution
    {
        public string Name { get; }
        public readonly Guid Guid;

        public readonly Settings Settings;
        public IReadOnlyDictionary<string, Module> Modules => modules;
        private readonly Dictionary<string, Module> modules = new Dictionary<string, Module>();
        public IReadOnlyDictionary<string, Project> Projects => projects;
        private readonly Dictionary<string, Project> projects = new Dictionary<string, Project>();

        public Project GetProject(string name) => projects[name];

        public IReadOnlyCollection<string> TargetPlatforms =>
            Settings.GetProperty<IReadOnlyCollection<string>>(Settings.PROP_TARGET_PLATFORMS);

        public string RootNamespace => Settings.GetProperty<string>(Settings.PROP_ROOT_NAMESPACE);

        public Solution(string name, Settings settings)
        {
            Name = name;
            Guid = Guid.NewGuid();
            Settings = settings;
        }
    }
}