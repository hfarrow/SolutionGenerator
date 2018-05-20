using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.ModelOld
{
    public class Solution
    {
        public ObjectElement SolutionObject { get; }
        public string Name => SolutionObject.Heading.Name;
        public Guid Guid { get; }
        
        public Dictionary<string, ConfigurationElement> ConfigurationGroups { get; } =
            new Dictionary<string, ConfigurationElement>();
        
        public string ActiveConfigurationGroup { get; set; }

        public IReadOnlyDictionary<string, HashSet<string>> ActiveConfigurations =>
            ConfigurationGroups[ActiveConfigurationGroup].Configurations;
        
        public readonly Settings Settings;
        public readonly HashSet<string> TargetPlatforms;
        
        public IReadOnlyCollection<Module> Modules => modules.Values;
        public void AddModule(Module module) => modules[module.Name] = module;
        public Module GetModule(string name) => modules[name];
        private readonly Dictionary<string, Module> modules = new Dictionary<string, Module>();
        private readonly Dictionary<string, Project> projects = new Dictionary<string, Project>();
        public void RegisterProject(Project project) => projects[project.Name] = project;
        public Project GetProject(string name) => projects[name];
        
        public Solution(ObjectElement solutionObject)
        {
            SolutionObject = solutionObject;
            Guid = Guid.NewGuid();
            
            ProcessElements();
            Settings = new Settings(null, this, solutionObject, null, null, null);
            Settings.Compile();

            if (Settings.HasProperty(Settings.PROP_TARGET_PLATFORMS))
            {
                TargetPlatforms = Settings.GetProperty<HashSet<object>>(Settings.PROP_TARGET_PLATFORMS)
                    .Select(o => o.ToString()).ToHashSet();
            }
        }

        private void ProcessElements()
        {
            foreach (ConfigElement element in SolutionObject.Elements)
            {
                switch (element)
                {
                    case ConfigurationElement configurationElement
                        when ConfigurationGroups.ContainsKey(configurationElement.ConfigurationGroupName):
                        throw new DuplicateConfigurationNameException(configurationElement,
                            ConfigurationGroups[configurationElement.ConfigurationGroupName]);

                    case ConfigurationElement configurationElement:
                        ConfigurationGroups.Add(configurationElement.ConfigurationGroupName, configurationElement);
                        break;
                }
            }
        }
    }
    
    public sealed class DuplicateConfigurationNameException : Exception
    {
        public DuplicateConfigurationNameException(ConfigurationElement newElement,
            ConfigurationElement existingElement)
            : base(string.Format("A configuration with name '{0}' has already been defined:\n" +
                                 "Existing Configuration:\n{1}\n" +
                                 "Invalid Configuration:\n{2}",
                newElement.ConfigurationGroupName, existingElement, newElement))
        {

        }
    }
    
    public sealed class UnexpectedElementException : Exception
    {
        public UnexpectedElementException(ConfigElement element, string expected)
            : base($"Expected element type was {expected} but actual element was: {element}")
        {

        }
    }
}