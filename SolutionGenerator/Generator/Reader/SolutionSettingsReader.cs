using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Utils;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Reader
{
    public class SolutionSettingsReader : SettingsReader
    {
        private static readonly List<PropertyDefinition> propertyDefinitions = new List<PropertyDefinition>
        {
            new PropertyCollectionDefinition<HashSet<string>, string, StringPropertyReader>(
                Settings.PROP_TARGET_PLATFORMS, new HashSet<string>() {"Any CPU"}),

            new PropertyDictionaryDefinition<object, DictionaryPropertyReader>(Settings.PROP_CONFIGURATIONS,
                new Dictionary<string, object>
                {
                    ["default"] = new Dictionary<string, HashSet<string>>
                    {
                        ["Debug"] = new HashSet<string> {"debug"},
                        ["Release"] = new HashSet<string> {"release"},
                    }
                }),
            
            new PropertyCollectionDefinition<HashSet<IPattern>, IPattern, PatternPropertyReader>(Settings.PROP_INCLUDE_TEMPLATES),
            new PropertyCollectionDefinition<HashSet<IPattern>, IPattern, PatternPropertyReader>(Settings.PROP_INCLUDE_MODULES),
            new PropertyCollectionDefinition<HashSet<IPattern>, IPattern, PatternPropertyReader>(Settings.PROP_INCLUDE_PROJECTS),
            new PropertyCollectionDefinition<HashSet<IPattern>, IPattern, PatternPropertyReader>(Settings.PROP_GENERATE_PROJECTS),
            new PropertyCollectionDefinition<HashSet<IPattern>, IPattern, PatternPropertyReader>(Settings.PROP_INCLUDE_BUILD_TASKS),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_BUILD_COMMAND, ""),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_BEFORE_BUILD_COMMAND, ""),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_AFTER_BUILD_COMMAND, ""),
            new PropertyDefinition<string, StringPropertyReader>(Settings.PROP_OPEN_SOLUTION_COMMAND, ""),
        };

        private static readonly Dictionary<string, PropertyDefinition> propertyDefinitionLookup =
            propertyDefinitions.ToDictionary(d => d.Name, d => d);

        public SolutionSettingsReader(IReadOnlyDictionary<string, string> variableExpansions = null)
            : base(variableExpansions)
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
