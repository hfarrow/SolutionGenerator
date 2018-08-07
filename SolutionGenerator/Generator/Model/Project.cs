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

        public string Name => id.Name;
        public Guid Guid => id.Guid;
        public string AbsoluteSourcePath => id.SourcePath;
        public string RelativeSourcePath { get; }
        
        public IReadOnlyCollection<string> IncludeFiles { get; }
        public IReadOnlyCollection<string> LibRefs { get; }
        public IReadOnlyCollection<string> ProjectRefs { get; }

        private readonly Identifier id;

        public Project(Solution solution, string moduleName, Identifier id,
            Configuration configuration, Settings settings)
        {
            Solution = solution;
            ModuleName = moduleName;
            this.id = id;
            Configuration = configuration;
            Settings = settings;

            RelativeSourcePath = System.IO.Path.GetRelativePath(Solution.SolutionConfigDir, AbsoluteSourcePath);
            
            // Include files, exclude files, lib refs
            var includeFilesProperty = Settings.GetProperty<HashSet<IPath>>(Settings.PROP_INCLUDE_FILES);
            var libRefsValues = Settings.GetProperty<HashSet<IPath>>(Settings.PROP_LIB_REFS);
            var projectRefsValues = Settings.GetProperty<HashSet<string>>(Settings.PROP_PROJECT_REFS);

            Log.WriteLine(
                "Matching glob pattern to files for project '{0}' as configuration '{1} - {2}' at source path '{3}'",
                id.Name, configuration.GroupName, configuration.Name, id.SourcePath);

            IncludeFiles = FileUtil.GetFiles(Solution.SolutionConfigDir,
                includeFilesProperty.Where(p => !p.Negated),
                includeFilesProperty.Where(p => p.Negated));
            
            // TODO: use each path in PROP_LIB_SEARCH_PATHS as base path for finding matching libraries.
            //  FileUtil.GetFiles(searchPath, libRefs.Where(p => !p.Negated), libRefs.Where(p => p.Negated)
            LibRefs = libRefsValues.Select(p => "NOT IMPLEMENTED").ToHashSet();
            ProjectRefs = projectRefsValues
                .ToHashSet();
        }
    }
}