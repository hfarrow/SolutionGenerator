using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Reader
{
    public class TemplateReader
    {
        private readonly IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups;

        public TemplateReader(IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups)
        {
            this.configurationGroups = configurationGroups;
        }
        
        public Template Read(ObjectElement templateElement)
        {
            var templateConfigurations = new Dictionary<Configuration, TemplateConfiguration>();
            foreach (ConfigurationGroup group in configurationGroups.Values)
            {
                foreach (Configuration configuration in group.Configurations.Values)
                {
                    templateConfigurations.Add(configuration,
                        ReadForConfiguration(templateElement, group, configuration));
                }
            }

            return new Template(templateConfigurations);
        }

        private TemplateConfiguration ReadForConfiguration(ObjectElement templateElement, ConfigurationGroup group, 
            Configuration configuration)
        {
            var rootReader = new SettingsReader(configuration, null);
            Settings rootSettings = rootReader.Read(templateElement);

            if (!rootSettings.TryGetProperty(Settings.PROP_PROJECT_DELCARATIONS, out HashSet<string> declarations))
            {
                throw new MissingProjectDeclarationsException(templateElement, configuration);
            }

            var projectDelcarations = new List<ProjectDelcaration>();
            foreach (string declaration in declarations)
            {
                string[] parts = declaration.Split(':');
                if (parts.Length != 2)
                {
                    throw new InvalidProjectDeclarationException(declaration);
                }
                projectDelcarations.Add(new ProjectDelcaration(parts[0], parts[1]));
            }

            var settingsLookup = new Dictionary<string, Settings>();
            foreach (ConfigElement element in templateElement.Elements)
            {
                if (element is ObjectElement objElement && objElement.Heading.Type == SectionType.SETTINGS)
                {
                    string settingsName = objElement.Heading.Name;
                    string baseSettingsName = objElement.Heading.InheritedObjectName;

                    Settings baseSettings = null;
                    if (!string.IsNullOrEmpty(baseSettingsName) && !settingsLookup.TryGetValue(baseSettingsName, out baseSettings))
                    {
                        throw new UndefinedBaseSettingsException(settingsName, baseSettingsName);
                    }

                    if (settingsLookup.ContainsKey(settingsName))
                    {
                        throw new DuplicateSettingsNameException(settingsName);
                    }

                    Settings settings = new SettingsReader(configuration, baseSettings).Read(objElement);
                    settingsLookup[settingsName] = settings;
                }
            }

            return new TemplateConfiguration(
                projectDelcarations.ToDictionary(d => d.ProjectName, d => d),
                settingsLookup);
        }
    }

    public sealed class MissingProjectDeclarationsException : Exception
    {
        public MissingProjectDeclarationsException(ObjectElement templateElement, Configuration configuration)
            : base(string.Format("Template '{0}' for configuration '{1}.{2}' must declare at least one project.",
                    templateElement.Heading.Name, configuration.GroupName, configuration.Name))
        {
            
        }
    }

    public sealed class InvalidProjectDeclarationException : Exception
    {
        public InvalidProjectDeclarationException(string declaration)
            : base($"Project declaration '{declaration}' must contain a project name and settings name delimited by" +
                   " a colon. Example: MyProject : My Settings")
        {
            
        }
    }

    public sealed class UndefinedBaseSettingsException : Exception
    {
        public UndefinedBaseSettingsException(string settingsName, string baseSettingsName)
            : base($"Base settings object '{baseSettingsName}' must be defined above settings object '{settingsName}")
        {
        }
    }

    public sealed class DuplicateSettingsNameException : Exception
    {
        public DuplicateSettingsNameException(string name)
            : base($"A settings object using name '{name}' has already been defined.")
        {
            
        }
    }
}