using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;
using Path = System.IO.Path;

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
            Log.WriteLine("Reading module element: {0}", moduleElement);

            using (var _ = new Log.ScopedIndent(true))
            {
                string moduleName = moduleElement.Heading.Name;
                string templateName = moduleElement.Heading.InheritedObjectName;
                
                ExpandableVar.SetExpandableVariable(ExpandableVar.VAR_MODULE_NAME, moduleName);
                
                string moduleSourcePath = Path.Combine(
                    solution.SolutionConfigDir,
                    solution.Settings.GetProperty<string>(Settings.PROP_MODULE_SOURCE_PATH),
                    moduleName);
                
                idLookup = new Dictionary<string, Project.Identifier>();

                if (string.IsNullOrEmpty(templateName))
                {
                    throw new NotImplementedException(
                        $"Module named '{moduleName} must inherit from a template" +
                        "but this could be supported in the future");
                }

                if (!templates.TryGetValue(templateName, out Template baseTemplate))
                {
                    throw new UndefinedTemplateException(templateName);
                }

                var templateReader = new TemplateReader(solution.Settings.ConfigurationGroups, baseTemplate);
                Template template = templateReader.Read(moduleElement);
                
                Dictionary<Configuration, ModuleConfiguration> moduleConfigs =
                    CreateModuleConfigs(template, moduleName, moduleSourcePath);
                
                ExpandableVar.ClearExpandableVariable(ExpandableVar.VAR_MODULE_NAME);

                return new Module(solution, moduleName, moduleConfigs, idLookup,
                    moduleSourcePath);
            }
        }

        private Dictionary<Configuration, ModuleConfiguration> CreateModuleConfigs(Template template, string moduleName,
            string moduleSourcePath)
        {
            Log.WriteLine("Creating module configs for module '{0}' from template '{1}",
                moduleName, template.Name);

            using (var _ = new Log.ScopedIndent())
            {
                var moduleConfigs = new Dictionary<Configuration, ModuleConfiguration>();
                foreach (KeyValuePair<Configuration, TemplateConfiguration> kvp in template.Configurations)
                {
                    List<Project> projects = CreateProjectsForConfig(moduleName, moduleSourcePath, kvp.Key, kvp.Value);
                    moduleConfigs[kvp.Key] = new ModuleConfiguration(projects.ToDictionary(p => p.Name, p => p));
                }

                return moduleConfigs;
            }
        }

        private List<Project> CreateProjectsForConfig(string moduleName, string moduleSourcePath, Configuration config,
            TemplateConfiguration templateConfig)
        {
            var projects = new List<Project>();
            foreach (ProjectDelcaration declaration in templateConfig.ProjectDeclarations.Values)
            {
                Log.WriteLine("Creating project config '{0} - {1}' for project '{2}' (module '{3}) with settings '{4}'",
                    config.GroupName, config.Name, declaration.ProjectName, moduleName, declaration.SettingsName);
                
                using (var _ = new Log.ScopedIndent(true))
                {                   
                    Settings projectSettings = templateConfig.Settings[declaration.SettingsName];
                    if (projectSettings.GetProperty<string>(Settings.PROP_EXCLUDE) == "true")
                    {
                        Log.WriteLine("Project '{0}' is excluded from configuration '{1} - {2}'",
                            declaration.ProjectName, config.GroupName, config.Name);
                        continue;
                    }
                    
                    string projectName = ExpandableVar.ExpandModuleNameInCopy(declaration.ProjectName, moduleName).ToString();
                    // All configurations of a project must have the same guid.
                    if (!idLookup.TryGetValue(projectName, out Project.Identifier id))
                    {
                        id = new Project.Identifier(projectName, Guid.NewGuid(), moduleSourcePath);
                        idLookup[projectName] = id;
                    }

                    // TODO: set guid from project settings object
                    var project = new Project(solution, moduleName, id, config,
                        projectSettings.ExpandVariablesInCopy());

                    projects.Add(project);
                }
            }
            
            return projects;
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