using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;
using Sprache;

namespace SolutionGen.Generator.Reader
{
    public class SolutionReader
    {
        private const string DEFAULT_TEMPLATE_SETTINGS = "template.defaults";

        public Solution Solution { get; }
        public Settings TemplateDefaultSettings { get; }
        public List<ObjectElement> IncludedTemplates { get; }
        public List<ObjectElement> IncludedModules { get; }

        public SolutionReader(ObjectElement solutionElement, string solutionConfigDir)
        {
            Log.Heading("Reading solution element: {0}", solutionElement);
            using (new Log.ScopedIndent())
            {
                ExpandableVar.SetExpandableVariable(ExpandableVar.VAR_SOLUTION_NAME,
                    solutionElement.ElementHeading.Name);
                
                var settingsReader = new SolutionSettingsReader(ExpandableVar.ExpandableVariables);
                Settings settings = settingsReader.Read(solutionElement);

                Solution = new Solution(solutionElement.ElementHeading.Name, settings, solutionConfigDir,
                    GetConfigurationGroups(settings));

                ObjectElement settingsElement = solutionElement.Elements
                    .OfType<ObjectElement>()
                    .FirstOrDefault(obj => obj.ElementHeading.Type == SectionType.SETTINGS &&
                        obj.ElementHeading.Name == DEFAULT_TEMPLATE_SETTINGS);

                if (settingsElement == null)
                {
                    Log.Info(
                        "No settings named '{0}' found in solution element. " +
                        "Templates will use hard coded defaults instead.",
                        DEFAULT_TEMPLATE_SETTINGS);
                    
                    TemplateDefaultSettings = settingsReader.GetDefaultSettings();
                }
                else
                {
                    Log.Info(
                        "Settings named '{0}' found in solution element. " +
                        "Templates will default to the settings read below.",
                        DEFAULT_TEMPLATE_SETTINGS);

                    using (new Log.ScopedIndent())
                    {
                        TemplateDefaultSettings =
                            new ProjectSettingsReader(ExpandableVar.ExpandableVariables)
                                .Read(settingsElement);
                    }
                }

                IncludedTemplates = GetIncludedTemplates();
                IncludedModules = GetIncludedModules();
            }
        }

        private static Dictionary<string, ConfigurationGroup> GetConfigurationGroups(Settings settings)
        {
            var property = settings.GetProperty<Dictionary<string, object>>(Settings.PROP_CONFIGURATIONS);
            var groups = new Dictionary<string, ConfigurationGroup>();

            foreach (KeyValuePair<string,object> kvp in property)
            {
                string groupName = kvp.Key;

                if (groups.TryGetValue(kvp.Key, out ConfigurationGroup duplicate))
                {
                    throw new DuplicateConfigurationGroupNameException(groupName, duplicate);
                }

                Dictionary<string, HashSet<string>> groupConfigs =
                    ((Dictionary<string, object>) kvp.Value).ToDictionary(
                        innerKvp => innerKvp.Key,
                        innerKvp => ((IEnumerable<object>) innerKvp.Value)
                            .Select(o => o.ToString())
                            .Concat(new []{groupName})
                            .ToHashSet());
                
                IEnumerable<Configuration> configurations = groupConfigs.Select(configKvp =>
                    new Configuration(groupName, configKvp.Key, configKvp.Value));
                
                groups[groupName] =
                    new ConfigurationGroup(groupName, configurations.ToDictionary(cfg => cfg.Name, cfg => cfg));
            }

            return groups;
        }

        private List<ObjectElement> GetIncludedTemplates()
        {
            Log.Info("Loading included templates");
            return GetIncludedElements(Settings.PROP_INCLUDE_TEMPLATES, r => r.TemplateElements);
        }
        
        private List<ObjectElement> GetIncludedModules()
        {
            Log.Info("Loading included modules");
            return GetIncludedElements(Settings.PROP_INCLUDE_MODULES, r => r.ModuleElements);
        }

        private List<ObjectElement> GetIncludedElements(string pathsPropertyName,
            Func<DocumentReader, IEnumerable<ObjectElement>> elementSelector)
        {
            using (new Log.ScopedIndent())
            {
                IEnumerable<ObjectElement> modules = new List<ObjectElement>();
                var includes = Solution.Settings.GetProperty<HashSet<IPattern>>(pathsPropertyName);
                HashSet<string> includePaths = FileUtil.GetFiles(Solution.SolutionConfigDir, includes, null);

                foreach (string includePath in includePaths)
                {
                    DocumentReader reader = ParseInclude(includePath);
                    reader.ParseElements();
                    modules = modules.Concat(elementSelector(reader));
                }

                return modules.ToList();
            }
        }

        private DocumentReader ParseInclude(string filePath)
        {
            Log.Info("Parsing included document at path '{0}'", filePath);
            using (new Log.ScopedIndent())
            {
                string configText = File.ReadAllText(filePath);
                IResult<ConfigDocument> result = DocumentParser.Document.TryParse(configText);
                if (!result.WasSuccessful)
                {
                    throw new DocumentParseException(filePath, result.ToString());
                }

                ConfigDocument configDoc = result.Value;
                var reader = new DocumentReader(configDoc, Solution.SolutionConfigDir);
                Log.Info("Finished parsing included document at path '{0}'", filePath);
                return reader;
            }
        }
    }
    
    public sealed class DuplicateConfigurationGroupNameException : Exception
    {
        public DuplicateConfigurationGroupNameException(string duplicateName, ConfigurationGroup existingGroup)
            : base(string.Format("A configuration group name '{0}' has already been defined:\n" +
                                 "Existing group:\n{1}\n" +
                                 duplicateName, existingGroup))
        {

        }
    }
}