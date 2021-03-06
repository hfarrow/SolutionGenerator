﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Reader
{
    public class ModuleReader
    {
        public struct Result
        {
            public readonly Module Module;
            public readonly IReadOnlyCollection<string> ExcludedProjects;

            public Result(Module module, IReadOnlyCollection<string> excludedProjects)
            {
                Module = module;
                ExcludedProjects = excludedProjects;
            }
        }
        
        private readonly Solution solution;
        private readonly IReadOnlyDictionary<string, Template> templates;
        private readonly TemplateReader templateReader;
        private Dictionary<string, Project.Identifier> idLookup;

        private HashSet<string> excludedProjects;

        public ModuleReader(Solution solution, IReadOnlyDictionary<string, Template> templates,
            TemplateReader templateReader)
        {
            this.solution = solution;
            this.templates = templates;
            this.templateReader = templateReader;
        }

        public Result Read(ObjectElement moduleElement)
        {
            Log.Heading("Reading module element: {0}", moduleElement);

            Result result;
            
            using (new CompositeDisposable(
                new Log.ScopedIndent(),
                new Log.ScopedTimer(Log.Level.Debug, "Read Module", moduleElement)))
            {
                string moduleName = moduleElement.Heading.Name;
                string templateName = moduleElement.Heading.InheritedObjectName;
                
                excludedProjects = new HashSet<string>();
                idLookup = new Dictionary<string, Project.Identifier>();

                if (!string.IsNullOrEmpty(templateName) && !templates.ContainsKey(templateName))
                {
                    throw new UndefinedTemplateException(templateName);
                }

                Template template = templateReader.Read(moduleElement);

                using (new ExpandableVars.ScopedVariable(ExpandableVars.Instance, ExpandableVars.VAR_MODULE_NAME,
                    moduleName))
                {
                    Dictionary<Configuration, ModuleConfiguration> moduleConfigs =
                        CreateModuleConfigs(template, moduleName);

                    result =  new Result(new Module(solution, moduleName, moduleConfigs, idLookup), excludedProjects);
                }
            }

            return result;
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
                string projectName = ExpandableVars.Instance.ExpandModuleNameInCopy(declaration.ProjectName, moduleName)
                    .ToString();

                using (new ExpandableVars.ScopedVariable(ExpandableVars.Instance, ExpandableVars.VAR_PROJECT_NAME,
                    projectName))
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
                            excludedProjects.Add(projectName);
                            continue;
                        }

                        Settings projectSettings = templateConfig.ProjectSettingsLookup[declaration.SettingsName];
                        if (projectSettings.GetProperty<string>(Settings.PROP_EXCLUDE) == "true")
                        {
                            Log.Info("Project '{0}' is excluded from configuration '{1} - {2}'",
                                projectName, config.GroupName, config.Name);
                            excludedProjects.Add(projectName);
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
                            string relativeSourcePath =
                                Path.GetRelativePath(solution.SolutionConfigDir, moduleSourcePath);

                            id = new Project.Identifier(projectName, guid, moduleSourcePath, relativeSourcePath);
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