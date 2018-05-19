using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Parsing;
using SolutionGen.Parsing.Model;

namespace SolutionGen.Compiling.Model
{
    public class Settings
    {
        public const string PROP_INCLUDE_FILES = "include files";
        public const string PROP_EXCLUDE_FILES = "exclude files";
        public const string PROP_LIB_REFS = "lib refs";
        public const string PROP_PROJECT_REFS = "project refs";
        public const string PROP_DEFINE_CONSTANTS = "define constants";
        public const string PROP_TARGET_FRAMEWORK = "target framework";
        public const string PROP_LANGUAGE_VERSION = "language version";
        public const string PROP_DEBUG_SYMBOLS = "debug symbols";
        public const string PROP_DEBUG_TYPE = "debug type";
        public const string PROP_OPTIMIZE = "optimize";
        public const string PROP_ERROR_REPORT = "error report";
        public const string PROP_WARNING_LEVEL= "warning level";
        public const string PROP_CONFIGURATION_PLATFORM_TARGET = "platform target";
        public const string PROP_TARGET_PLATFORMS = "target platforms";
        public const string PROP_ROOT_NAMESPACE = "root namespace";
        public const string PROP_SKIP = "skip";
        public const string PROP_EXCLUDE = "exclude";
        
        public static readonly List<PropertyDefinition> PropertyDefinitions = new List<PropertyDefinition>
        {
            // Module/Project Settings
            new PropertyDefinition<HashSet<object>, HashSetPropertyCompiler>(PROP_INCLUDE_FILES,
                new HashSet<object> {"glob \".{cs,txt,json,xml,md}\""}),
            new PropertyDefinition<HashSet<object>, HashSetPropertyCompiler>(PROP_EXCLUDE_FILES),
            new PropertyDefinition<HashSet<object>, HashSetPropertyCompiler>(PROP_LIB_REFS),
            new PropertyDefinition<HashSet<object>, HashSetPropertyCompiler>(PROP_PROJECT_REFS),
            new PropertyDefinition<HashSet<object>, HashSetPropertyCompiler>(PROP_DEFINE_CONSTANTS),
            new PropertyDefinition<string, StringPropertyCompiler>(PROP_TARGET_FRAMEWORK, "v4.6"),
            new PropertyDefinition<string, StringPropertyCompiler>(PROP_LANGUAGE_VERSION, "6"),
            new PropertyDefinition<string, StringPropertyCompiler>(PROP_DEBUG_SYMBOLS, "true"),
            new PropertyDefinition<string, StringPropertyCompiler>(PROP_DEBUG_TYPE, "full"),
            new PropertyDefinition<string, StringPropertyCompiler>(PROP_OPTIMIZE, "false"),
            new PropertyDefinition<string, StringPropertyCompiler>(PROP_ERROR_REPORT, "prompt"),
            new PropertyDefinition<string, StringPropertyCompiler>(PROP_WARNING_LEVEL, "4"),
            new PropertyDefinition<string, StringPropertyCompiler>(PROP_CONFIGURATION_PLATFORM_TARGET, "AnyCPU"),
            
            // Solution Settings
            new PropertyDefinition<HashSet<object>, HashSetPropertyCompiler>(PROP_TARGET_PLATFORMS),
            new PropertyDefinition<string, StringPropertyCompiler>(PROP_ROOT_NAMESPACE, ""),
        };
        
        public static readonly List<CommandDefinition> CommandDefinitions = new List<CommandDefinition>
        {
            new CommandDefinition<SimpleCommandCompiler>(PROP_EXCLUDE, settings =>
            {
                settings.SetProperty(PROP_EXCLUDE, true);
                return ElementCompiler.Result.Terminate;
            }),
            new CommandDefinition<SimpleCommandCompiler>(PROP_SKIP, settings =>
            {
                settings.SetProperty(PROP_SKIP, true);
                return ElementCompiler.Result.Terminate;
            }),
        };
        
        private static readonly Dictionary<string, PropertyDefinition> propertyDefinitionMap =
            PropertyDefinitions.ToDictionary(x => x.Name, x => x);
        
        private static readonly Dictionary<string, CommandDefinition> commandDefinitionMap =
            CommandDefinitions.ToDictionary(x => x.Name, x => x);

        public bool IsCompiled { get; private set; }
        
        public Template Template { get; }
        public Solution Solution { get; }
        public ObjectElement SettingsObject { get; }
        public string ConfigurationGroup { get; }
        public string ConfigurationName { get; }
        public string[] ExternalDefineConstants { get; }
        public HashSet<string> AllDefineConstants { get; private set; }
        public HashSet<string> ConditionalConstants { get; }

        private readonly Dictionary<string, object> properties = new Dictionary<string, object>();
        public bool HasProperty(string name) => properties.ContainsKey(name);
        public T GetProperty<T>(string name) => (T) properties[name];
        public void SetProperty<T>(string name, T value) => properties[name] = value;

        public Settings(Template template, Solution solution, ObjectElement settingsObject,
            string configurationGroup, string configurationName, string[] externalDefineConstants)
        {
            Template = template;
            Solution = solution;
            SettingsObject = settingsObject;
            ConfigurationGroup = configurationGroup;
            ConfigurationName = configurationName;
            ExternalDefineConstants = externalDefineConstants;

            if (Template != null)
            {
                ConditionalConstants =
                    Solution.ConfigurationGroups[ConfigurationGroup].Configurations[ConfigurationName];
            }
            else
            {
                ConditionalConstants = new HashSet<string>();
            }
        }

        public void Compile()
        {
            BooleanExpressionParser.SetConditionalConstants(ConditionalConstants);
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
                    
                    case CommentElement commentElement when element is CommentElement:
                        // Do nothing
                        result = ElementCompiler.Result.Continue;
                        break;

                    default:
                        throw new UnrecognizedSettingsElementException(element);
                }

                if (result == ElementCompiler.Result.Terminate)
                {
                    break;
                }
            }

            if (Template != null)
            {
                AllDefineConstants =
                    Solution.ConfigurationGroups[ConfigurationGroup].Configurations[ConfigurationName]
                        .Select(s => s.ToUpper())
                        .Concat(ExternalDefineConstants)
                        .Concat(GetProperty<HashSet<object>>(PROP_DEFINE_CONSTANTS).Select(obj => obj.ToString()))
                        .ToHashSet();
            }
            else
            {
                AllDefineConstants = (ExternalDefineConstants ?? new string[0]).ToHashSet();
            }

            IsCompiled = true;
        }

        public void ApplyToProject(Project project)
        {
            var configuration = new Project.Configuration(ConfigurationName, AllDefineConstants);
            project.SetConfiguration(ConfigurationName, configuration);           
            foreach (KeyValuePair<string,object> pair in properties)
            {
                configuration.SetProperty(pair.Key, pair.Value);
            }

            configuration.InitFromProperties(project);
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
                // Return a copy of any collection so that different projects don't modify the settings of other projects
                // When refactoring, need to cleanly support any type of collection.
                if (pair.Value is HashSet<object> hashset)
                {
                    properties[pair.Key] = new HashSet<object>(hashset);
                }
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
