using System;
using System.Collections.Generic;

namespace SolutionGen.Generator.Model
{
    public class Template
    {
        public IReadOnlyDictionary<Configuration, TemplateConfiguration> Configurations { get; }
        
        public Template(IReadOnlyDictionary<Configuration, TemplateConfiguration> configurations)
        {
            Configurations = configurations;
        }
        
        public static string ExpandModuleName(string str, string moduleName)
        {
            return str.Replace("$(MODULE_NAME)", moduleName);
        }
    }
    
    public class TemplateConfiguration
    {
        public IReadOnlyDictionary<string, ProjectDelcaration> ProjectDeclarations { get; }
        public IReadOnlyDictionary<string, Settings> Settings { get; }

        public TemplateConfiguration(
            IReadOnlyDictionary<string, ProjectDelcaration> projectDelcarations,
            IReadOnlyDictionary<string, Settings> settings)
        {
            ProjectDeclarations = projectDelcarations;
            Settings = settings;
        }
    }
    
    public class ProjectDelcaration
    {
        public string ProjectName { get; }
        public string SettingsName { get; }
        
        public ProjectDelcaration(string projectName, string settingsName)
        {
            ProjectName = projectName;
            SettingsName = settingsName;
        }
    }

    public sealed class DuplicateProjectNameException : Exception
    {
        public DuplicateProjectNameException(string name)
            : base($"Project with name '{name}' has already been declared. Projects must have a unique name.")
        {
            
        }
    }
}