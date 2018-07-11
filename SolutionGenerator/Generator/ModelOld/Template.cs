using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.ModelOld
{
    public class Template
    {
        public ObjectElement TemplateObject { get; }
        public bool IsCompiled { get; private set; }

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
        
        public void Compile(Solution solution, string[] externalDefineConstants)
        {
            foreach (ConfigurationElement configurationElement in solution.ConfigurationGroups.Values)
            {
                string configurationGroup = configurationElement.ConfigurationGroupName;
                foreach (KeyValuePair<string, HashSet<string>> pair in configurationElement.Configurations)
                {
                    string configuration = pair.Key;
                    CompileSettings(solution, configurationGroup, configuration, externalDefineConstants);
                }
            }

            IsCompiled = true;
        }

        private void CompileSettings(Solution solution, string configurationGroup, string configuration,
            string[] externalDefineConstants)
        {
            foreach (ObjectElement settingsObject in SettingsObjects.Values)
            {
                string key = Settings.GetCompiledSettingsKey(
                    settingsObject.Heading.Name,
                    configurationGroup,
                    configuration,
                    externalDefineConstants);
                
                if (CompiledSettings.ContainsKey(key))
                {
                    continue;
                }

                var compiledSettings = new Settings(this, solution, settingsObject, configurationGroup, configuration,
                    externalDefineConstants);

                compiledSettings.Compile();
                CompiledSettings[key] = compiledSettings;
            }
        }

        public void ApplyToModule(string configurationGroup, Solution solution, Module module,
            string[] externalDefineConstants)
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
                string projectName = ExpandModuleName(pair.Key, module.Name);
                var project = new Project(projectName, module);
                project.ClearConfigurations();
                string settingsName = pair.Value.ValueElement.Value.ToString();

                foreach (string configurationName in solution.ConfigurationGroups[configurationGroup].Configurations.Keys)
                {
                    string settingsKey = Settings.GetCompiledSettingsKey(settingsName, configurationGroup,
                        configurationName, externalDefineConstants);
                    
                    if (!CompiledSettings.TryGetValue(settingsKey, out Settings settings))
                    {
                        throw new UndefinedSettingsObjectException(settingsName, configurationGroup, configurationName,
                            externalDefineConstants);
                    }
                    
                    settings.ApplyToProject(project);
                }

                if (!project.ExcludedFromGeneration)
                {
                    module.AddProject(project);
                }
            }
        }
    }

    public sealed class DuplicateSettingsNameException : DuplicateObjectNameException
    {
        public DuplicateSettingsNameException(ObjectElement newElement, ObjectElement existingElement)
            : base("settings", newElement, existingElement)
        {

        }
    }
    
    public sealed class DuplicateProjectNameException : Exception
    {
        public DuplicateProjectNameException(string name)
            : base($"A project named '{name}' was already defined.")
        {

        }
    }
}
