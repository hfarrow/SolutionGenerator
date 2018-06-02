using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using SolutionGen.Generator.Model;

namespace SolutionGen.Templates
{
    public partial class DotNetProject
    {
        public SolutionGenerator Generator { get; set; }
        public Solution Solution { get; set; }
        public Module Module { get; set; }
        public Project Project { get; set; }

        public string DefaultConfiguration
        {
            get
            {
                string group = Generator.ActiveConfigurationGroup;
                if (string.IsNullOrEmpty(group))
                {
                    group = Solution.Settings.ConfigurationGroups.First().Key;
                }

                return Solution.Settings.ConfigurationGroups[group].Configurations.First().Key;
            }
        }

        public string ProjectGuid => Project.Guid.ToString().ToUpper();

        public string DefaultPlatform =>
            RemoveWhitespace(Solution.TargetPlatforms.First());
        
        public string RootNamespace => Solution.RootNamespace;

        public string TargetFrameworkVersion => "v4.6";
//            Project.GetConfiguration(DefaultConfiguration).GetProperty<string>(Settings.PROP_TARGET_FRAMEWORK);

        public string LanguageVersion => "6";
//            Project.GetConfiguration(DefaultConfiguration).GetProperty<string>(Settings.PROP_LANGUAGE_VERSION);

        public string GetStringProperty(string configuration, string property) => string.Empty;
//            Project.GetConfiguration(configuration).GetProperty<string>(property);

        public HashSet<string> GetStringHashSetProperty(string configuration, string property) => new HashSet<string>();
//            Project.GetConfiguration(configuration).GetProperty<HashSet<object>>(property).Select(obj => obj.ToString())
//                .ToHashSet();

        public string GetDefineConstants(string configuration) => string.Empty;
//            string.Join(';', Project.GetConfiguration(configuration).DefineConstants);

        public IEnumerable<string> TargetPlatforms => Solution.TargetPlatforms.Select(RemoveWhitespace);
        
        public string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());
        }

        public IReadOnlyCollection<string> ActiveConfigurations => Solution.Settings
            .ConfigurationGroups[Generator.ActiveConfigurationGroup].Configurations.Keys.ToArray();

        private HashSet<string> commonIncludes;
        public HashSet<string> GetCommonIncludes() => new HashSet<string>();
//        {
//            if (commonIncludes == null)
//            {
//                List<IReadOnlyCollection<string>> collections =
//                    ActiveConfigurations
//                        .Select(Project.GetConfiguration)
//                        .Select(c => c.IncludeFiles)
//                        .ToList();
//
//                commonIncludes = collections
//                    .Skip(1)
//                    .Aggregate(new HashSet<string>(collections.First()),
//                        (h, e) =>
//                        {
//                            h.IntersectWith(e);
//                            return h;
//                        });
//            }
//
//            return commonIncludes;
//        }

        public HashSet<string> GetConfigurationSpecificIncludes(string configuration) => new HashSet<string>();
//        {
//            return Project.GetConfiguration(configuration)
//                .IncludeFiles.Except(GetCommonIncludes())
//                .ToHashSet();
//        }

        private HashSet<string> commonProjectRefs;
        public HashSet<string> GetCommonProjectRefs() => new HashSet<string>();
//        {
//            if (commonProjectRefs == null)
//            {
//                List<IReadOnlyCollection<string>> collections =
//                    Solution.ActiveConfigurations.Keys
//                        .Select(Project.GetConfiguration)
//                        .Select(c => c.ProjectRefs)
//                        .ToList();
//
//                commonProjectRefs = collections
//                    .Skip(1)
//                    .Aggregate(new HashSet<string>(collections.First()),
//                        (h, e) =>
//                        {
//                            h.IntersectWith(e);
//                            return h;
//                        });
//            }
//
//            return commonProjectRefs;
//        }

        public HashSet<string> GetConfigurationSpecificProjectRefs(string configuration) => new HashSet<string>();
//        {
//            return Project.GetConfiguration(configuration)
//                .ProjectRefs.Except(GetCommonProjectRefs())
//                .ToHashSet();
//        }
    }
}