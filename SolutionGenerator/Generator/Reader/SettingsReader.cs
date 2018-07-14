using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Reader
{
    public abstract class SettingsReader
    {
        protected Dictionary<string, object> Properties = new Dictionary<string, object>();
        protected readonly HashSet<string> VisitedProperties = new HashSet<string>();

        private readonly Dictionary<string, ConfigurationGroup> configurationGroups =
            new Dictionary<string, ConfigurationGroup>();

        public Configuration Configuration { get; }
        private readonly Settings defaultSettings;
        private readonly Settings baseSettings;
        private readonly BooleanExpressionParser conditionalParser;
        private readonly IReadOnlyDictionary<string, string> variableExpansions;

        protected SettingsReader(IReadOnlyDictionary<string, string> variableExpansions)
        {
            this.variableExpansions = variableExpansions;
            conditionalParser = new BooleanExpressionParser();
            defaultSettings = GetDefaultSettings();
            
            if (variableExpansions != null)
            {
                Log.WriteLine("Settings reader variable expansions:{0}", variableExpansions.Count > 0 ? "" : "<none>" );
                Log.WriteIndentedCollection(
                    variableExpansions,
                    (kvp) => string.Format("{0} => {1}", kvp.Key, kvp.Value));
            }
        }

        protected SettingsReader(Configuration configuration, Settings baseSettings, Settings defaultSettings,
            IReadOnlyDictionary<string, string> variableExpansions = null)
            : this(variableExpansions)
        {
            Configuration = configuration;
            if (configuration != null)
            {
                conditionalParser.SetConditionalConstants(configuration.Conditionals);
            }
            this.baseSettings = baseSettings;
            this.defaultSettings = defaultSettings;
        }
        
        protected abstract Dictionary<string, PropertyDefinition> PropertyDefinitionLookup { get; }
        protected abstract Dictionary<string, CommandDefinition> CommandDefinitionLookup { get; }

        public PropertyDefinition GetPropertyDefinition(string propertyName)
        {
            return PropertyDefinitionLookup[propertyName];
        }

        /// <summary>
        /// Get the hard coded default settings defined by property definitions.
        /// These are not the "settings template.default" that can override these defaults
        /// from the solution object.
        /// </summary>
        /// <returns>Default settings.</returns>
        public Settings GetDefaultSettings()
        {
            return new Settings(GetDefaultPropertiesDictionary(), null, GetPropertyDefinition);
        }

        public Dictionary<string, object> GetDefaultPropertiesDictionary()
        {
            return PropertyDefinitionLookup.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetOrCloneDefaultValue());
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
                    Properties = defaultSettings != null
                        ? defaultSettings.CopyProperties()
                        : GetDefaultPropertiesDictionary();
                }
                else
                {
                    Properties = new Dictionary<string, object>();
                    foreach (KeyValuePair<string, PropertyDefinition> kvp in PropertyDefinitionLookup)
                    {
                        if (baseSettings.TryGetProperty(kvp.Key, out object value))
                        {
                            Properties[kvp.Key] = kvp.Value.CloneValue(value);
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

                ExpandVariables(Properties);

                Log.WriteLine("Finished reading settings element:");
                Log.WriteIndentedCollection(
                    GetVisitedProperties(),
                    kvp => string.Format("{0} => {1}",
                        kvp.Key, GetPropertyDefinition(kvp.Key).PrintValue(kvp.Value)));

                return new Settings(Properties, configurationGroups, GetPropertyDefinition);
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
            return Properties.Where(kvp => VisitedProperties.Contains(kvp.Key));
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
            if (!PropertyDefinitionLookup.TryGetValue(element.FullName, out PropertyDefinition definition))
            {
                throw new UnrecognizedPropertyException(element);
            }

            ElementReader.IResult<IEnumerable<object>> result =
                definition.Reader.EvaluateAndRead(element, definition, conditionalParser);
            
            if (result.HasValue)
            {
                VisitedProperties.Add(definition.Name);
                
                switch (definition)
                {
                    case PropertyCollectionDefinition collectionDefinition:
                        if (element.Action == PropertyAction.Set)
                        {
                            collectionDefinition.ClearCollection(Properties[definition.Name]);
                        }

                        object collection = Properties[definition.Name];
                        foreach (object propertyValue in result.Value)
                        {
                            collectionDefinition.AddToCollection(collection, propertyValue);
                        }

                        break;
                    case PropertyDictionaryDefinition dictionaryDefinition:
                        if (element.Action == PropertyAction.Set)
                        {
                            dictionaryDefinition.ClearDictionary(Properties[definition.Name]);
                        }

                        object dictionary = Properties[definition.Name];
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

                        Properties[definition.Name] = result.Value.First();
                        break;
                }
            }

            return result.Terminate;
        }

        private bool ReadCommand(SimpleCommandElement element)
        {
            if (!CommandDefinitionLookup.TryGetValue(element.CommandName, out CommandDefinition definition))
            {
                throw new UnrecognizedCommandException(element);
            }

            ElementReader.IResult<IEnumerable<object>> result =
                definition.Reader.EvaluateAndRead(element, definition, conditionalParser);
            
            return result.Terminate;
        }

        private void ExpandVariables(IDictionary<string, object> expandableProperties)
        {
            var modifiedProperties = new Dictionary<string, object>();
            if (variableExpansions != null && variableExpansions.Count > 0)
            {
                foreach (KeyValuePair<string, object> kvp in expandableProperties)
                {
                    object expanded = ExpandableVar.ExpandAllForProperty(kvp.Key, kvp.Value, variableExpansions,
                        GetPropertyDefinition);
                    
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
                   "It must contain only properties that are arrays such as \'Debug = [debug,test]\'",
                innerException)
        {
            
        }
        
    }
}