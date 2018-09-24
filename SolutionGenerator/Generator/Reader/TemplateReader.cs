using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                templateElement.Heading.Name,
                templateElement.Heading.InheritedObjectName);

            using (new CompositeDisposable(
                new Log.ScopedIndent(),
                new Log.ScopedTimer(Log.Level.Debug, "Read Template", templateElement)))
            {
                // Not parallel... In testing parallel code was slower but keeping the code so that it can be checked
                // after other improvements and on larger solutions.
                Dictionary<Configuration, TemplateConfiguration> templateConfigurations =
                    configurationGroups.Values.SelectMany(g => g.Configurations.Values)
                        .ToDictionary(cfg => cfg, cfg => CreateTemplateConfig(templateElement, cfg));

                // Parallel
//                IEnumerable<Configuration> configs = configurationGroups.Values
//                    .SelectMany(g => g.Configurations.Values);
//                
//                Task<(Configuration cfg, TemplateConfiguration templateConfig)>[] tasks =
//                    configs.Select(cfg => CreateTemplateConfigAsync(templateElement, cfg)).ToArray();
//                
//                try
//                {
//                    Task.WaitAll(tasks.Cast<Task>().ToArray());
//                }
//                catch (AggregateException ae)
//                {
//                    Log.Error("One or more exceptions occured while creating template config asynchronously:");
//                    foreach (Exception ex in ae.Flatten().InnerExceptions)
//                    {
//                        Log.Error(ex.Message);
//                    }
//
//                    throw;
//                }
//                
//                Dictionary<Configuration, TemplateConfiguration> templateConfigurations = tasks.Select(task => task.Result)
//                    .ToDictionary(pair => pair.cfg, pair => pair.templateConfig);

                Dictionary<string, ObjectElement> settingsSourceElements = templateElement.Children
                    .Where(e => e is ObjectElement obj && obj.Heading.Type == SectionType.SETTINGS)
                    .Cast<ObjectElement>().ToDictionary(obj => obj.Heading.Name, obj => obj);
                
                var template = new Template(
                    templateElement.Heading.Name,
                    templateElement,
                    settingsSourceElements,
                    templateConfigurations);
                
                lock (cachedTemplates)
                {
                    cachedTemplates[template.Name] = template;
                }
                
                return template;
            }
        }

//        private Task<(Configuration cfg, TemplateConfiguration templateConfig)> CreateTemplateConfigAsync(
//            ObjectElement templateElement, Configuration configuration)
//        {
//            var baseVars = new Dictionary<string, string>(ExpandableVars.Instance.Variables);
//            return Task.Run(() =>
//            {
//                using (new Log.BufferedTaskOutput($"CTC-{templateElement.ElementHeading.Name}-{configuration.Name}"))
//                {
//                    ExpandableVars.Init(baseVars);
//                    return (configuration, CreateTemplateConfig(templateElement, configuration));
//                }
//            });
//        }

        private TemplateConfiguration CreateTemplateConfig(ObjectElement templateElement, Configuration configuration)
        {
            Log.Heading("Creating template config '{0} - {1}' for template '{2}'",
                configuration.GroupName, configuration.Name, templateElement.Heading.Name);

            using (new Log.ScopedIndent())
            {
                string baseTemplateName = templateElement.Heading.InheritedObjectName;

                Template baseTemplate = null;
                Settings baseTemplateSettings = null;
                
                lock (cachedTemplates)
                {
                    if (!string.IsNullOrEmpty(baseTemplateName) &&
                        cachedTemplates.TryGetValue(baseTemplateName, out baseTemplate))
                    {
                        baseTemplateSettings = baseTemplate.Configurations[configuration].TemplateSettings;
                    }
                }

                var rootReader = new ProjectSettingsReader(configuration, baseTemplateSettings, null);
                Settings templateSettings = rootReader.Read(templateElement);

                Dictionary<string, Settings> settingsLookup = baseTemplate != null
                    ? new Dictionary<string, Settings>(baseTemplate.Configurations[configuration].ProjectSettingsLookup)
                    : new Dictionary<string, Settings>();

                foreach (ConfigElement element in templateElement.Children)
                {
                    if (element is ObjectElement objElement && objElement.Heading.Type == SectionType.SETTINGS)
                    {
                        string settingsName = objElement.Heading.Name;
                        string inheritedSettingsName = objElement.Heading.InheritedObjectName;

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