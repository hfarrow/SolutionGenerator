using System;
using System.Collections.Generic;
using System.IO;
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
            public string AbsoluteSourcePath { get; }
            public string RelativeSourcePath { get; }

            public Identifier(string name, Guid guid, string absoluteSourcePath, string relativeSourcePath)
            {
                Name = name;
                Guid = guid;
                AbsoluteSourcePath = absoluteSourcePath;
                RelativeSourcePath = relativeSourcePath;
            }
        }
        
        public Solution Solution { get; }
        public string ModuleName { get; }
        public Configuration Configuration { get; }
        public Settings Settings { get; }

        public string Name => Id.Name;
        public Guid Guid => Id.Guid;
        public string AbsoluteSourcePath => Id.AbsoluteSourcePath;
        public string RelativeSourcePath => Id.RelativeSourcePath;
        
        public IReadOnlyCollection<string> IncludeFiles { get; }
        public IReadOnlyCollection<string> LibRefs { get; }
        public IReadOnlyCollection<string> ProjectRefs { get; private set; }
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
           
            // Include files, exclude files, lib refs
            var includeFilesProperty = Settings.GetProperty<HashSet<IPattern>>(Settings.PROP_INCLUDE_FILES);
            var libSearchPaths = Settings.GetProperty<HashSet<IPattern>>(Settings.PROP_LIB_SEARCH_PATHS);
            var libRefsValues = Settings.GetProperty<HashSet<IPattern>>(Settings.PROP_LIB_REFS);
            var projectRefsValues = Settings.GetProperty<HashSet<string>>(Settings.PROP_PROJECT_REFS);
            CustomContents = Settings.GetProperty<List<string>>(Settings.PROP_CUSTOM_CSPROJ_CONTENTS);

            Log.Debug(
                "Matching path patterns to source files for project '{0}' as configuration '{1} - {2}' at base directory '{3}'",
                id.Name, configuration.GroupName, configuration.Name, AbsoluteSourcePath);

//            using (new Log.ScopedTimer(Log.Level.Debug, "Get Include Files", id.Name))
            {
                IncludeFiles = FileUtil.GetFiles(
                    Solution.FileCache,
                    RelativeSourcePath,
                    includeFilesProperty.Where(p => !p.Negated),
                    includeFilesProperty.Where(p => p.Negated),
                    RelativeSourcePath);
            }

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

            IEnumerable<IPattern> libIncludes = libRefsValues.Where(p => !p.Negated).ToArray();
            IEnumerable<LiteralPattern> absoluteLibIncludes =
                libIncludes.OfType<LiteralPattern>().Where(p => Path.IsPathRooted(p.Value)).ToArray();
            libIncludes = libIncludes.Except(absoluteLibIncludes);


            HashSet<string> libRefs;
//            using (new Log.ScopedTimer(Log.Level.Debug, "Get Lib Ref Files", id.Name))
            {
                 libRefs = FileUtil.GetFiles(
                    Solution.FileCache,
                    directories,
                    libIncludes,
                    libRefsValues.Where(p => p.Negated),
                    Solution.SolutionConfigDir);
            }

            foreach (LiteralPattern pattern in absoluteLibIncludes)
            {
                if (!File.Exists(pattern.Value))
                {
                    Log.Warn("Absolute lib path does not exists and will not be added to references: {0}",
                        pattern.Value);
                }
                else
                {
                    Log.Debug("Found absolute path to lib reference: {0}", pattern.Value);
                    libRefs.Add(pattern.Value);
                }
            }

            LibRefs = libRefs;
            
            ProjectRefs = projectRefsValues
                .ToHashSet();
        }

        public IReadOnlyCollection<string> ExcludeProjectRefs(IReadOnlyCollection<string> excludedProjects)
        {
            string[] removedRefs = ProjectRefs.Intersect(excludedProjects).ToArray();
            ProjectRefs = ProjectRefs.Except(removedRefs).ToHashSet();
            return removedRefs;
        }
    }
}