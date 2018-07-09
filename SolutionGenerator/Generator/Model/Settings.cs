﻿using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Reader;

namespace SolutionGen.Generator.Model
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
        public const string PROP_EXCLUDE = "exclude";
        public const string PROP_PROJECT_DELCARATIONS = "projects";
        
        public const string CMD_SKIP = "skip";
        public const string CMD_DECLARE_PROJECT = "project";

        private readonly IReadOnlyDictionary<string, object> properties;
        public readonly IReadOnlyDictionary<string, ConfigurationGroup> ConfigurationGroups;

        public bool ContainsProperty(string name) => properties.ContainsKey(name);
        public bool ContainsProperty<T>(string name) => properties.TryGetValue(name, out object value) && value is T;
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
    }
}