using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolutionGen.Parsing.Model;

namespace SolutionGen.Compiling.Model
{
    public class Project
    {
        public class Configuration
        {
            public string Name { get; }
            public bool ExcludedFromGeneration;

            private readonly HashSet<string> defineConstants;
            
            private readonly Dictionary<string, object> properties = new Dictionary<string, object>();
            public T GetProperty<T>(string name) => (T) properties[name];
            public void SetProperty<T>(string name, T value) => properties[name] = value;

            public bool HasPropertyWithValue<T>(string name, T expectedValue)
            {
                return properties.TryGetValue(name, out object obj) && Equals(obj, expectedValue);
            }
            
            public IReadOnlyCollection<string> DefineConstants => defineConstants;
            public IReadOnlyCollection<string> IncludeFiles { get; private set; }
            public IReadOnlyCollection<string> LibRefs { get; private set; }
            public IReadOnlyCollection<string> ProjectRefs { get; private set; }

            public Configuration(string name, HashSet<string> defineConstants)
            {
                Name = name;
                this.defineConstants = defineConstants;
            }
            
            public void InitFromProperties(Project project)
            {
                if (HasPropertyWithValue<bool>(Settings.PROP_EXCLUDE, true))
                {
                    ExcludedFromGeneration = true;
                    return;
                }
                
                // Include files, exclude files, lib refs
                var includeFilesValues = GetProperty<HashSet<object>>(Settings.PROP_INCLUDE_FILES);
                var excludeFilesValues = GetProperty<HashSet<object>>(Settings.PROP_EXCLUDE_FILES);
                var libRefsValues = GetProperty<HashSet<object>>(Settings.PROP_LIB_REFS);
                var projectRefsValues = GetProperty<HashSet<object>>(Settings.PROP_PROJECT_REFS);

                var includePatterns = new HashSet<string>();
                var excludePatterns = new HashSet<string>();
                var includeFiles = new HashSet<string>();
                var excludeFiles = new HashSet<string>();
                
                ProcessFileValues(includeFilesValues, includeFiles, includePatterns, project);
                ProcessFileValues(excludeFilesValues, excludeFiles, excludePatterns, project);

                var glob = new Utils.Glob(includePatterns, excludePatterns);
                // TODO: cache all files under RootPath instead of using DirectoryInfo
                IncludeFiles = includeFiles.Concat(glob.FilterMatches(new DirectoryInfo(project.Module.RootPath)))
                    .Except(excludeFiles)
                    .ToHashSet();
                
                LibRefs = libRefsValues.Select(obj => obj.ToString()).ToHashSet();
                ProjectRefs = projectRefsValues.Select(obj => Template.ExpandModuleName(obj.ToString(), project.Module.Name)).ToHashSet();
            }

            private static void ProcessFileValues(IEnumerable<object> filesValues, ISet<string> files, ISet<string> globs,
                Project project)
            {
                foreach (object includeFilesValue in filesValues)
                {
                    switch (includeFilesValue)
                    {
                        case GlobValue glob when includeFilesValue is GlobValue:
                            globs.Add(Template.ExpandModuleName(glob.GlobStr, project.Module.Name));
                            break;
                        case string file when includeFilesValue is string:
                            files.Add(file);
                            break;
                        default:
                            Console.WriteLine(
                                $"Unrecognized include files value type will be skipped: {includeFilesValue.GetType().FullName}");
                            break;
                    }
                }
            }

            public override string ToString()
            {
                return $"Project.Configuration{{{Name}}}";
            }

        }
        
        public string Name { get; }
        public Guid Guid { get; }
        public Module Module { get; }
        public bool HasConfiguration(string name) => configurations.ContainsKey(name);
        public Configuration GetConfiguration(string name) => configurations[name];
        public void SetConfiguration(string name, Configuration configuration) => configurations[name] = configuration;
        public void ClearConfigurations() => configurations.Clear();
        public bool ExcludedFromGeneration => configurations.Values.All(c => c.ExcludedFromGeneration);
        
        private readonly Dictionary<string, Configuration> configurations = new Dictionary<string, Configuration>();

        public Project(string name, Module module)
        {
            Name = name;
            Module = module;
            Guid = Guid.NewGuid();
        }
    }
}