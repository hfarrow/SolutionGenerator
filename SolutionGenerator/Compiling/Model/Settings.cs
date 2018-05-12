using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Parsing;
using SolutionGen.Parsing.Model;

namespace SolutionGen.Compiling.Model
{
    public class Settings
    {
        public static readonly List<PropertyDefinition> PropertyDefinitions = new List<PropertyDefinition>
        {
            // Module/Project Settings
            new PropertyDefinition<HashSet<object>, HashSetPropertyCompiler>("include files",
                new HashSet<object> {"glob \".{cs,txt,json,xml,md}\""}),
            new PropertyDefinition<HashSet<object>, HashSetPropertyCompiler>("exclude files"),
            new PropertyDefinition<HashSet<object>, HashSetPropertyCompiler>("lib refs"),
            new PropertyDefinition<HashSet<object>, HashSetPropertyCompiler>("define constants"),
            new PropertyDefinition<string, StringPropertyCompiler>("target framework", "v4.6"),
            new PropertyDefinition<string, StringPropertyCompiler>("language version", "6"),
            
            // Solution Settings
            new PropertyDefinition<HashSet<object>, HashSetPropertyCompiler>("target platforms"),
            new PropertyDefinition<string, StringPropertyCompiler>("root namespace", ""),
        };
        
        public static readonly List<CommandDefinition> CommandDefinitions = new List<CommandDefinition>
        {
            new CommandDefinition<SimpleCommandCompiler>("exclude", settings =>
            {
                settings.SetProperty("exclude", true);
                return ElementCompiler.Result.Terminate;
            }),
            new CommandDefinition<SimpleCommandCompiler>("skip", settings =>
            {
                settings.SetProperty("skip", true);
                return ElementCompiler.Result.Terminate;
            }),
        };
        
        private static readonly Dictionary<string, PropertyDefinition> propertyDefinitionMap =
            PropertyDefinitions.ToDictionary(x => x.Name, x => x);
        
        private static readonly Dictionary<string, CommandDefinition> commandDefinitionMap =
            CommandDefinitions.ToDictionary(x => x.Name, x => x);

        public bool IsCompiled { get; private set; }
        
        public Template Template { get; }
        public ObjectElement SettingsObject { get; }
        public string ConfigurationGroup { get; }
        public string ConfigurationName { get; }
        public string[] ExternalDefineConstants { get; }
        public HashSet<string> AllDefineConstants { get; }

        private readonly Dictionary<string, object> properties = new Dictionary<string, object>();
        public bool HasProperty(string name) => properties.ContainsKey(name);
        public T GetProperty<T>(string name) => (T) properties[name];
        public void SetProperty<T>(string name, T value) => properties[name] = value;

        // TODO: Just pass in AllDefines now that configurations declaration was moved to Solution object
        public Settings(Template template, Solution solution, ObjectElement settingsObject,
            string configurationGroup, string configurationName, string[] externalDefineConstants)
        {
            Template = template;
            SettingsObject = settingsObject;
            ConfigurationGroup = configurationGroup;
            ConfigurationName = configurationName;
            ExternalDefineConstants = externalDefineConstants;

            if (Template != null)
            {
                AllDefineConstants =
                    solution.ConfigurationGroups[ConfigurationGroup].Configurations[ConfigurationName]
                        .Concat(ExternalDefineConstants).ToHashSet();
            }
            else
            {
                AllDefineConstants = ExternalDefineConstants != null 
                    ? ExternalDefineConstants.ToHashSet()
                    : new HashSet<string>();
            }
        }

        public void Compile()
        {
            BooleanExpressionParser.SetConditionalConstants(AllDefineConstants);
            ApplyBaseSettings();
            
            foreach (ConfigElement element in SettingsObject.Elements)
            {
                ElementCompiler.Result result;
                switch (element)
                {
                    case PropertyElement propertyElement when element is PropertyElement:
                        result = CompileProperty(propertyElement);
                        break;
                    
                    case CommandElement cmdElement when cmdElement.CommandName == "configuration":
                        // Ignore configuration elements in settings. They are processed only by the Solution object
                        continue;

                    case CommandElement cmdElement when element is CommandElement:
                        result = CompileSimpleCommand(cmdElement);
                        break;

                    default:
                        throw new UnrecognizedSettingsElementException(element);
                }

                if (result == ElementCompiler.Result.Terminate)
                {
                    break;
                }
            }

            IsCompiled = true;
        }

        public void ApplyTo(Project project)
        {
            var configuration = new Project.Configuration(ConfigurationName, AllDefineConstants);
            project.SetConfiguration(ConfigurationName, configuration);           
            foreach (KeyValuePair<string,object> pair in properties)
            {
                configuration.SetProperty(pair.Key, pair.Value);
            }
        }

        private void ApplyBaseSettings()
        {
            string baseSettingsName = SettingsObject.Heading.InheritedObjectName;
            if (string.IsNullOrEmpty(baseSettingsName) || Template == null)
            {
                return;
            }

            string baseSettingsKey =
                GetCompiledSettingsKey(
                    SettingsObject.Heading.InheritedObjectName,
                    ConfigurationGroup,
                    ConfigurationName,
                    ExternalDefineConstants);
            
            if(!Template.CompiledSettings.TryGetValue(baseSettingsKey, out Settings baseSettings))
            {
                throw new UndefinedSettingsObjectException(SettingsObject.Heading.InheritedObjectName,
                    ConfigurationGroup, ConfigurationName, ExternalDefineConstants);
            }

            if (!baseSettings.IsCompiled)
            {
                throw new InvalidOperationException(string.Format("Base settings '{0}' must be compiled before it's inheritors. " +
                                                                  "Was it defined before '{1}' in the template '{2}'?",
                    baseSettings.SettingsObject.Heading.Name,
                    SettingsObject.Heading.Name,
                    Template.TemplateObject.Heading.Name));
            }

            foreach (KeyValuePair<string,object> pair in baseSettings.properties)
            {
                properties[pair.Key] = pair.Value;
            }
        }

        private ElementCompiler.Result CompileProperty(PropertyElement propertyElement)
        {
            if (!propertyDefinitionMap.TryGetValue(propertyElement.FullName,
                out PropertyDefinition propertyDef))
            {
                throw new UnrecognizedPropertyException(propertyElement);
            }

            return propertyDef.Compiler.Compile(this, propertyElement, propertyDef);
        }

        private ElementCompiler.Result CompileSimpleCommand(CommandElement commandElement)
        {
            if (!commandDefinitionMap.TryGetValue(commandElement.CommandName,
                out CommandDefinition commandDef))
            {
                throw new UnrecognizedCommandException(commandElement);
            }

            return commandDef.Compiler.Compile(this, commandElement, commandDef);
        }
        
        public static string GetCompiledSettingsKey(string settingsName, 
            string configurationGroup = "", string configuation = "",
            string[] externalDefineConstants = null)
        {
            string defines = "";
            if (externalDefineConstants != null)
            {
                defines = string.Join(string.Empty, externalDefineConstants);
            }
            return settingsName + configurationGroup + configuation + defines;
        }
    }
    
    public sealed class UnrecognizedPropertyException : Exception
    {
        public UnrecognizedPropertyException(PropertyElement element)
            : base(string.Format(
                "The property '{0}' is not recongized and cannot be compiled. The full property was: {1}",
                element.FullName, element))
        {
        }
    }
    
    public sealed class UnrecognizedCommandException : Exception
    {
        public UnrecognizedCommandException(CommandElement element)
            : base(string.Format(
                "The command '{0}' is not recongized and cannot be compiled. The full property was: {1}",
                element.CommandName, element))
        {
        }
    }
    
    public sealed class UnrecognizedSettingsElementException : Exception
    {
        public UnrecognizedSettingsElementException(ConfigElement element)
            : base($"The element '{element}' is not recongized and cannot be compiled.")
        {
        }
    }

    public sealed class UndefinedSettingsObjectException : Exception
    {
        public UndefinedSettingsObjectException(string name, string configurationGroup, string configurationName,
            string[] externalDefineConstants)
            : base(string.Format(
                "A setting object named '{0}' was not defined for the following confuration: {1}.{2} ({3})",
                name, configurationGroup, configurationName, string.Join(",", externalDefineConstants)))
        {

        }
    }
}
