﻿using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Reader
{
    public class TemplateReader
    {
        public const string ROOT_SETTINGS_NAME = "_root";
        
        private readonly IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups;
        private readonly Template baseTemplate;

        public TemplateReader(IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups)
        {
            this.configurationGroups = configurationGroups;
        }

        public TemplateReader(IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups,
            Template baseTemplate)
            : this(configurationGroups)
        {
            this.baseTemplate = baseTemplate;
        }

        public Template Read(ObjectElement templateElement)
        {
            Dictionary<Configuration, TemplateConfiguration> templateConfigurations =
                configurationGroups.Values.SelectMany(g => g.Configurations.Values)
                    .ToDictionary(cfg => cfg, cfg => ReadForConfiguration(templateElement, cfg));

            return new Template(templateConfigurations);
        }
        
        private TemplateConfiguration ReadForConfiguration(ObjectElement templateElement, Configuration configuration)
        {
            // When reading a template element there will be no base settings. However, when reading a module element
            // which is treated like a template there will will be base settings. Those base settings are the root template
            // element where project declarations are normally made.
            Settings rootBaseSettings = null;
            baseTemplate?.Configurations[configuration].Settings
                .TryGetValue(ROOT_SETTINGS_NAME, out rootBaseSettings);

            var rootReader = new SettingsReader(configuration, rootBaseSettings);
            Settings rootSettings = rootReader.Read(templateElement);

            if (!rootSettings.TryGetProperty(Settings.PROP_PROJECT_DELCARATIONS, out HashSet<string> declarations))
            {
                throw new MissingProjectDeclarationsException(templateElement, configuration);
            }

            Dictionary<string, Settings> settingsLookup = baseTemplate != null
                ? new Dictionary<string, Settings>(baseTemplate.Configurations[configuration].Settings)
                : new Dictionary<string, Settings>();

            settingsLookup[ROOT_SETTINGS_NAME] = rootSettings;
            
            var projectDelcarations = new List<ProjectDelcaration>();
            foreach (string declaration in declarations)
            {
                string[] parts = declaration.Split(':');
                if (parts.Length != 2)
                {
                    throw new InvalidProjectDeclarationException(declaration);
                }
                projectDelcarations.Add(new ProjectDelcaration(parts[0].Trim(), parts[1].Trim()));
            }

            foreach (ConfigElement element in templateElement.Elements)
            {
                if (element is ObjectElement objElement && objElement.Heading.Type == SectionType.SETTINGS)
                {
                    string settingsName = objElement.Heading.Name;
                    string baseSettingsName = objElement.Heading.InheritedObjectName;

                    Settings baseSettings = null;
                    if (!string.IsNullOrEmpty(baseSettingsName) &&
                        !settingsLookup.TryGetValue(baseSettingsName, out baseSettings))
                    {
                        throw new UndefinedBaseSettingsException(settingsName, baseSettingsName);
                    }
                    
                    // Only check for duplicate settings when reading a template object. Module objects get a copy of
                    // their template's settings. The module can then overwrite the settings as needed.
                    if (baseTemplate == null && settingsLookup.ContainsKey(settingsName))
                    {
                        throw new DuplicateSettingsNameException(settingsName);
                    }

                    Settings settings;
                    if (baseSettings == null && baseTemplate != null &&
                        baseTemplate.Configurations[configuration].Settings
                            .TryGetValue(settingsName, out Settings baseTemplateBaseSettings))
                    {
                        settings = new SettingsReader(configuration, baseTemplateBaseSettings).Read(objElement);
                    }
                    else
                    {
                        settings = new SettingsReader(configuration, baseSettings).Read(objElement);
                    }
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