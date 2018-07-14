using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Reader
{
    public class SettingsReader
    {
        private static readonly List<PropertyDefinition> propertyDefinitions = new List<PropertyDefinition>
        {
            // Module / Project Settings
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_GUID, string.Empty),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_ROOT_NAMESPACE, $"$({ExpandableVar.VAR_SOLUTION_NAME})"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_PROJECT_SOURCE_PATH, string.Empty),
            new PropertyCollectionDefinition<HashSet<IPath>, IPath, PathPropertyReader>(Settings.PROP_INCLUDE_FILES,
                new HashSet<IPath> {new GlobPath(".{cs,txt,json,xml,md}")}),
            new PropertyCollectionDefinition<HashSet<IPath>, IPath, PathPropertyReader>(Settings.PROP_EXCLUDE_FILES),
            new PropertyCollectionDefinition<HashSet<string>, string, StringPropertyReader>(Settings.PROP_LIB_REFS),
            new PropertyCollectionDefinition<HashSet<string>, string, StringPropertyReader>(Settings.PROP_PROJECT_REFS),
            new PropertyCollectionDefinition<HashSet<string>, string, StringPropertyReader>(Settings.PROP_DEFINE_CONSTANTS),
            new PropertyCollectionDefinition<HashSet<string>, string, StringPropertyReader>(Settings.PROP_PROJECT_DELCARATIONS),
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
        };

        private static readonly Dictionary<string, PropertyDefinition> propertyDefinitionLookup =
            propertyDefinitions.ToDictionary(d => d.Name, d => d);

        private readonly Dictionary<string, CommandDefinition> commandDefinitionLookup;

        private Dictionary<string, object> properties = new Dictionary<string, object>();
        private readonly HashSet<string> visitedProperties = new HashSet<string>();

        private readonly Dictionary<string, ConfigurationGroup> configurationGroups =
            new Dictionary<string, ConfigurationGroup>();

        public Configuration Configuration { get; }
        private readonly Settings defaultSettings;
        private readonly Settings baseSettings;
        private readonly BooleanExpressionParser conditionalParser;
        private readonly IReadOnlyDictionary<string, string> variableExpansions;

        public SettingsReader(Configuration configuration, Settings baseSettings, Settings defaultSettings,
            IReadOnlyDictionary<string, string> variableExpansions = null)
        {
            Configuration = configuration;
            conditionalParser = new BooleanExpressionParser();
            conditionalParser.SetConditionalConstants(configuration.Conditionals);
            this.variableExpansions = variableExpansions;
            this.baseSettings = baseSettings;
            this.defaultSettings = defaultSettings;

            var commandDefinitions = new List<CommandDefinition>
            {
                new CommandDefinition<CommandReader>(Settings.CMD_SKIP, _ => true),
                new CommandDefinition<CommandReader>(Settings.CMD_EXCLUDE, ExcludeProjectCommand),
                new CommandDefinition<CommandReader>(Settings.CMD_DECLARE_PROJECT, ProjectDeclarationCommand),
            };

            commandDefinitionLookup =
                commandDefinitions.ToDictionary(c => c.Name, c => c);
        }

        public SettingsReader(IReadOnlyDictionary<string, string> variableExpansions = null)
        {
            conditionalParser = new BooleanExpressionParser();
            this.variableExpansions = variableExpansions;
            defaultSettings = GetDefaultSettings();

            if (variableExpansions != null)
            {
                Log.WriteLine("Settings reader variable expansions:{0}", variableExpansions.Count > 0 ? "" : "<none>" );
                Log.WriteIndentedCollection(
                    variableExpansions,
                    (kvp) => string.Format("{0} => {1}", kvp.Key, kvp.Value));
            }
        }

        public static PropertyDefinition GetPropertyDefinition(string propertyName)
        {
            return propertyDefinitionLookup[propertyName];
        }

        /// <summary>
        /// Get the hard coded default settings defined by property definitions.
        /// These are not the "settings template.default" that can override these defaults
        /// from the solution object.
        /// </summary>
        /// <returns>Default settings.</returns>
        public static Settings GetDefaultSettings()
        {
            return new Settings(GetDefaultPropertiesDictionary(), null);
        }

        public static Dictionary<string, object> GetDefaultPropertiesDictionary()
        {
            return propertyDefinitionLookup.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetOrCloneDefaultValue());
        }

        public Settings Read(ObjectElement settingsObject)
        {
            if (Configuration != null)
            {
                Log.WriteLine("Reading settings element for configuration '{0} - {1}: {2}",
                    Configuration.GroupName, Configuration.Name, settingsObject);
            }
            else
            {
                Log.WriteLine("Reading settings element for static configuration: {0}", settingsObject);
            }

            using (new Log.ScopedIndent())
            {
                if (baseSettings == null)
                {
                    properties = defaultSettings != null
                        ? defaultSettings.CopyProperties()
                        : GetDefaultPropertiesDictionary();
                }
                else
                {
                    properties = new Dictionary<string, object>();
                    foreach (KeyValuePair<string, PropertyDefinition> kvp in propertyDefinitionLookup)
                    {
                        if (baseSettings.TryGetProperty(kvp.Key, out object value))
                        {
                            properties[kvp.Key] = kvp.Value.CloneValue(value);
                        }
                    }
                }

                IEnumerable<ConfigElement> elements = EvaluateConditionalBlocks(settingsObject.Elements);
                foreach (ConfigElement element in elements)
                {
                    bool terminate;
                    switch (element)
                    {
                        case ObjectElement objElement
                            when element is ObjectElement
                                 && objElement.ElementHeading.Type == SectionType.CONFIGURATION:
                            terminate = ReadConfiguration(objElement);
                            break;
                        
                        case PropertyElement propertyElement when element is PropertyElement:
                            terminate = ReadProperty(propertyElement);
                            break;

                        case SimpleCommandElement cmdElement when element is SimpleCommandElement:
                            terminate = ReadCommand(cmdElement);
                            break;
                        
                        case ObjectElement _ when element is ObjectElement:
                            // settings objects do not have nested objects; however, SettingsReader is also used to read
                            // other object types such a "solution" and "module" that do have nested objects but are not
                            // read as part of the Settings.
                            // Note: Nested settings could be supported and would produce nested dictionaries in 'properties'
                            terminate = false;
                            break;

                        case CommentElement _ when element is CommentElement:
                            terminate = false;
                            break;

                        default:
                            throw new UnrecognizedSettingsElementException(element);
                    }

                    if (terminate)
                    {
                        break;
                    }
                }

                ExpandVariables(properties);

                Log.WriteLine("Finished reading settings element:");
                Log.WriteIndentedCollection(
                    GetVisitedProperties(),
                    kvp => string.Format("{0} => {1}",
                        kvp.Key, GetPropertyDefinition(kvp.Key).PrintValue(kvp.Value)));

                return new Settings(properties, configurationGroups);
            }
        }

        private IEnumerable<ConfigElement> EvaluateConditionalBlocks(IEnumerable<ConfigElement> elements)
        {
            foreach (ConfigElement element in elements)
            {
                if (element is ConditionalBlockElement block)
                {
                    if (ElementReader.EvaluateConditional(block.ConditionalExpression, conditionalParser))
                    {
                        foreach (ConfigElement blockElement in EvaluateConditionalBlocks(block.Elements))
                        {
                            yield return blockElement;
                        }
                    }
                }
                else
                {
                    yield return element;
                }
            }
        }

        private IEnumerable<KeyValuePair<string, object>> GetVisitedProperties()
        {
            return properties.Where(kvp => visitedProperties.Contains(kvp.Key));
        }
        
        private bool ReadConfiguration(ObjectElement element)
        {
            Log.WriteLine("Reading configuration declaration: {0}", element);

            using (new Log.ScopedIndent())
            {
                if (configurationGroups.TryGetValue(element.ElementHeading.Name,
                    out ConfigurationGroup existingGroup))
                {
                    throw new DuplicateConfigurationGroupNameException(element, existingGroup);
                }

                ConfigurationGroup group;
                try
                {
                    Dictionary<string, Configuration> configurationMap = element.Elements
                        .Cast<PropertyElement>()
                        .Select(p => new Configuration(element.ElementHeading.Name, p.FullName,
                            ((ArrayValue) p.ValueElement).Values
                            .Select(v => v.ToString())
                            .Concat(new []{element.ElementHeading.Name})
                            .ToHashSet()))
                        .ToDictionary(cfg => cfg.Name, cfg => cfg);
                    
                    group = new ConfigurationGroup(element.ElementHeading.Name,
                        configurationMap);
                }
                catch (Exception ex)
                {
                    throw new InvalidConfigurationObjectElement(element, ex);
                }

                configurationGroups[group.Name] = group;
                Log.WriteLine("Read configuration: {0}", group);

                // Never terminate
                return false;
            }
        }

        private bool ReadProperty(PropertyElement element)
        {
            if (!propertyDefinitionLookup.TryGetValue(element.FullName, out PropertyDefinition definition))
            {
                throw new UnrecognizedPropertyException(element);
            }

            ElementReader.IResult<IEnumerable<object>> result =
                definition.Reader.EvaluateAndRead(element, definition, conditionalParser);
            
            if (result.HasValue)
            {
                visitedProperties.Add(definition.Name);
                
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
            }

            return result.Terminate;
        }

        private bool ReadCommand(SimpleCommandElement element)
        {
            if (!commandDefinitionLookup.TryGetValue(element.CommandName, out CommandDefinition definition))
            {
                throw new UnrecognizedCommandException(element);
            }

            ElementReader.IResult<IEnumerable<object>> result =
                definition.Reader.EvaluateAndRead(element, definition, conditionalParser);
            
            return result.Terminate;
        }

        private bool ExcludeProjectCommand(SimpleCommandElement element)
        {
            properties[Settings.PROP_EXCLUDE] = "true";
            return true;
        }

        private bool ProjectDeclarationCommand(SimpleCommandElement element)
        {
            object projects = properties[Settings.PROP_PROJECT_DELCARATIONS];
            var projectsDefinition =
                (PropertyCollectionDefinition) propertyDefinitionLookup[Settings.PROP_PROJECT_DELCARATIONS];

            projectsDefinition.AddToCollection(projects, element.ArgumentStr);

            visitedProperties.Add(Settings.PROP_PROJECT_DELCARATIONS);
            
            return false;
        }

        private void ExpandVariables(IDictionary<string, object> expandableProperties)
        {
            var modifiedProperties = new Dictionary<string, object>();
            if (variableExpansions != null && variableExpansions.Count > 0)
            {
                foreach (KeyValuePair<string, object> kvp in expandableProperties)
                {
                    object expanded = ExpandableVar.ExpandAllForProperty(kvp.Key, kvp.Value, variableExpansions);
                    modifiedProperties[kvp.Key] = expanded;
                }

                foreach (KeyValuePair<string,object> kvp in modifiedProperties)
                {
                    expandableProperties[kvp.Key] = kvp.Value;
                }
            }
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
        public DuplicateConfigurationGroupNameException(ObjectElement newGroup, ConfigurationGroup existingGroup)
            : base(string.Format("A configuration group name '{0}' has already been defined:\n" +
                                 "Existing group:\n{1}\n" +
                                 "Duplicate group:\n{2}",
                newGroup.ElementHeading.Name, existingGroup, newGroup))
        {
            
        }
    }

    public sealed class InvalidConfigurationObjectElement : Exception
    {
        public InvalidConfigurationObjectElement(ObjectElement element, Exception innerException)
            : base($"Configuration group '{element}' contains contains invalid elements." +
                   $"It must contain only properties that are arrays such as 'Debug = [debug,test]'",
                innerException)
        {
            
        }
        
    }
}