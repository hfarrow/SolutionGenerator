using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using SolutionGen.Generator.Model;
using Path = System.IO.Path;

namespace SolutionGen.Templates
{
    public partial class DotNetProject
    {
        public SolutionGenerator Generator { get; set; }
        public Solution Solution { get; set; }
        public Module Module { get; set; }
        public string ProjectName { get; set; }
        public Configuration CurrentConfiguration { get; set; }
        public Dictionary<string, Project.Identifier> ProjectIdLookup { get; set; }
        public HashSet<string> ExternalDefineConstants { get; set; }
        public string ProjectNamePostfix { get; set; }
        
        public Project Project => Module.Configurations[CurrentConfiguration].Projects[ProjectName];

        public string DefaultConfiguration
        {
            get
            {
                string group = Generator.MasterConfiguration;
                if (string.IsNullOrEmpty(group))
                {
                    group = Solution.ConfigurationGroups.First().Key;
                }

                return Solution.ConfigurationGroups[group].Configurations.First().Key;
            }
        }

        public string ProjectGuid => Project.Guid.ToString().ToUpper();

        public string DefaultPlatform =>
            RemoveWhitespace(Solution.TargetPlatforms.First());

        public string RootNamespace =>
            GetStringProperty(Settings.PROP_ROOT_NAMESPACE);

        public string TargetFrameworkVersion =>
            GetStringProperty(Settings.PROP_TARGET_FRAMEWORK);

        public string LanguageVersion =>
            GetStringProperty(Settings.PROP_LANGUAGE_VERSION);

        public string GetStringProperty(string property) =>
            Project.Settings.GetProperty<string>(property);

        public List<string> GetListProperty(string property) =>
            Project.Settings.GetProperty<List<string>>(property);
        
        public HashSet<string> GetStringHashSetProperty(string property) =>
            Project.Settings.GetProperty<HashSet<string>>(property);

        public string GetDefineConstants() =>
            string.Join(';', GetStringHashSetProperty(Settings.PROP_DEFINE_CONSTANTS).Concat(ExternalDefineConstants));

        public IEnumerable<string> TargetPlatforms => Solution.TargetPlatforms.Select(RemoveWhitespace);
        
        public string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());
        }

        public IReadOnlyCollection<Configuration> ActiveConfigurations => 
            Solution.ConfigurationGroups[Generator.MasterConfiguration].Configurations.Values.ToArray();

        private HashSet<(string, string)> commonIncludes;
        public HashSet<(string, string)> GetCommonIncludes()
        {
            if (commonIncludes == null)
            {
                string basePath = GetRelativeSourcePath();
                
                List<IEnumerable<(string, string)>> collections =
                    ActiveConfigurations
                        .Select(c => Module.Configurations[c].Projects[Project.Name])
                        .Select(c => c.IncludeFiles.Select(f => (basePath != "." ? Path.Combine(basePath, f) : f, f)))
                        .ToList();

                commonIncludes = collections
                    .Skip(1)
                    .Aggregate(new HashSet<(string, string)>(collections.First()),
                        (h, e) =>
                        {
                            h.IntersectWith(e);
                            return h;
                        });
            }

            return commonIncludes;
        }

        public HashSet<(string, string)> GetConfigurationSpecificIncludes()
        {
            string basePath = GetRelativeSourcePath();
            return Project.IncludeFiles
                .Select(f => (basePath != "." ? Path.Combine(basePath, f): f, f))
                .Except(GetCommonIncludes())
                .ToHashSet();
        }

        public string GetRelativeSourcePath()
        {
            string fromPath = Path.Combine(Solution.OutputDir, Project.RelativeSourcePath);
            return Path.GetRelativePath(fromPath,
                Path.Combine(Solution.SolutionConfigDir, Project.RelativeSourcePath));
        }

        private HashSet<string> commonProjectRefs;
        public HashSet<string> GetCommonProjectRefs()
        {
            if (commonProjectRefs == null)
            {
                List<IReadOnlyCollection<string>> collections =
                    ActiveConfigurations
                        .Select(c => Module.Configurations[c].Projects[ProjectName])
                        .Select(c => c.ProjectRefs)
                        .ToList();

                commonProjectRefs = collections
                    .Skip(1)
                    .Aggregate(new HashSet<string>(collections.First()),
                        (h, e) =>
                        {
                            h.IntersectWith(e);
                            return h;
                        });
            }

            return commonProjectRefs;
        }

        public HashSet<string> GetConfigurationSpecificProjectRefs()
        {
            return Project.ProjectRefs
                .Except(GetCommonProjectRefs())
                .ToHashSet();
        }

        public string GetRelativeProjectRefPath(string projectRefName)
        {
            Project.Identifier projectRef = ProjectIdLookup[projectRefName];
            return Path.GetRelativePath(Path.Combine(Solution.OutputDir, Project.RelativeSourcePath),
                Path.Combine(Solution.OutputDir, projectRef.RelativeSourcePath, projectRefName + ProjectNamePostfix + ".csproj"));
        }
        
        private HashSet<string> commonLibRefs;
        public HashSet<string> GetCommonLibRefs()
        {
            if (commonLibRefs == null)
            {
                List<IReadOnlyCollection<string>> collections =
                    ActiveConfigurations
                        .Select(c => Module.Configurations[c].Projects[Project.Name])
                        .Select(c => c.LibRefs)
                        .ToList();

                commonLibRefs = collections
                    .Skip(1)
                    .Aggregate(new HashSet<string>(collections.First()),
                        (h, e) =>
                        {
                            h.IntersectWith(e);
                            return h;
                        });
            }

            return commonLibRefs;
        }

        public HashSet<string> GetConfigurationSpecificLibRefs()
        {
            return Project.LibRefs
                .Except(GetCommonLibRefs())
                .ToHashSet();
        }
        
        public string GetRelativeLibRefPath(string libPath)
        {
            string libRoot = Directory.GetDirectoryRoot(libPath);
            string outputRoot = Directory.GetDirectoryRoot(Solution.OutputDir);
            bool useAbsolutePath = true;
            
            if (libRoot == outputRoot)
            {
                string firstLibFolder = libPath.Substring(libRoot.Length)
                    .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .First();
                
                string firstOutputFolder = Solution.OutputDir.Substring(outputRoot.Length)
                    .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .First();
                
                if(firstLibFolder == firstOutputFolder)
                {
                    useAbsolutePath = false;
                }
            }

            return useAbsolutePath
                ? libPath
                : Path.GetRelativePath(Path.Combine(Solution.OutputDir, Project.RelativeSourcePath), libPath);
        }
        
        public IReadOnlyCollection<string> GetCommonCustomContents()
        {
            return Project.CustomContents;
        }

        public string FormatCustomContents(IReadOnlyCollection<string> customContents, int indentSize)
        {
            string indent = "".PadRight(indentSize);
            return string.Join("\n", customContents)
                .Replace("\n", "\n" + indent)
                .Insert(0, indent);
        }

        public void ValidateNoConfigurationSpecificCustomContents()
        {
            IReadOnlyCollection<string> prev = null;
            foreach (Configuration configuration in ActiveConfigurations)
            {
                CurrentConfiguration = configuration;
                IReadOnlyCollection<string> current = GetCommonCustomContents();
                if (prev != null)
                {
                    if (prev.Count != current.Count ||
                        !current.SequenceEqual(prev))
                    {
                        throw new NotSupportedException(
                            string.Format(
                                "Configuration specific '{0}' are not supported. Only common contents are allowed. " +
                                "Consider hard coding msbuild conditionals directly into custom contents",
                                Settings.PROP_CUSTOM_CSPROJ_CONTENTS));
                    }
                }

                prev = current;
            }
        }
    }
}