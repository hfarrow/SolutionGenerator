using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Reader
{
    public class SettingsReader
    {
        private static readonly List<PropertyDefinition> propertyDefinitions = new List<PropertyDefinition>
        {
            // Module / Project Settings
            new PropertyCollectionDefinition<HashSet<IPath>, IPath, PathPropertyReader>(Settings.PROP_INCLUDE_FILES,
                new HashSet<IPath>(){new GlobPath(".{cs,txt,json,xml,md}")}),
            new PropertyCollectionDefinition<HashSet<IPath>, IPath, PathPropertyReader>(Settings.PROP_EXCLUDE_FILES),
            new PropertyCollectionDefinition<HashSet<string>, string, StringPropertyReader>(Settings.PROP_LIB_REFS),
            new PropertyCollectionDefinition<HashSet<string>, string, StringPropertyReader>(Settings.PROP_PROJECT_REFS),
            new PropertyCollectionDefinition<HashSet<string>, string, StringPropertyReader>(Settings.PROP_DEFINE_CONSTANTS),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_TARGET_FRAMEWORK, "v4.6"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_LANGUAGE_VERSION, "6"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_DEBUG_SYMBOLS, "true"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_DEBUG_TYPE, "full"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_OPTIMIZE, "false"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_ERROR_REPORT, "prompt"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_WARNING_LEVEL, "4"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_CONFIGURATION_PLATFORM_TARGET, "AnyCPU"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_EXCLUDE, "false"),
            
            // Solution Settings
            new PropertyCollectionDefinition<HashSet<string>, string, StringPropertyReader>(Settings.PROP_TARGET_PLATFORMS,
                new HashSet<string>(){"Any CPU"}),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_ROOT_NAMESPACE, string.Empty)
        };

        private static readonly List<CommandDefinition> commandDefinitions =
            new List<CommandDefinition>()
            {
                new CommandDefinition<CommandReader>(Settings.CMD_SKIP, () => true),
            };

        private static readonly Dictionary<string, PropertyDefinition> propertyDefinitionLookup =
            propertyDefinitions.ToDictionary(d => d.Name, d => d);

        private static readonly Dictionary<string, CommandDefinition> commandDefinitionLookup =
            commandDefinitions.ToDictionary(c => c.Name, c => c);

        private readonly Dictionary<string, object> properties =
            propertyDefinitionLookup.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetOrCloneDefaultValue());

        private readonly Dictionary<string, ConfigurationGroup> configurationGroups =
            new Dictionary<string, ConfigurationGroup>();
        
        public Settings Read(ObjectElement settingsObject)
        {
            foreach (ConfigElement element in settingsObject.Elements)
            {
                bool terminate = false;
                switch (element)
                {
                    case ConfigurationGroupElement configurationElement:
                        terminate = ReadConfiguration(configurationElement);
                        break;
                    
                    case PropertyElement propertyElement when element is PropertyElement:
                        terminate = ReadProperty(propertyElement);
                        break;

                    case CommandElement cmdElement when element is CommandElement:
                        terminate = ReadCommand(cmdElement);
                        break;

                    default:
                        throw new UnrecognizedSettingsElementException(element);
                }

                if (terminate)
                {
                    break;
                }
            }
            
            return new Settings(properties, configurationGroups);
        }

        private bool ReadConfiguration(ConfigurationGroupElement element)
        {
            if (configurationGroups.TryGetValue(element.ConfigurationGroupName, out ConfigurationGroup existingGroup))
            {
                throw new DuplicateConfigurationGroupNameException(element, existingGroup);
            }

            var group = new ConfigurationGroup(element.ConfigurationGroupName,
                element.Configurations.ToDictionary(kvp => kvp.Key, kvp => new Configuration(kvp.Key, kvp.Value)));
            configurationGroups[group.Name] = group;

            return false;
        }

        private bool ReadProperty(PropertyElement element)
        {
            if (!propertyDefinitionLookup.TryGetValue(element.FullName, out PropertyDefinition definition))
            {
                throw new UnrecognizedPropertyException(element);
            }
            
            ElementReader.IResult<IEnumerable<object>> result = definition.Reader.EvaluateAndRead(element, definition);
            
            switch (definition)
            {
                case PropertyCollectionDefinition collectionDefinition:
                    if (element.Action == PropertyAction.Set)
                    {
                        collectionDefinition.ClearCollection(properties[definition.Name]);
                    }

                    object collection = properties[definition.Name];
                    foreach (object propertyValue in result.Value)
                    {
                        collectionDefinition.AddToCollection(collection, propertyValue);
                    }

                    break;
                case PropertyDictionaryDefinition dictionaryDefinition:
                    if (element.Action == PropertyAction.Set)
                    {
                        dictionaryDefinition.ClearDictionary(properties[definition.Name]);
                    }

                    object dictionary = properties[definition.Name];
                    foreach (object value in result.Value)
                    {
                        var kvp = (Box<KeyValuePair<string, string>>) value;
                        dictionaryDefinition.AddToDictionary(dictionary, kvp.Value.Key, kvp.Value.Value);
                    }

                    break;
                default:
                    if (element.Action != PropertyAction.Set)
                    {
                        throw new InvalidPropertyActionException(element,
                            "Properties that are not a collection may only set values");
                    }

                    properties[definition.Name] = result.Value.First();
                    break;
            }

            return result.Terminate;
        }

        private bool ReadCommand(CommandElement element)
        {
            if (!commandDefinitionLookup.TryGetValue(element.CommandName, out CommandDefinition definition))
            {
                throw new UnrecognizedCommandException(element);
            }
            
            ElementReader.IResult<IEnumerable<object>> result = definition.Reader.EvaluateAndRead(element, definition);
            return result.Terminate;
        }
    }
    
    public sealed class UnrecognizedSettingsElementException : Exception
    {
        public UnrecognizedSettingsElementException(ConfigElement element)
            : base($"The element '{element}' is not recongized and cannot be compiled.")
        {
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
    
    public class InvalidPropertyActionException : Exception
    {
        public InvalidPropertyActionException(PropertyElement element, string message)
            : base(string.Format("Property action '{0}' is not valid for property '{1}'. {2}",
                element.Action, element, message))
        {
            
        }
    }
    
    public sealed class DuplicateConfigurationGroupNameException : Exception
    {
        public DuplicateConfigurationGroupNameException(ConfigurationGroupElement newGroup, ConfigurationGroup existingGroup)
            : base(string.Format("A configuration group name '{0}' has already been defined:\n" +
                                 "Existing group:\n{1}\n" +
                                 "Duplicate group:\n{2}",
                newGroup.ConfigurationGroupName, existingGroup, newGroup))
        {
            
        }
    }
}