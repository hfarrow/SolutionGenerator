using System.Collections.Generic;
using System.Linq;
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
                string group = Generator.ActiveConfigurationGroup;
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
            Solution.ConfigurationGroups[Generator.ActiveConfigurationGroup].Configurations.Values.ToArray();

        private HashSet<string> commonIncludes;
        public HashSet<string> GetCommonIncludes()
        {
            if (commonIncludes == null)
            {
                List<IEnumerable<string>> collections =
                    ActiveConfigurations
                        .Select(c => Module.Configurations[c].Projects[Project.Name])
                        .Select(c => c.IncludeFiles.Select(f => f.Substring(Project.RelativeSourcePath.Length + 1)))
                        .ToList();

                commonIncludes = collections
                    .Skip(1)
                    .Aggregate(new HashSet<string>(collections.First()),
                        (h, e) =>
                        {
                            h.IntersectWith(e);
                            return h;
                        });
            }

            return commonIncludes;
        }

        public HashSet<string> GetConfigurationSpecificIncludes()
        {
            return Project.IncludeFiles
                .Select(f => f.Substring(Project.RelativeSourcePath.Length + 1))
                .Except(GetCommonIncludes())
                .ToHashSet();
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
            return Path.GetRelativePath(Project.AbsoluteSourcePath,
                Path.Combine(projectRef.SourcePath, projectRefName + ProjectNamePostfix + ".csproj"));
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
            return Path.GetRelativePath(Project.AbsoluteSourcePath, libPath);
        }
    }
}