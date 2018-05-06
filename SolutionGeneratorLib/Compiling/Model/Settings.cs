using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGenerator.Parsing;
using SolutionGenerator.Parsing.Model;

namespace SolutionGenerator.Compiling.Model
{
    public class Settings
    {
        public static readonly List<PropertyDefinition> PropertyDefinitions = new List<PropertyDefinition>
        {
            new PropertyDefinition<HashSet<string>, HashSetPropertyCompiler>("include paths",
                new HashSet<string> {"./"}),
            new PropertyDefinition<HashSet<string>, HashSetPropertyCompiler>("exclude paths"),
            new PropertyDefinition<HashSet<string>, HashSetPropertyCompiler>("include files",
                new HashSet<string> {"glob \".{cs,txt,json,xml,md}\""}),
            new PropertyDefinition<HashSet<string>, HashSetPropertyCompiler>("lib refs"),
            new PropertyDefinition<HashSet<string>, HashSetPropertyCompiler>("define constants"),
            new PropertyDefinition<string, StringPropertyCompiler>("target framework", "net4.6"),
            new PropertyDefinition<string, StringPropertyCompiler>("language version", "6"),
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
        public string Configuration { get; }
        public IEnumerable<string> ExternalDefineConstants { get; }
        public HashSet<string> AllDefineConstants { get; }

        private readonly Dictionary<string, object> properties = new Dictionary<string, object>();
        public T GetProperty<T>(string name) => (T) properties[name];
        public void SetProperty<T>(string name, T value) => properties[name] = value;

        public Settings(Template template, ObjectElement settingsObject,
            string configurationGroup, string configuration, IEnumerable<string> externalDefineConstants)
        {
            Template = template;
            SettingsObject = settingsObject;
            ConfigurationGroup = configurationGroup;
            Configuration = configuration;
            ExternalDefineConstants = externalDefineConstants;

            AllDefineConstants =
                Template.Configurations[ConfigurationGroup].Configurations[Configuration]
                    .Concat(ExternalDefineConstants).ToHashSet();
        }

        public void Compile()
        {
            BooleanExpressionParser.SetConditionalConstants(AllDefineConstants);
            
            foreach (ConfigElement element in SettingsObject.Elements)
            {
                ElementCompiler.Result result;
                switch (element)
                {
                    case PropertyElement propertyElement when element is PropertyElement:
                        result = CompileProperty(propertyElement);
                        break;

                    case CommandElement simpleCommandElement when element is CommandElement:
                        result = CompileSimpleCommand(simpleCommandElement);
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

        public void Apply(Project project)
        {
            foreach (KeyValuePair<string,object> pair in properties)
            {
                // TODO: make sure add vs set is being respected...
                // settings can't be compiled individually. They must be stacked and then compiled.
                project.SetProperty(pair.Key, pair.Value);
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
}
