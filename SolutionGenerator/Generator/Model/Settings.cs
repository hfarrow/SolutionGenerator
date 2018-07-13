using System.Collections.Generic;
using SolutionGen.Generator.Reader;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Model
{
    public class Settings
    {
        public const string PROP_GUID = "guid";
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
        public const string PROP_PROJECT_SOURCE_PATH = "module source path";
        public const string PROP_EXCLUDE = "exclude";
        public const string PROP_PROJECT_DELCARATIONS = "projects";
        
        public const string CMD_SKIP = "skip";
        public const string CMD_EXCLUDE = "exclude";
        public const string CMD_DECLARE_PROJECT = "project";

        private readonly IReadOnlyDictionary<string, object> properties;
        
        // Why is this needed? Seems out of place. Should only store the specific Configuration that was used to produce
        // these settings. Can be null.
        public readonly IReadOnlyDictionary<string, ConfigurationGroup> ConfigurationGroups;

        public T GetProperty<T>(string name) => (T) properties[name];

        public bool TryGetProperty<T>(string name, out T value)
        {
            if (properties.TryGetValue(name, out object valueObj) && valueObj is T v)
            {
                value = v;
                return true;
            }

            value = default(T);
            return false;
        }

        public Settings(IReadOnlyDictionary<string, object> properties,
            IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups)
        {
            this.properties = properties;
            ConfigurationGroups = configurationGroups;
        }

        public Settings ExpandVariablesInCopy()
        {
            var copy = new Dictionary<string, object>(properties);
            foreach (string propertyName in properties.Keys)
            {
                copy[propertyName] = ExpandableVar.ExpandAllForProperty(propertyName, copy[propertyName],
                    ExpandableVar.ExpandableVariables);

            }
            
            return new Settings(copy, ConfigurationGroups);
        }

        public Dictionary<string, object> CopyProperties()
        {
            var copy = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kvp in properties)
            {
                copy[kvp.Key] = SettingsReader.GetPropertyDefinition(kvp.Key).CloneValue(kvp.Value);
            }

            return copy;
        }
    }
}