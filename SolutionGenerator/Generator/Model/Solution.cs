﻿using System;
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
        public IReadOnlyDictionary<string, ConfigurationGroup> ConfigurationGroups { get; private set; }
        public readonly FileUtil.ResultCache FileCache;
        
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
        public HashSet<string> IncludedProjects
        {
            get
            {
                lock (includedProjectMap)
                {
                    return includedProjectMap.Keys.ToHashSet();
                }
            }
        }

        public IReadOnlyCollection<string> BuildTasksFiles { get; }
        
        public Solution(
            string name,
            Settings settings,
            string solutionConfigDir,
            IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups,
            FileUtil.ResultCache fileCache)
        {
            Name = name;
            Guid = Guid.NewGuid();
            Settings = settings;
            SolutionConfigDir = solutionConfigDir;
            ConfigurationGroups = configurationGroups;
            FileCache = fileCache;

            using (new Log.ScopedTimer(Log.Level.Debug, "Get Build Task Files"))
            {
                BuildTasksFiles = FileUtil.GetFiles(
                    fileCache,
                    Path.GetRelativePath(Directory.GetCurrentDirectory(), SolutionConfigDir),
                    IncludeBuildTasksPattern.Where(p => !p.Negated),
                    IncludeBuildTasksPattern.Where(p => p.Negated));
            }
        }

        public bool CanIncludeProject(string projectName)
        {
            // ReSharper disable once InlineOutVariableDeclaration
            // Inlining the out variable would cause it to default to false instead of true.
            bool canInclude = true;
            
            // TODO: Keep this cached list of project directly in module reader so no synchronization is required.
            // Would require the reader task to provide the included projects as a result
            lock (includedProjectMap)
            {
                if (IncludedProjectsPatterns.Count > 0 &&
                    !includedProjectMap.TryGetValue(projectName, out canInclude))
                {
                    canInclude = IncludedProjectsPatterns.Any(p => p.IsMatch(projectName));
                    includedProjectMap[projectName] = canInclude;
                }
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

        public void FilterConfigurations(string[] nameFilter)
        {
            Log.Info("Filtering configurations with provided filter:");
            Log.IndentedCollection(nameFilter, Log.Info);
            
            var filteredGroups = new Dictionary<string, ConfigurationGroup>();
            foreach (KeyValuePair<string,ConfigurationGroup> kvp in ConfigurationGroups)
            {
                filteredGroups[kvp.Key] = new ConfigurationGroup(kvp.Key,
                    kvp.Value.Configurations.Values.Where(c => nameFilter.Contains(c.Name))
                        .ToDictionary(c => c.Name, c => c));
            }

            ConfigurationGroups = filteredGroups;

            ConfigurationGroup[] emptyGroups = ConfigurationGroups.Values
                .Where(g => !g.Configurations.Any()).ToArray();
            
            foreach (ConfigurationGroup emptyGroup in emptyGroups)
            {
                Log.Error(
                    "The provided configuration filter resulted in group '{0}' containing no configurations. The provided filter was:",
                    emptyGroup.Name);
                Log.IndentedCollection(nameFilter, Log.Error);
            }

            if (emptyGroups.Any())
            {
                throw new ArgumentException("Invalid filter results in one more more groups with zero configurations.",
                    nameof(nameFilter));
            }
            
            ConfigurationGroups = filteredGroups;
        }

        public void FilterMasterConfigurations(string[] nameFilter)
        {
            ConfigurationGroups = ConfigurationGroups.Values.Where(c => nameFilter.Contains(c.Name))
                .ToDictionary(c => c.Name, c => c);
        }
    }
}