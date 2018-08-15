using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Reader
{
    public class ProjectSettingsReader : SettingsReader
    {
        private static readonly List<PropertyDefinition> propertyDefinitions = new List<PropertyDefinition>
        {
            // Module / Project Settings
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_GUID, string.Empty),
            
            new PropertyDefinition<string, StringPropertyReader>(
                Settings.PROP_ROOT_NAMESPACE, $"$({ExpandableVar.VAR_SOLUTION_NAME})"),
            
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_PROJECT_SOURCE_PATH, string.Empty),
            
            new PropertyCollectionDefinition<HashSet<IPattern>, IPattern, PatternPropertyReader>(
                Settings.PROP_INCLUDE_FILES, new HashSet<IPattern> {new GlobPattern(".{cs,txt,json,xml,md}", false)}),
            
            new PropertyCollectionDefinition<HashSet<IPattern>, IPattern, PatternPropertyReader>(
                Settings.PROP_LIB_SEARCH_PATHS, new HashSet<IPattern>{ new LiteralPattern("./", false)}),
            
            new PropertyCollectionDefinition<HashSet<IPattern>, IPattern, PatternPropertyReader>(Settings.PROP_LIB_REFS),
            
            new PropertyCollectionDefinition<HashSet<string>, string, StringPropertyReader>(Settings.PROP_PROJECT_REFS),
            
            new PropertyCollectionDefinition<HashSet<string>, string, StringPropertyReader>(
                Settings.PROP_DEFINE_CONSTANTS),
            
            new PropertyCollectionDefinition<HashSet<string>, string, StringPropertyReader>(
                Settings.PROP_PROJECT_DELCARATIONS),
            
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_TARGET_FRAMEWORK, "v4.6"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_LANGUAGE_VERSION, "6"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_DEBUG_SYMBOLS, "true"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_DEBUG_TYPE, "full"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_OPTIMIZE, "false"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_ERROR_REPORT, "prompt"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_WARNING_LEVEL, "4"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_CONFIGURATION_PLATFORM_TARGET, "AnyCPU"),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_EXCLUDE, "false"),
        };

        private static readonly Dictionary<string, PropertyDefinition> propertyDefinitionLookup =
            propertyDefinitions.ToDictionary(d => d.Name, d => d);

        public ProjectSettingsReader(Configuration configuration, Settings baseSettings, Settings defaultSettings,
            IReadOnlyDictionary<string, string> variableExpansions = null)
            : base(configuration, baseSettings, defaultSettings, variableExpansions)
        {
            var commandDefinitions = new List<CommandDefinition>
            {
                new CommandDefinition<CommandReader>(Settings.CMD_SKIP, _ => true),
                new CommandDefinition<CommandReader>(Settings.CMD_EXCLUDE, ExcludeProjectCommand),
                new CommandDefinition<CommandReader>(Settings.CMD_DECLARE_PROJECT, ProjectDeclarationCommand),
            };

            CommandDefinitionLookup =
                commandDefinitions.ToDictionary(c => c.Name, c => c);
        }

        public ProjectSettingsReader(IReadOnlyDictionary<string, string> variableExpansions)
            : this(null, null, null, variableExpansions)
        {
            
        }

        protected override Dictionary<string, PropertyDefinition> PropertyDefinitionLookup => propertyDefinitionLookup;
        protected override Dictionary<string, CommandDefinition> CommandDefinitionLookup { get; }
        
        private bool ExcludeProjectCommand(SimpleCommandElement element)
        {
            Properties[Settings.PROP_EXCLUDE] = "true";
            return true;
        }

        private bool ProjectDeclarationCommand(SimpleCommandElement element)
        {
            object projects = Properties[Settings.PROP_PROJECT_DELCARATIONS];
            var projectsDefinition =
                (PropertyCollectionDefinition) propertyDefinitionLookup[Settings.PROP_PROJECT_DELCARATIONS];

            projectsDefinition.AddToCollection(projects, element.ArgumentStr);
            VisitedProperties.Add(Settings.PROP_PROJECT_DELCARATIONS);
            
            return false;
        }
    }
}