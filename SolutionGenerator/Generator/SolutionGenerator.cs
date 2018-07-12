using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
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

        public string ActiveConfigurationGroup { get; private set; }
        
        public static SolutionGenerator FromPath(string solutionConfigPath)
        {
            Log.WriteLine("Loading solution from path '{0}'", solutionConfigPath);
            
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
            Log.WriteLine("Loading solution from text with root path '{0}'", rootPath);
            
            var generator = new SolutionGenerator();
            generator.ParseSolutionConfig(configText, rootPath);
            return generator;
        }
        
        private void ParseSolutionConfig(string configText, string rootDir)
        {
            Log.WriteLine("Parsing main solution document");
            using (var _ = new Log.ScopedIndent(true))
            {
                IResult<ConfigDocument> result = DocumentParser.Document.TryParse(configText);
                if (!result.WasSuccessful)
                {
                    throw new DataException($"Solution config could not be parsed: {result}");
                }

                ConfigDoc = result.Value;
                Reader = new DocumentReader(ConfigDoc, rootDir);

                Log.WriteLine("Finished parsing solution named '{0}'", Reader.Solution.Name);
            }
        }

        public void GenerateSolution(string configurationGroup, params string[] externalDefineConstants)
        {
            
            Log.WriteLine("Generating solution '{0}' for configuration group '{1}'{2}",
                Reader.Solution.Name,
                configurationGroup,
                externalDefineConstants.Length > 0 ? " with external define constants:" : "");

            Log.WriteIndentedCollection(s => s, externalDefineConstants, true);

            using (var _ = new Log.ScopedIndent())
            {
                Log.WriteLine("Generation solution projects files");
                using (var __ = new Log.ScopedIndent(true))
                {
                    ActiveConfigurationGroup = configurationGroup;

                    Configuration currentConfiguration = Reader.Solution.Settings
                        .ConfigurationGroups[ActiveConfigurationGroup].Configurations
                        .First().Value;

                    foreach (Module module in Reader.Modules.Values)
                    {
                        Log.WriteLine("Generating module '{0}' with project count of {1}",
                            module.Name, module.ProjectIdLookup.Count);
                        using (var ___ = new Log.ScopedIndent())
                        {
                            foreach (Project.Identifier project in module.ProjectIdLookup.Values)
                            {

                                if (!module.Configurations[currentConfiguration].Projects.ContainsKey(project.Name))
                                {
                                    // Project was excluded for this configuration group.
                                    continue;
                                }

                                Log.WriteLine("Generating project '{0}' with GUID '{1}' at source path '{2}'",
                                    project.Name, project.Guid, project.SourcePath);

                                using (var ____ = new Log.ScopedIndent())
                                {
                                    var projectTemplate = new DotNetProject
                                    {
                                        Generator = this,
                                        Solution = Reader.Solution,
                                        Module = module,
                                        ProjectName = project.Name,
                                        ProjectIdLookup = Reader.Modules
                                            .SelectMany(kvp => kvp.Value.ProjectIdLookup)
                                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                                        CurrentConfiguration = currentConfiguration,
                                        ExternalDefineConstants = externalDefineConstants.ToHashSet(),
                                    };

                                    string projectText = projectTemplate.TransformText();
                                    string projectPath =
                                        Path.Combine(project.SourcePath, project.Name) + ".csproj";
                                    
                                    Log.WriteLine("Writing project to disk at path '{0}'", projectPath);
                                    File.WriteAllText(projectPath, projectText);
                                }
                            }
                        }
                    }
                }

                Log.WriteLine("Generating main solution file '{0}'.sln", Reader.Solution.Name);
                using (var __ = new Log.ScopedIndent(true))
                {
                    var solutionTemplate = new DotNetSolution
                    {
                        Generator = this,
                        Solution = Reader.Solution,
                        Modules = Reader.Modules,
                        ActiveConfigurationGroup =
                            Reader.Solution.Settings.ConfigurationGroups[ActiveConfigurationGroup],
                    };

                    string solutionText = solutionTemplate.TransformText();
                    string solutionPath = Path.Combine(Reader.SolutionConfigDir, Reader.Solution.Name) + ".sln";
                    Log.WriteLine("Writing solution to disk at path '{0}'", solutionPath);
                    File.WriteAllText(solutionPath, solutionText);
                }

                Log.WriteLine("Finished generating solution '{0}' for configuration group '{1}'",
                    Reader.Solution.Name,
                    configurationGroup);
            }
        }
    }
}