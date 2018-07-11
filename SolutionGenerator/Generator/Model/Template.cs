using System;
using System.Collections.Generic;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Model
{
    public class Template
    {
        public readonly string Name;
        public ObjectElement SourceElement { get; }
        public IReadOnlyDictionary<string, ObjectElement> SettingsSourceElements { get; }
        public IReadOnlyDictionary<Configuration, TemplateConfiguration> Configurations { get; }
        
        public Template(string name, ObjectElement sourceElement,
            IReadOnlyDictionary<string, ObjectElement> settingsSourceElements,
            IReadOnlyDictionary<Configuration, TemplateConfiguration> configurations)
        {
            Name = name;
            SourceElement = sourceElement;
            SettingsSourceElements = settingsSourceElements;
            Configurations = configurations;
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