using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Reader;
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
        public IReadOnlyDictionary<string, Settings> ProjectSettingsLookup { get; }
        public Settings TemplateSettings { get; }

        public TemplateConfiguration(
            IReadOnlyDictionary<string, Settings> projectSettingsLookup,
            Settings templateSettings)
        {
            ProjectSettingsLookup = projectSettingsLookup;
            TemplateSettings = templateSettings;

            if (templateSettings.TryGetProperty(Settings.PROP_PROJECT_DELCARATIONS, out HashSet<string> declarations))
            {
                var projectDelcarations = new List<ProjectDelcaration>();
                foreach (string declaration in declarations)
                {
                    string[] parts = declaration.Split(':');
                    if (parts.Length != 2)
                    {
                        throw new InvalidProjectDeclarationException(declaration);
                    }
                    
                    projectDelcarations.Add(new ProjectDelcaration(parts[0].Trim(), parts[1].Trim()));
                }

                ProjectDeclarations = projectDelcarations.ToDictionary(d => d.ProjectName, d => d);
            }
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
    
    public sealed class InvalidProjectDeclarationException : Exception
    {
        public InvalidProjectDeclarationException(string declaration)
            : base($"Project declaration '{declaration}' must contain a project name and settings name delimited by" +
                   " a colon. Example: MyProject : My Settings")
        {
            
        }
    }
}