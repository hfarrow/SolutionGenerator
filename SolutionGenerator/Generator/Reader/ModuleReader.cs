using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;

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
            Log.Heading("Reading module element: {0}", moduleElement);

            using (new Disposable(
                new Log.ScopedIndent(),
                new Log.ScopedTimer(Log.Level.Debug, "Read Module")))
            {
                string moduleName = moduleElement.ElementHeading.Name;
                string templateName = moduleElement.ElementHeading.InheritedObjectName;
                
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

                var templateReader = new TemplateReader(solution.ConfigurationGroups, baseTemplate, null);
                Template template = templateReader.Read(moduleElement);
                
                using (new ExpandableVar.ScopedVariable(ExpandableVar.VAR_MODULE_NAME, moduleName))
                {
                    Dictionary<Configuration, ModuleConfiguration> moduleConfigs =
                        CreateModuleConfigs(template, moduleName);
                    
                    return new Module(solution, moduleName, moduleConfigs, idLookup);
                }
            }
        }

        private Dictionary<Configuration, ModuleConfiguration> CreateModuleConfigs(Template template, string moduleName)
        {
            Log.Info("Creating module configs for module '{0}' from template '{1}",
                moduleName, template.Name);

            using (new Log.ScopedIndent())
            {
                var moduleConfigs = new Dictionary<Configuration, ModuleConfiguration>();
                foreach (KeyValuePair<Configuration, TemplateConfiguration> kvp in template.Configurations)
                {
                    List<Project> projects = CreateProjectsForConfig(moduleName, kvp.Key, kvp.Value);
                    moduleConfigs[kvp.Key] = new ModuleConfiguration(projects.ToDictionary(p => p.Name, p => p));
                }

                return moduleConfigs;
            }
        }

        private List<Project> CreateProjectsForConfig(string moduleName, Configuration config,
            TemplateConfiguration templateConfig)
        {
            var projects = new List<Project>();
            foreach (ProjectDelcaration declaration in templateConfig.ProjectDeclarations.Values)
            {
                string projectName = ExpandableVar.ExpandModuleNameInCopy(declaration.ProjectName, moduleName)
                    .ToString();
                
                using (new ExpandableVar.ScopedVariable(ExpandableVar.VAR_PROJECT_NAME, projectName))
                {
                    
                    Log.Heading(
                        "Creating project config '{0} - {1}' for project '{2}' (module '{3}') with settings '{4}'",
                        config.GroupName, config.Name, projectName, moduleName, declaration.SettingsName);
                    

                    using (new Log.ScopedIndent())
                    {
                        if (solution.IncludedProjectsPatterns.Count > 0 && !solution.CanIncludeProject(projectName))
                        {
                            Log.Info("Project '{0}' is excluded by solution '{1}' property white list",
                                projectName, Settings.PROP_INCLUDE_PROJECTS);
                            continue;
                        }
                        
                        Settings projectSettings = templateConfig.Settings[declaration.SettingsName];
                        if (projectSettings.GetProperty<string>(Settings.PROP_EXCLUDE) == "true")
                        {
                            Log.Info("Project '{0}' is excluded from configuration '{1} - {2}'",
                                projectName, config.GroupName, config.Name);
                            continue;
                        }

                        projectSettings = projectSettings.ExpandVariablesInCopy();
                        string moduleSourcePath =
                            projectSettings.GetProperty<string>(Settings.PROP_PROJECT_SOURCE_PATH);

                        string guidStr = projectSettings.GetProperty<string>(Settings.PROP_GUID);
                        Guid guid = string.IsNullOrEmpty(guidStr) ? Guid.NewGuid() : Guid.Parse(guidStr);

                        // All configurations of a project must have the same guid.
                        if (!idLookup.TryGetValue(projectName, out Project.Identifier id))
                        {
                            id = new Project.Identifier(projectName, guid, moduleSourcePath);
                            idLookup[projectName] = id;
                        }

                        var project = new Project(solution, moduleName, id, config, projectSettings);

                        if (solution.IncludedProjectsPatterns.Count > 0)
                        {
                            string[] invalidProjectRefs = project.ProjectRefs
                                .Where(r => !solution.CanIncludeProject(r))
                                .ToArray();
                            
                            if (invalidProjectRefs.Length > 0)
                            {
                                throw new InvalidProjectReferenceException(project,
                                    $"Referenced project is not in the '{Settings.PROP_INCLUDE_PROJECTS}' whitelist property." +
                                    $" Invalid references are [{string.Join(", ", invalidProjectRefs)}]");
                            }
                        }

                        projects.Add(project);
                    }
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

    public sealed class InvalidProjectReferenceException : Exception
    {
        public InvalidProjectReferenceException(Project project, string reason)
            : base(string.Format(
                "Project '{0}' in module {1}' for configuration '{2} - {3}' contains an invalid project reference. Reason: {4}",
                project.Name,
                project.ModuleName,
                project.Configuration.GroupName,
                project.Configuration.Name,
                reason))
        {

        }
    }
}