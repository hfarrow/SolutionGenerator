using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolutionGen.Parser.Model;

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
            IEnumerable<string> matches =
                glob.FilterMatches(new DirectoryInfo(id.SourcePath));

            IncludeFiles = includeFiles
                .Concat(matches)
                .Except(excludeFiles)
                .ToHashSet();
            
            LibRefs = libRefsValues.Select(obj => obj.ToString()).ToHashSet();
            ProjectRefs = projectRefsValues
                .Select(obj => Template.ExpandModuleName(obj.ToString(), ModuleName))
                .ToHashSet();
        }

        private void ProcessFileValues(IEnumerable<object> filesValues, ISet<string> files, ISet<string> globs)
        {
            foreach (object includeFilesValue in filesValues)
            {
                switch (includeFilesValue)
                {
                    case GlobPath glob when includeFilesValue is GlobPath:
                        globs.Add(Template.ExpandModuleName(glob.Value, ModuleName));
                        break;
                    case LiteralPath file when includeFilesValue is LiteralPath:
                        files.Add(Template.ExpandModuleName(file.Value, ModuleName));
                        break;
                    default:
                        Console.WriteLine(
                            "Unrecognized include files value type will be skipped: " +
                            includeFilesValue.GetType().FullName);
                        break;
                }
            }
        }
    }
}