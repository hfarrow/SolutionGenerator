using System.Collections.Generic;
using System.Linq;
using SolutionGen.Compiling.Model;

namespace SolutionGen.Templates
{
    public partial class DotNetProject
    {
        public Solution Solution { get; set; }
        public Module Module { get; set; }
        public Project Project { get; set; }

        public string DefaultConfiguration
        {
            get
            {
                string group = Solution.ActiveConfigurationGroup;
                if (string.IsNullOrEmpty(group))
                {
                    group = Solution.ConfigurationGroups.First().Key;
                }

                return Solution.ConfigurationGroups[group].Configurations.First().Key;
            }
        }

        public string ProjectGuid => Project.Guid.ToString().ToUpper();
        public string DefaultPlatform => RemoveWhitespace(Solution.TargetPlatforms.First());
        public string RootNamespace => Solution.Settings.GetProperty<string>(Settings.PROP_ROOT_NAMESPACE);

        public string TargetFrameworkVersion =>
            Project.GetConfiguration(DefaultConfiguration).GetProperty<string>(Settings.PROP_TARGET_FRAMEWORK);

        public string LanguageVersion =>
            Project.GetConfiguration(DefaultConfiguration).GetProperty<string>(Settings.PROP_LANGUAGE_VERSION);

        public string GetStringProperty(string configuration, string property) =>
            Project.GetConfiguration(configuration).GetProperty<string>(property);

        public HashSet<string> GetStringHashSetProperty(string configuration, string property) =>
            Project.GetConfiguration(configuration).GetProperty<HashSet<object>>(property).Select(obj => obj.ToString())
                .ToHashSet();

        public string GetDefineConstants(string configuration) =>
            string.Join(';', Project.GetConfiguration(configuration).DefineConstants);

        public IEnumerable<string> TargetPlatforms => Solution.TargetPlatforms.Select(RemoveWhitespace);
        
        public string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());
        }

        private HashSet<string> commonIncludes;
        public HashSet<string> GetCommonIncludes()
        {
            if (commonIncludes == null)
            {
                List<IReadOnlyCollection<string>> collections =
                    Solution.ActiveConfigurations.Keys
                        .Select(Project.GetConfiguration)
                        .Select(c => c.IncludeFiles)
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

        public HashSet<string> GetConfigurationSpecificIncludes(string configuration)
        {
            return Project.GetConfiguration(configuration)
                .IncludeFiles.Except(GetCommonIncludes())
                .ToHashSet();
        }

        private HashSet<string> commonProjectRefs;
        public HashSet<string> GetCommonProjectRefs()
        {
            if (commonProjectRefs == null)
            {
                List<IReadOnlyCollection<string>> collections =
                    Solution.ActiveConfigurations.Keys
                        .Select(Project.GetConfiguration)
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

        public HashSet<string> GetConfigurationSpecificProjectRefs(string configuration)
        {
            return Project.GetConfiguration(configuration)
                .ProjectRefs.Except(GetCommonProjectRefs())
                .ToHashSet();
        }
    }
}