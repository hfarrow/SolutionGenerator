using System;
using System.Collections.Generic;
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

        public IReadOnlyCollection<string> TargetPlatforms =>
            Settings.GetProperty<IReadOnlyCollection<string>>(Settings.PROP_TARGET_PLATFORMS);

        public IReadOnlyCollection<string> GeneratedProjects =>
            Settings.GetProperty<IReadOnlyCollection<string>>(Settings.PROP_GENERATE_PROJECTS);
        
        public Solution(string name, Settings settings, string solutionConfigDir,
            IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups)
        {
            Name = name;
            Guid = Guid.NewGuid();
            Settings = settings;
            SolutionConfigDir = solutionConfigDir;
            ConfigurationGroups = configurationGroups;
        }
    }
}