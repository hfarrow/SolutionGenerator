using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using SolutionGen.Builder;
using SolutionGen.Generator.Model;
using SolutionGen.Generator.Reader;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using SolutionGen.Templates;
using SolutionGen.Utils;
using Sprache;
using Path = System.IO.Path;

namespace SolutionGen
{
    public class SolutionGenerator
    {
        internal ConfigDocument ConfigDoc;
        internal DocumentReader Reader;

        public string MasterConfiguration { get; private set; }
        public Solution Solution { get; private set; }

        private Dictionary<string, Project.Identifier> projectIdLookup;
        
        public static SolutionGenerator FromPath(string solutionConfigPath)
        {
            Log.Info("Loading solution from path '{0}'", solutionConfigPath);
            
            string configText;
            try
            {
                configText = File.ReadAllText(solutionConfigPath);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Solution config could not be loaded from path '{solutionConfigPath}'",
                    nameof(solutionConfigPath), ex);
            }
            
            var generator = new SolutionGenerator();
            generator.ParseSolutionConfig(configText, Path.GetDirectoryName(solutionConfigPath));
            return generator;
        }

        public static SolutionGenerator FromText(string configText, string rootPath)
        {
            Log.Info("Loading solution from text with root path '{0}'", rootPath);
            
            var generator = new SolutionGenerator();
            generator.ParseSolutionConfig(configText, rootPath);
            return generator;
        }
        
        private void ParseSolutionConfig(string configText, string rootDir)
        {
            Log.Heading("Parsing main solution document");
            using (new Disposable(
                new Log.ScopedIndent(),
                new Log.ScopedTimer(Log.Level.Info, "Parse Solution Config")))
            {
                IResult<ConfigDocument> result = DocumentParser.Document.TryParse(configText);
                if (!result.WasSuccessful)
                {
                    throw new DocumentParseException("<in-memory>", $"Solution config could not be parsed: {result}");
                }

                ConfigDoc = result.Value;
                Reader = new DocumentReader(ConfigDoc, rootDir);
                Reader.ParseSolution();
                Solution = Reader.Solution;

                Log.Info("Finished parsing solution named '{0}'", Reader.Solution.Name);
            }
        }

        public void GenerateSolution(string masterConfiguration)
            => GenerateSolution(masterConfiguration, null, null);

        public void GenerateSolution(string masterConfiguration,
            string[] externalDefineConstants,
            PropertyElement[] propertyOverrides)
        {
            Reader.ReadFullSolution(propertyOverrides);
            projectIdLookup = Reader.Modules
                .SelectMany(kvp => kvp.Value.ProjectIdLookup)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            externalDefineConstants = externalDefineConstants ?? new string[0];

            Log.Heading("Generating solution '{0}' for master configuration '{1}'",
                Reader.Solution.Name,
                masterConfiguration);

            if (externalDefineConstants.Length > 0)
            {
                Log.Info("with external define constans:");
                Log.IndentedCollection(externalDefineConstants, Log.Info);
            }

            using (new Disposable(
                new Log.ScopedIndent(),
                new Log.ScopedTimer(Log.Level.Info, "Generate Solution")))
            {
                if (string.IsNullOrEmpty(masterConfiguration))
                {
                    masterConfiguration = Reader.Solution.ConfigurationGroups.Keys.First();
                    Log.Info("No master configuration was provided. Using default '{0}'.", masterConfiguration);
                }

                HashSet<string> includedProjects = Reader.Solution.IncludedProjects;
                HashSet<string> generatableProjects = includedProjects
                    .Where(p => Reader.Solution.CanGenerateProject(p))
                    .ToHashSet();

                bool generateAll = includedProjects.SetEquals(generatableProjects);

                GenerateSolutionFiles("", Reader.Modules.Values, masterConfiguration, includedProjects,
                    externalDefineConstants);

                if (Reader.Solution.GeneratedProjectsPatterns.Count > 0 && !generateAll)
                {
                    var builder = new SolutionBuilder(Reader.Solution, masterConfiguration);
                    builder.BuildAllConfigurations();

                    // Generate new solution with references to those DLLs.
                    IEnumerable<Module> updatedModules = ReplacePrebuiltReferences(generatableProjects);
                    GenerateSolutionFiles("-small", updatedModules, masterConfiguration, generatableProjects,
                        externalDefineConstants);
                }
            }
        }

        private void GenerateSolutionFiles(string namePostfix, IEnumerable<Module> modules, string configurationGroup,
            HashSet<string> projectWhitelist, params string[] externalDefineConstants)
        {
            Log.Heading("Generating solution projects files");
            using (new Log.ScopedIndent())
            {
                MasterConfiguration = configurationGroup;
                Solution solution = Reader.Solution;

                Configuration currentConfiguration = solution
                    .ConfigurationGroups[MasterConfiguration].Configurations
                    .First().Value;

                Directory.CreateDirectory(Reader.Solution.OutputDir);

                foreach (Module module in modules)
                {
                    Log.Info("Generating module '{0}' with project count of {1}",
                        module.Name, module.ProjectIdLookup.Count);
                    using (new Disposable(
                        new Log.ScopedIndent(),
                        new ExpandableVar.ScopedState()))
                    {
                        ExpandableVar.SetExpandableVariable(ExpandableVar.VAR_MODULE_NAME, module.Name);
                        foreach (Project.Identifier project in module.ProjectIdLookup.Values)
                        {
                            if (!module.Configurations[currentConfiguration].Projects.ContainsKey(project.Name))
                            {
                                // Project was excluded for this configuration group.
                                Log.Info(
                                    "Project '{0}' for configuration '{1} - {2}' is excluded from generation",
                                    project.Name,
                                    currentConfiguration.GroupName,
                                    currentConfiguration.Name);
                                continue;
                            }

                            Log.Heading("Generating project '{0}' with GUID '{1}' at source path '{2}'",
                                project.Name, project.Guid, project.AbsoluteSourcePath);

                            using (new Log.ScopedIndent())
                            {
                                ExpandableVar.SetExpandableVariable(ExpandableVar.VAR_PROJECT_NAME,
                                    project.Name);

                                var projectTemplate = new DotNetProject
                                {
                                    Generator = this,
                                    Solution = Reader.Solution,
                                    Module = module,
                                    ProjectName = project.Name,
                                    ProjectIdLookup = projectIdLookup,
                                    CurrentConfiguration = currentConfiguration,
                                    ExternalDefineConstants = externalDefineConstants.ToHashSet(),
                                    ProjectNamePostfix = namePostfix,
                                };

                                string projectText = projectTemplate.TransformText();
                                string projectPath =
                                    Path.Combine(
                                        Reader.Solution.OutputDir,
                                        project.AbsoluteSourcePath, 
                                        project.Name + namePostfix) + ".csproj";

                                Log.Info("Writing project to disk at path '{0}'", projectPath);
                                Directory.CreateDirectory(Path.GetDirectoryName(projectPath));
                                File.WriteAllText(projectPath, projectText);
                            }
                        }
                    }
                }
            }

            Log.Heading("Generating main solution file '{0}'.sln", Reader.Solution.Name);
            using (new Log.ScopedIndent())
            {
                Log.Debug("Project Whitelist:");
                Log.IndentedCollection(projectWhitelist, Log.Debug);
                
                var solutionTemplate = new DotNetSolution
                {
                    Generator = this,
                    Solution = Reader.Solution,
                    Modules = Reader.Modules,
                    ActiveConfigurationGroup =
                        Reader.Solution.ConfigurationGroups[MasterConfiguration],
                    ProjectNamePostfix = namePostfix,
                    ProjectWhitelist = projectWhitelist,
                };

                string solutionText = solutionTemplate.TransformText();
                string solutionPath =
                    Path.Combine(
                        Reader.SolutionConfigDir,
                        Reader.Solution.OutputDir,
                        Reader.Solution.Name + namePostfix) + ".sln";
                
                Log.Info("Writing solution to disk at path '{0}'", solutionPath);
                File.WriteAllText(solutionPath, solutionText);
            }

            Log.Info("Finished generating solution '{0}' for configuration group '{1}'",
                Reader.Solution.Name,
                configurationGroup);
        }

        private IEnumerable<Module> ReplacePrebuiltReferences(ICollection<string> generatableProjects)
        {
            Log.Heading("Replacing project references with prebuilt assemblies:");
            
            string[] projectsToReplace = Reader.Solution.IncludedProjects
                .Except(generatableProjects)
                .ToArray();
            
            Log.IndentedCollection(projectsToReplace, Log.Info);

            var results = new List<Module>();
            
            using (new Log.ScopedIndent())
            {
                foreach (Module module in Reader.Modules.Values)
                {
                    var configurations = new Dictionary<Configuration, ModuleConfiguration>();
                    foreach (KeyValuePair<Configuration, ModuleConfiguration> kvp in module.Configurations)
                    {
                        var newProjects = new List<Project>();
                        foreach (Project project in
                            kvp.Value.Projects.Values.Where(p => generatableProjects.Contains(p.Name)))
                        {
                            string[] libRefs = project.ProjectRefs
                                .Intersect(projectsToReplace)
                                .Select(p => Path.Combine(GetRelativePathToProject(p), "bin", "Debug", p + ".dll"))
                                .ToArray();
                            
                            string[] projectRefs = project.ProjectRefs
                                .Except(projectsToReplace)
                                .ToArray();

                            Settings newSettings = project.Settings.Clone();
                            var libRefsValues = newSettings.GetProperty<HashSet<IPattern>>(Settings.PROP_LIB_REFS);
                            var projectRefsValues =
                                newSettings.GetProperty<HashSet<string>>(Settings.PROP_PROJECT_REFS);

                            projectRefsValues.Clear();
                            foreach (string p in projectRefs)
                            {
                                projectRefsValues.Add(p);
                            }

                            foreach (string libRef in libRefs)
                            {
                                libRefsValues.Add(new LiteralPattern(libRef, false));
                            }

                            if (libRefs.Length > 0)
                            {
                                Log.Info("Project references in project '{0}' will be replaced with lib references:",
                                    project.Name);
                                Log.IndentedCollection(libRefs, Log.Info);
                                
                                // Only create a new project instance if the reference actually changed.
                                newProjects.Add(
                                    new Project(Reader.Solution, module.Name, project.Id, kvp.Key, newSettings));
                            }
                            else
                            {
                                Log.Info("Skipping project '{0}' because there are no references to replace",
                                    project.Name);
                                
                                newProjects.Add(project);
                            }
                        }

                        configurations[kvp.Key] =
                            new ModuleConfiguration(newProjects.ToDictionary(p => p.Name, p => p));
                    }

                    results.Add(new Module(Reader.Solution, module.Name, configurations, module.ProjectIdLookup));
                }
            }

            return results;
        }
        
        private string GetRelativePathToProject(string toProject)
        {
            Project.Identifier toId = projectIdLookup[toProject];
            return Path.GetRelativePath(Reader.SolutionConfigDir, toId.AbsoluteSourcePath);
        }
    }

    public sealed class DocumentParseException : Exception
    {
        public DocumentParseException(string filePath, string reason)
            : base(string.Format("Failed to parse document at path '{0}': {1}", filePath, reason))
        {
            
        }
    }
}