using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Reader
{
    public class TemplateReader
    {
        public const string ROOT_SETTINGS_NAME = "_root";
        
        private readonly IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups;
        private readonly Template baseTemplate;
        private readonly Settings defaultSettings;

        public TemplateReader(IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups,
            Settings defaultSettings)
        {
            this.configurationGroups = configurationGroups;
            this.defaultSettings = defaultSettings;
        }

        public TemplateReader(IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups,
            Template baseTemplate, Settings defaultSettings)
            : this(configurationGroups, defaultSettings)
        {
            this.baseTemplate = baseTemplate;
        }

        public Template Read(ObjectElement templateElement)
        {
            Log.Info("Reading template element with base template '{0}': {1}",
                baseTemplate?.Name ?? "none", templateElement);
            
            using (new Disposable(
                new Log.ScopedIndent(),
                new Log.ScopedTimer(Log.Level.Debug, "Read Template")))
            {
                Dictionary<Configuration, TemplateConfiguration> templateConfigurations =
                    configurationGroups.Values.SelectMany(g => g.Configurations.Values)
                        .ToDictionary(cfg => cfg, cfg => CreateTemplateConfig(templateElement, cfg));

                Dictionary<string, ObjectElement> settingsSourceElements = templateElement.Elements
                    .Where(e => e is ObjectElement obj && obj.ElementHeading.Type == SectionType.SETTINGS)
                    .Cast<ObjectElement>().ToDictionary(obj => obj.ElementHeading.Name, obj => obj);
                
                return new Template(
                    templateElement.ElementHeading.Name,
                    templateElement,
                    settingsSourceElements,
                    templateConfigurations);
            }
        }
        
        private TemplateConfiguration CreateTemplateConfig(ObjectElement templateElement, Configuration configuration)
        {
            Log.Heading("Creating template config '{0} - {1}' for template '{2}'",
                configuration.GroupName, configuration.Name, templateElement.ElementHeading.Name);

            using (new Log.ScopedIndent())
            {
                // When reading a template element there will be no base settings. However, when reading a module element
                // which is treated like a template there will be base settings. Those base settings are the root template
                // element where project declarations are normally made.
                Settings rootBaseSettings = null;
                baseTemplate?.Configurations[configuration].Settings
                    .TryGetValue(ROOT_SETTINGS_NAME, out rootBaseSettings);

                var rootReader = new ProjectSettingsReader(configuration, rootBaseSettings, null);
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
                    if (element is ObjectElement objElement && objElement.ElementHeading.Type == SectionType.SETTINGS)
                    {
                        string settingsName = objElement.ElementHeading.Name;
                        string baseSettingsName = objElement.ElementHeading.InheritedObjectName;

                        Settings inheritedSettings = null;
                        if (!string.IsNullOrEmpty(baseSettingsName) &&
                            !settingsLookup.TryGetValue(baseSettingsName, out inheritedSettings))
                        {
                            throw new UndefinedBaseSettingsException(settingsName, baseSettingsName);
                        }

                        // Only check for duplicate settings when reading a template object. Module objects get a copy of
                        // their template's settings. The module can then overwrite the settings as needed.
                        if (baseTemplate == null && settingsLookup.ContainsKey(settingsName))
                        {
                            throw new DuplicateSettingsNameException(settingsName);
                        }

                        Settings baseSettings;
                        // 1. reading a template object (as opposed to a module object read as a template) so use the
                        //    inherited settings as the base.
                        if (baseTemplate == null)
                        {
                            Log.Info("Using inherited settings '{0}' for base settings (1)", baseSettingsName);
                            // Can be null if the settings element does not inherit anything
                            baseSettings = inheritedSettings;
                        }
                        // 2. reading a module object without inheritance so use the template settings of the same name
                        //    as the base settings if they exist
                        else if (inheritedSettings == null)
                        {
                            Log.Info("Using template settings of same name '{0}' for base settings (2)", settingsName);
                            settingsLookup.TryGetValue(settingsName, out baseSettings);
                        }
                        // 3. reading a module object with inheritance
                        else
                        {
                            // a. Re-read template settings of the same name using inherited settings as the base but
                            // only if the template contains settings of the same name.
                            if (baseTemplate.SettingsSourceElements.TryGetValue(settingsName, out ObjectElement sourceElement))
                            {
                                Log.Info("Re-reading template settings of same name '{0}' for base settings (3.a)",
                                    settingsName);
                                baseSettings =
                                    new ProjectSettingsReader(configuration, inheritedSettings, defaultSettings).Read(sourceElement);
                            }
                            // b. Template does not contain settings same name so use the inheritedSettings
                            else
                            {
                                Log.Info(
                                    "Template does not contain settings of same name '{0}' so using inherited settings " +
                                    "'{0}' for base settings (3.b)",
                                    settingsName, baseSettingsName);
                                baseSettings = inheritedSettings;
                            }
                        }

                        settingsLookup[settingsName] =
                            new ProjectSettingsReader(configuration, baseSettings, defaultSettings).Read(objElement);
                    }
                }

                return new TemplateConfiguration(
                    projectDelcarations.ToDictionary(d => d.ProjectName, d => d),
                    settingsLookup);
            }
        }
    }

    public sealed class MissingProjectDeclarationsException : Exception
    {
        public MissingProjectDeclarationsException(ObjectElement templateElement, Configuration configuration)
            : base(string.Format("Template '{0}' for configuration '{1}.{2}' must declare at least one project.",
                    templateElement.ElementHeading.Name, configuration.GroupName, configuration.Name))
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