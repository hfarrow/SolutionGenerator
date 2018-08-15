using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Model
{
    public class Solution
    {
        public string Name { get; }
        public readonly Guid Guid;
        public string SolutionConfigDir { get; }
        public readonly Settings Settings;
        public readonly IReadOnlyDictionary<string, ConfigurationGroup> ConfigurationGroups;

        public IEnumerable<string> TargetPlatforms =>
            Settings.GetProperty<IReadOnlyCollection<string>>(Settings.PROP_TARGET_PLATFORMS);

        public IReadOnlyCollection<IPattern> GeneratedProjectsPatterns =>
            Settings.GetProperty<IReadOnlyCollection<IPattern>>(Settings.PROP_GENERATE_PROJECTS);
        
        private readonly Dictionary<string, bool> generatableProjectMap  = new Dictionary<string, bool>();
        
        public Solution(string name, Settings settings, string solutionConfigDir,
            IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups)
        {
            Name = name;
            Guid = Guid.NewGuid();
            Settings = settings;
            SolutionConfigDir = solutionConfigDir;
            ConfigurationGroups = configurationGroups;
        }

        public bool CanGenerateProject(string projectName)
        {
            // ReSharper disable once InlineOutVariableDeclaration
            // Inlining the out variable would cause it to default to false instead of true.
            bool canGenerate = true;
            
            if (GeneratedProjectsPatterns.Count > 0 &&
                !generatableProjectMap.TryGetValue(projectName, out canGenerate))
            {
                canGenerate = GeneratedProjectsPatterns.Any(p => p.IsMatch(projectName));
                generatableProjectMap[projectName] = canGenerate;
            }

            return canGenerate;
        }
    }
}