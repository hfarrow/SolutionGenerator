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
//            System.IO.Path.GetRelativePath(Solution.SolutionConfigDir, AbsoluteSourcePath);
        
        public IReadOnlyCollection<string> IncludeFiles { get; private set; }
        public IReadOnlyCollection<string> LibRefs { get; private set; }
        public IReadOnlyCollection<string> ProjectRefs { get; private set; }

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
            var includeFilesValues = Settings.GetProperty<HashSet<IPath>>(Settings.PROP_INCLUDE_FILES);
            var excludeFilesValues = Settings.GetProperty<HashSet<IPath>>(Settings.PROP_EXCLUDE_FILES);
            var libRefsValues = Settings.GetProperty<HashSet<string>>(Settings.PROP_LIB_REFS);
            var projectRefsValues = Settings.GetProperty<HashSet<string>>(Settings.PROP_PROJECT_REFS);
            var includePatterns = new HashSet<string>();
            var excludePatterns = new HashSet<string>();
            var includeFiles = new HashSet<string>();
            var excludeFiles = new HashSet<string>();
            
            ProcessFileValues(includeFilesValues, includeFiles, includePatterns);
            ProcessFileValues(excludeFilesValues, excludeFiles, excludePatterns);

            var glob = new Utils.Glob(includePatterns, excludePatterns);
            // TODO: cache all files under RootPath instead of using DirectoryInfo
            string[] matches =
                glob.FilterMatches(new DirectoryInfo(Solution.SolutionConfigDir)).ToArray();

            Log.WriteLine(
                "Matching glob pattern to files for project '{0}' as configuration '{1} - {2}' at source path '{3}'",
                id.Name, configuration.GroupName, configuration.Name, id.SourcePath);
            
            using (var _ = new Log.ScopedIndent())
            {
                Log.WriteLine("include patterns:");
                Log.WriteIndentedCollection(s => s, includePatterns);
                Log.WriteLine("exclude patterns:");
                Log.WriteIndentedCollection(s => s, excludePatterns);
                Log.WriteLine("matched files:");
                Log.WriteIndentedCollection(s => s, matches);
            }

            IncludeFiles = includeFiles
                .Concat(matches)
                .Except(excludeFiles)
                .ToHashSet();
            
            LibRefs = libRefsValues.Select(obj => obj.ToString()).ToHashSet();
            ProjectRefs = projectRefsValues
                .Select(str => ExpandableVar.ExpandModuleNameInCopy(str, ModuleName).ToString())
                .ToHashSet();
        }

        private void ProcessFileValues(IEnumerable<object> filesValues, ISet<string> files, ISet<string> globs)
        {
            foreach (object includeFilesValue in filesValues)
            {
                object includeFilesValueCopy = ExpandableVar.ExpandModuleNameInCopy(includeFilesValue, ModuleName);
                
                switch (includeFilesValueCopy)
                {
                    case GlobPath glob when includeFilesValueCopy is GlobPath:
                        globs.Add(glob.Value);
                        break;
                    case LiteralPath file when includeFilesValueCopy is LiteralPath:
                        files.Add(file.Value);
                        break;
                    default:
                        Log.WriteLineWarning(
                            "Unrecognized include files value type will be skipped: " +
                            includeFilesValue.GetType().FullName);
                        break;
                }
            }
        }
    }
}