using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Model
{
    public class Project
    {
        public class Identifier
        {
            public string Name { get; }
            public Guid Guid { get; }
            public string SourcePath { get; }
            
            public Identifier(string name, Guid guid, string sourcePath)
            {
                Name = name;
                Guid = guid;
                SourcePath = sourcePath;
            }
        }
        
        public Solution Solution { get; }
        public string ModuleName { get; }
        public Configuration Configuration { get; }
        public Settings Settings { get; }

        public string Name => Id.Name;
        public Guid Guid => Id.Guid;
        public string AbsoluteSourcePath => Id.SourcePath;
        public string RelativeSourcePath { get; }
        
        public IReadOnlyCollection<string> IncludeFiles { get; }
        public IReadOnlyCollection<string> LibRefs { get; }
        public IReadOnlyCollection<string> ProjectRefs { get; }
        public IReadOnlyCollection<string> CustomContents { get; }

        public readonly Identifier Id;

        public Project(Solution solution, string moduleName, Identifier id,
            Configuration configuration, Settings settings)
        {
            Solution = solution;
            ModuleName = moduleName;
            Id = id;
            Configuration = configuration;
            Settings = settings;

            RelativeSourcePath = System.IO.Path.GetRelativePath(Solution.SolutionConfigDir, AbsoluteSourcePath);
            
            // Include files, exclude files, lib refs
            var includeFilesProperty = Settings.GetProperty<HashSet<IPattern>>(Settings.PROP_INCLUDE_FILES);
            var libSearchPaths = Settings.GetProperty<HashSet<IPattern>>(Settings.PROP_LIB_SEARCH_PATHS);
            var libRefsValues = Settings.GetProperty<HashSet<IPattern>>(Settings.PROP_LIB_REFS);
            var projectRefsValues = Settings.GetProperty<HashSet<string>>(Settings.PROP_PROJECT_REFS);
            CustomContents = Settings.GetProperty<List<string>>(Settings.PROP_CUSTOM_CSPROJ_CONTENTS);

            Log.Debug(
                "Matching path patterns to source files for project '{0}' as configuration '{1} - {2}' at base directory '{3}'",
                id.Name, configuration.GroupName, configuration.Name, Solution.SolutionConfigDir);

            IncludeFiles = FileUtil.GetFiles(Solution.SolutionConfigDir,
                includeFilesProperty.Where(p => !p.Negated),
                includeFilesProperty.Where(p => p.Negated));

            IPattern[] invalidPatternTypes = libSearchPaths.Where(p => !(p is LiteralPattern)).ToArray();
            if (invalidPatternTypes.Length > 0)
            {
                Log.Warn("Invalid lib search paths:");
                Log.IndentedCollection(invalidPatternTypes, Log.Warn);
            }

            IEnumerable<string> directories = libSearchPaths
                .OfType<LiteralPattern>()
                .Select(p => p.Value);

            Log.Debug(
                "Matching path patterns to libs refs for project '{0}' as configuration '{1} - {2}' at base directory '{3}",
                id.Name, configuration.GroupName, configuration.Name, Solution.SolutionConfigDir);
            
            LibRefs = FileUtil.GetFiles(directories,
                libRefsValues.Where(p => !p.Negated),
                libRefsValues.Where(p => p.Negated),
                Solution.SolutionConfigDir);
            
            ProjectRefs = projectRefsValues
                .ToHashSet();
        }
    }
}