using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Reader
{
    public class ModuleReader
    {
        private readonly Solution solution;
        private readonly IReadOnlyDictionary<string, Template> templates;
        private Dictionary<string, Project.Identifier> idLookup;
            
        public ModuleReader(Solution solution, IReadOnlyDictionary<string, Template> templates)
        {
            this.solution = solution;
            this.templates = templates;
        }
        
        public Module Read(ObjectElement moduleElement)
        {
            string moduleName = moduleElement.Heading.Name;
            string templateName = moduleElement.Heading.InheritedObjectName;
            var moduleConfigs = new Dictionary<Configuration, ModuleConfiguration>();
            string moduleSourcePath = Path.Combine(solution.SolutionConfigPath, moduleName);
            idLookup = new Dictionary<string, Project.Identifier>();
            
            if (!string.IsNullOrEmpty(templateName))
            {
                if (!templates.TryGetValue(templateName, out Template baseTemplate))
                {
                    throw new UndefinedTemplateException(templateName);
                }

                var templateReader = new TemplateReader(solution.Settings.ConfigurationGroups, baseTemplate);
                Template template = templateReader.Read(moduleElement);
                moduleConfigs = CreateModuleConfigs(template, moduleName, moduleSourcePath);
            }
            else
            {
                throw new NotImplementedException(
                    $"Module named '{moduleName} must inherit from a template" +
                    "but this could be supported in the future");
            }

            return new Module(solution, moduleName, moduleConfigs, idLookup,
                // TODO: default to the module path below but allow override in settings
                moduleSourcePath);
        }

        private Dictionary<Configuration, ModuleConfiguration> CreateModuleConfigs(Template template, string moduleName,
            string moduleSourcePath)
        {
            var moduleConfigs = new Dictionary<Configuration, ModuleConfiguration>();
            foreach (KeyValuePair<Configuration,TemplateConfiguration> kvp in template.Configurations)
            {
                List<Project> projects = CreateProjectsForConfig(moduleName, moduleSourcePath, kvp.Key, kvp.Value);
                moduleConfigs[kvp.Key] = new ModuleConfiguration(projects.ToDictionary(p => p.Name, p => p));
            }

            return moduleConfigs;
        }

        private List<Project> CreateProjectsForConfig(string moduleName, string moduleSourcePath, Configuration config,
            TemplateConfiguration templateConfig)
        {
            var projects = new List<Project>();
            foreach (ProjectDelcaration declaration in templateConfig.ProjectDeclarations.Values)
            {
                Settings projectSettings = templateConfig.Settings[declaration.SettingsName];
                string projectName = ExpandModuleName(declaration.ProjectName, moduleName);
                // All configurations of a project must have the same guid.
                if (!idLookup.TryGetValue(projectName, out Project.Identifier id))
                {
                    id = new Project.Identifier(projectName, Guid.NewGuid(), moduleSourcePath);
                    idLookup[projectName] = id;
                }

                // TODO: set guid from project settings object
                var project = new Project(solution, moduleName, id, config,
                    projectSettings);
                
                projects.Add(project);
            }
            
            return projects;
        }

        private string ExpandModuleName(string input, string moduleName)
        {
            return input.Replace("$(MODULE_NAME)", moduleName);
        }
    }

    public sealed class UndefinedTemplateException : Exception
    {
        public UndefinedTemplateException(string templateName)
            : base($"A template with name '{templateName}' was not defined by the solution.")
        {
            
        }
    }
}