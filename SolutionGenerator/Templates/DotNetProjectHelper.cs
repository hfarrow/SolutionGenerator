﻿using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;

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
        
        public Project Project => Module.Configurations[CurrentConfiguration].Projects[ProjectName];

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

        public string TargetFrameworkVersion =>
            GetStringProperty(Settings.PROP_TARGET_FRAMEWORK);

        public string LanguageVersion =>
            GetStringProperty(Settings.PROP_LANGUAGE_VERSION);

        public string GetStringProperty(string property) =>
            Project.Settings.GetProperty<string>(property);

        public HashSet<string> GetStringHashSetProperty(string property) =>
            Project.Settings.GetProperty<HashSet<string>>(property);

        public string GetDefineConstants() =>
            string.Join(';', GetStringHashSetProperty(Settings.PROP_DEFINE_CONSTANTS));

        public IEnumerable<string> TargetPlatforms => Solution.TargetPlatforms.Select(RemoveWhitespace);
        
        public string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());
        }

        public IReadOnlyCollection<Configuration> ActiveConfigurations => Solution.Settings
            .ConfigurationGroups[Generator.ActiveConfigurationGroup].Configurations.Values.ToArray();

        private HashSet<string> commonIncludes;
        public HashSet<string> GetCommonIncludes()
        {
            if (commonIncludes == null)
            {
                List<IReadOnlyCollection<string>> collections =
                    ActiveConfigurations
                        .Select(c => Module.Configurations[c].Projects[Project.Name])
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

        public HashSet<string> GetConfigurationSpecificIncludes()
        {
            return Project.IncludeFiles
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
    }
}