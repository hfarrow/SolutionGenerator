using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SolutionGenerator.Parsing.Model;

namespace SolutionGenerator.Compiling.Model
{
    public class Template
    {
        public ObjectElement TemplateObject { get; }
        public bool IsCompiled { get; private set; }

        public Dictionary<string, ConfigurationElement> Configurations { get; } =
            new Dictionary<string, ConfigurationElement>();

        public Dictionary<string, PropertyElement> ProjectDeclarations { get; } =
            new Dictionary<string, PropertyElement>();
        
        public Dictionary<string, ObjectElement> SettingsObjects { get; } = new Dictionary<string, ObjectElement>();
        
        public Dictionary<string, Settings> CompiledSettings { get; } =
            new Dictionary<string, Settings>();

        public Template(ObjectElement templateObject)
        {
            TemplateObject = templateObject;
            ProcessElements();
        }
        
        private void ProcessElements()
        {
            foreach (ConfigElement element in TemplateObject.Elements)
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

                    case PropertyElement propertyElement when propertyElement.NameParts.First() != "project" ||
                                                              propertyElement.Action != PropertyAction.Add:
                        throw new UnexpectedElementException(element, "add project");
                    
                    case PropertyElement propertyElement:
                        string name = propertyElement.NameParts.ElementAt(1);
                        if (ProjectDeclarations.ContainsKey(name))
                        {
                            throw new DuplicateProjectNameException(name);
                        }
                        ProjectDeclarations[name] = propertyElement;
                        break;

                    case ObjectElement obj when obj.Heading.Type != "settings":
                        throw new UnexpectedElementException(element, "settings");

                    case ObjectElement obj when SettingsObjects.ContainsKey(obj.Heading.Name):
                        throw new DuplicateSettingsNameException(obj, SettingsObjects[obj.Heading.Name]);

                    case ObjectElement obj:
                        SettingsObjects.Add(obj.Heading.Name, obj);
                        break;

                    default:
                        throw new UnexpectedElementException(element, "configuration; add project; settings");
                }
            }
        }
        
        public void Compile(string[] externalDefineConstants)
        {
            foreach (ConfigurationElement configurationElement in Configurations.Values)
            {
                string configurationGroup = configurationElement.ConfigurationName;
                foreach (KeyValuePair<string, HashSet<string>> pair in configurationElement.Configurations)
                {
                    string configuration = pair.Key;
                    CompileSettings(configurationGroup, configuration, externalDefineConstants);
                }
            }

            IsCompiled = true;
        }

        private void CompileSettings(string configurationGroup, string configuration,
            string[] externalDefineConstants)
        {
            foreach (ObjectElement settingsObject in SettingsObjects.Values)
            {
                string key = GetCompiledSettingsKey(configurationGroup, configuration, externalDefineConstants);
                if (CompiledSettings.ContainsKey(key))
                {
                    continue;
                }

                var compiledSettings = new Settings(this, settingsObject, configurationGroup, configuration,
                    externalDefineConstants);

                compiledSettings.Compile();
                CompiledSettings[key] = compiledSettings;
            }
        }

        public void Apply(Module module)
        {
            if (!IsCompiled)
            {
                throw new InvalidOperationException(
                    string.Format("Template '{0}' must be compiled before it can be applied to module '{1}'.",
                        TemplateObject.Heading.Name, module.ModuleElement.Heading.Name));
            }
            
            module.Clear();
            foreach (KeyValuePair<string, PropertyElement> pair in ProjectDeclarations)
            {
                var project = new Project(pair.Key);
                
                var settingsToApply = new Stack<Settings>();
                Settings current = null;
                do
                {
                    string settingsName = current == null
                        ? pair.Value.ValueElement.Value.ToString()
                        : current.SettingsObject.Heading.InheritedObjectName;

                    if (!CompiledSettings.TryGetValue(settingsName, out Settings settings))
                    {
                        throw new UndefinedSettingsObjectException(settingsName);
                    }

                    current = settings;
                    settingsToApply.Push(settings);

                } while (!string.IsNullOrEmpty(current.SettingsObject.Heading.InheritedObjectName));

                while (settingsToApply.Count > 0)
                {
                    Settings settings = settingsToApply.Pop();
                    settings.Apply(project);
                }

                module.AddProject(project);
            }
        }

        private string GetCompiledSettingsKey(string configurationGroup, string configuation,
            string[] externalDefineConstants)
        {
            return configurationGroup + configuation + string.Join(string.Empty, externalDefineConstants);
        }
    }
    
    public class DuplicateConfigurationNameException : Exception
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

    public class DuplicateSettingsNameException : DuplicateObjectNameException
    {
        public DuplicateSettingsNameException(ObjectElement newElement, ObjectElement existingElement)
            : base(newElement, existingElement)
        {

        }
    }
    
    public class DuplicateProjectNameException : Exception
    {
        public DuplicateProjectNameException(string name)
            : base($"A project named '{name}' was already defined.")
        {

        }
    }

    public class UnexpectedElementException : Exception
    {
        public UnexpectedElementException(ConfigElement element, string expected)
            : base($"Expected element type was {expected} but actual element was: {element}")
        {

        }
    }

    public class UndefinedSettingsObjectException : Exception
    {
        public UndefinedSettingsObjectException(string name)
            : base($"A setting object named '{name} was never defined.")
        {
            
        }
    }
}
