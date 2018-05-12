using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Parsing.Model;

namespace SolutionGen.Compiling.Model
{
    public class Solution
    {
        public static readonly List<PropertyDefinition> PropertyDefinitions = new List<PropertyDefinition>
        {
            new PropertyDefinition<HashSet<object>, HashSetPropertyCompiler>("target platforms"),
        };
        
        private static readonly Dictionary<string, PropertyDefinition> propertyDefinitionMap =
            PropertyDefinitions.ToDictionary(x => x.Name, x => x);
        
        public ObjectElement SolutionObject { get; }
        public Dictionary<string, ConfigurationElement> Configurations { get; } =
            new Dictionary<string, ConfigurationElement>();
        public string[] TargetPlatforms { get; }
        public readonly Settings Settings;
        
        public Solution(ObjectElement solutionObject)
        {
            SolutionObject = solutionObject;
            ProcessElements();
            Settings = new Settings(null, this, solutionObject, null, null, null);
            Settings.Compile();
        }

        private void ProcessElements()
        {
            foreach (ConfigElement element in SolutionObject.Elements)
            {
                switch (element)
                {
                    case ConfigurationElement configurationElement
                        when Configurations.ContainsKey(configurationElement.ConfigurationName):
                        throw new DuplicateConfigurationNameException(configurationElement,
                            Configurations[configurationElement.ConfigurationName]);

                    case ConfigurationElement configurationElement:
                        Configurations.Add(configurationElement.ConfigurationName, configurationElement);
                        break;
                    
                    default:
                        throw new UnexpectedElementException(element, "configuration; inline-settings");
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
                newElement.ConfigurationName, existingElement, newElement))
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