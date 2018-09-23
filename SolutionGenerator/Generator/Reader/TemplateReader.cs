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
        private readonly IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups;
        private readonly Settings defaultSettings;
        private readonly Dictionary<string, Template> cachedTemplates = new Dictionary<string, Template>();
        
        public TemplateReader(IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups,
            Settings defaultSettings)
        {
            this.configurationGroups = configurationGroups;
            this.defaultSettings = defaultSettings;
        }

        public Template Read(ObjectElement templateElement)
        {
            Log.Info("Reading template element '{0} : {1}'",
                templateElement.ElementHeading.Name,
                templateElement.ElementHeading.InheritedObjectName);

            using (new CompositeDisposable(
                new Log.ScopedIndent(),
                new Log.ScopedTimer(Log.Level.Debug, "Read Template")))
            {

                Dictionary<Configuration, TemplateConfiguration> templateConfigurations =
                    configurationGroups.Values.SelectMany(g => g.Configurations.Values)
                        .ToDictionary(cfg => cfg, cfg => CreateTemplateConfig(templateElement, cfg));

                Dictionary<string, ObjectElement> settingsSourceElements = templateElement.Elements
                    .Where(e => e is ObjectElement obj && obj.ElementHeading.Type == SectionType.SETTINGS)
                    .Cast<ObjectElement>().ToDictionary(obj => obj.ElementHeading.Name, obj => obj);
                
                var template = new Template(
                    templateElement.ElementHeading.Name,
                    templateElement,
                    settingsSourceElements,
                    templateConfigurations);
                cachedTemplates[template.Name] = template;
                return template;
            }
        }

        private TemplateConfiguration CreateTemplateConfig(ObjectElement templateElement, Configuration configuration)
        {
            Log.Heading("Creating template config '{0} - {1}' for template '{2}'",
                configuration.GroupName, configuration.Name, templateElement.ElementHeading.Name);

            using (new Log.ScopedIndent())
            {
                string baseTemplateName = templateElement.ElementHeading.InheritedObjectName;

                Template baseTemplate = null;
                Settings baseTemplateSettings = null;
                if (!string.IsNullOrEmpty(baseTemplateName) &&
                    cachedTemplates.TryGetValue(baseTemplateName, out baseTemplate))
                {
                    baseTemplateSettings = baseTemplate.Configurations[configuration].TemplateSettings;
                }

                var rootReader = new ProjectSettingsReader(configuration, baseTemplateSettings, null);
                Settings templateSettings = rootReader.Read(templateElement);

                Dictionary<string, Settings> settingsLookup = baseTemplate != null
                    ? new Dictionary<string, Settings>(baseTemplate.Configurations[configuration].ProjectSettingsLookup)
                    : new Dictionary<string, Settings>();

                foreach (ConfigElement element in templateElement.Elements)
                {
                    if (element is ObjectElement objElement && objElement.ElementHeading.Type == SectionType.SETTINGS)
                    {
                        string settingsName = objElement.ElementHeading.Name;
                        string inheritedSettingsName = objElement.ElementHeading.InheritedObjectName;

                        Settings inheritedSettings = null;
                        if (!string.IsNullOrEmpty(inheritedSettingsName) &&
                            !settingsLookup.TryGetValue(inheritedSettingsName, out inheritedSettings))
                        {
                            throw new UndefinedInheritedSettingsException(settingsName, inheritedSettingsName);
                        }
                        
                        // Only check for duplicate settings when reading a template object. Module objects get a copy of
                        // their template's settings. The module can then overwrite the settings as needed.
                        if (baseTemplate == null && settingsLookup.ContainsKey(settingsName))
                        {
                            throw new DuplicateSettingsNameException(settingsName);
                        }

                        Settings baseSettings;
                        // 1. reading a root template settings object so use the inherited settings as the base
                        if (baseTemplate == null)
                        {
                            Log.Info("Using inherited settings '{0}' for base settings (1)", inheritedSettingsName);
                            // Can be null if the settings element does not inherit anything
                            baseSettings = inheritedSettings;
                        }
                        // 2. reading a non-root template settings object without inheritance so use the template
                        // settings of the same name as the base settings if they exist
                        else if (inheritedSettings == null)
                        {
                            Log.Info("Using template settings of same name '{0}' for base settings (2)", settingsName);
                            settingsLookup.TryGetValue(settingsName, out baseSettings);
                        }
                        // 3. reading a non-root template settings object with inheritance
                        else
                        {
                            // a. When a non root template settings object inherits other settings, the base template's
                            // settings must be re-read (reinterpreted) ontop of the inherited settings
                            if (baseTemplate.SettingsSourceElements.TryGetValue(settingsName,
                                out ObjectElement sourceElement))
                            {
                                Log.Info("Re-reading template settings of same name '{0}' for base settings (3.a)",
                                    settingsName);
                                baseSettings =
                                    new ProjectSettingsReader(configuration, inheritedSettings, defaultSettings).Read(
                                        sourceElement);
                            }
                            // b. Base template does not contain settings of same name so use the inheritedSettings
                            // without re-reading them.
                            else
                            {
                                Log.Info(
                                    "Template does not contain settings of same name '{0}' so using inherited settings " +
                                    "'{0}' for base settings (3.b)",
                                    settingsName, inheritedSettingsName);
                                baseSettings = inheritedSettings;
                            }
                        }
                        settingsLookup[settingsName] =
                            new ProjectSettingsReader(configuration, baseSettings, defaultSettings).Read(objElement);
                    }
                }

                return new TemplateConfiguration(settingsLookup, templateSettings);
            }
        }
    }
    
    public sealed class UndefinedInheritedSettingsException : Exception
    {
        public UndefinedInheritedSettingsException(string settingsName, string baseSettingsName)
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