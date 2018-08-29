using System;
using System.Collections.Generic;
using System.IO;
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
        
        public string OutputDir =>
            new DirectoryInfo(Settings.GetProperty<string>(Settings.PROP_OUTPUT_DIR)).FullName;
        
        public string ToolsVersion =>
            Settings.GetProperty<string>(Settings.PROP_MSBUILD_TOOLS_VERSION);

        public IEnumerable<string> TargetPlatforms =>
            Settings.GetProperty<IReadOnlyCollection<string>>(Settings.PROP_TARGET_PLATFORMS);

        public IReadOnlyCollection<IPattern> IncludedProjectsPatterns =>
            Settings.GetProperty<IReadOnlyCollection<IPattern>>(Settings.PROP_INCLUDE_PROJECTS);
        
        public IReadOnlyCollection<IPattern> GeneratedProjectsPatterns =>
            Settings.GetProperty<IReadOnlyCollection<IPattern>>(Settings.PROP_GENERATE_PROJECTS);
        
        public IReadOnlyCollection<IPattern> IncludeBuildTasksPattern =>
            Settings.GetProperty<IReadOnlyCollection<IPattern>>(Settings.PROP_INCLUDE_BUILD_TASKS);

        public IReadOnlyCollection<string> BuildCommands =>
            Settings.GetProperty<IReadOnlyCollection<string>>(Settings.PROP_BUILD_COMMANDS);
        
        public IReadOnlyCollection<string> BeforeBuildCommands =>
            Settings.GetProperty<IReadOnlyCollection<string>>(Settings.PROP_BEFORE_BUILD_COMMANDS);

        public IReadOnlyCollection<string> AfterBuildCommands =>
            Settings.GetProperty<IReadOnlyCollection<string>>(Settings.PROP_AFTER_BUILD_COMMANDS);
        
        public string OpenCommand =>
            Settings.GetProperty<string>(Settings.PROP_OPEN_SOLUTION_COMMAND);
        
        private readonly Dictionary<string, bool> includedProjectMap  = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> generatedProjectMap  = new Dictionary<string, bool>();
        public HashSet<string> IncludedProjects => includedProjectMap.Keys.ToHashSet();

        public IReadOnlyCollection<string> BuildTasksFiles { get; }
        
        public Solution(string name, Settings settings, string solutionConfigDir,
            IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups)
        {
            Name = name;
            Guid = Guid.NewGuid();
            Settings = settings;
            SolutionConfigDir = solutionConfigDir;
            ConfigurationGroups = configurationGroups;

            BuildTasksFiles = FileUtil.GetFiles(
                Path.GetRelativePath(Directory.GetCurrentDirectory(), SolutionConfigDir),
                IncludeBuildTasksPattern.Where(p => !p.Negated),
                IncludeBuildTasksPattern.Where(p => p.Negated));
        }

        public bool CanIncludeProject(string projectName)
        {
            // ReSharper disable once InlineOutVariableDeclaration
            // Inlining the out variable would cause it to default to false instead of true.
            bool canInclude = true;
            
            if (IncludedProjectsPatterns.Count > 0 &&
                !includedProjectMap.TryGetValue(projectName, out canInclude))
            {
                canInclude = IncludedProjectsPatterns.Any(p => p.IsMatch(projectName));
                includedProjectMap[projectName] = canInclude;
            }

            return canInclude;
        }

        public bool CanGenerateProject(string projectName)
        {
            // ReSharper disable once InlineOutVariableDeclaration
            // Inlining the out variable would cause it to default to false instead of true.
            bool canGenerate = true;
            
            if (GeneratedProjectsPatterns.Count > 0 &&
                !generatedProjectMap.TryGetValue(projectName, out canGenerate))
            {
                canGenerate = GeneratedProjectsPatterns.Any(p => p.IsMatch(projectName));
                generatedProjectMap[projectName] = canGenerate;
            }

            return canGenerate;
        }

        public IReadOnlyCollection<string> GetBuildTasksFilesForProject(Project project)
        {
            return BuildTasksFiles
                .Select(f => Path.GetRelativePath(Path.Combine(OutputDir, project.RelativeSourcePath), f)).ToHashSet();
        }
    }
}