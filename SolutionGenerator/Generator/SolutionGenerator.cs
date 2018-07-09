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
using Sprache;

namespace SolutionGen
{
    public class SolutionGenerator
    {
        internal ConfigDocument ConfigDoc;
        internal DocumentReader Reader;

        public string ActiveConfigurationGroup { get; private set; }
        
        public static SolutionGenerator FromPath(string solutionConfigPath)
        {
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
            var generator = new SolutionGenerator();
            generator.ParseSolutionConfig(configText, rootPath);
            return generator;
        }
        
        private void ParseSolutionConfig(string configText, string rootPath)
        {
            IResult<ConfigDocument> result = DocumentParser.Document.TryParse(configText);
            if (!result.WasSuccessful)
            {
                throw new DataException($"Solution config could not be parsed: {result}");
            }

            ConfigDoc = result.Value;
            Reader = new DocumentReader(ConfigDoc, rootPath);
        }

        public void GenerateSolution(string configurationGroup, params string[] externalDefineConstants)
        {
            ActiveConfigurationGroup = configurationGroup;
            foreach (Module module in Reader.Modules.Values)
            {
                foreach (Project.Identifier project in module.ProjectIdLookup.Values)
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
                        CurrentConfiguration =
                            Reader.Solution.Settings.ConfigurationGroups[ActiveConfigurationGroup].Configurations
                                .First().Value
                    };

                    string projectText = projectTemplate.TransformText();
                    File.WriteAllText(Path.Combine(module.SourcePath, project.Name) + ".csproj", projectText);
                }
            }

            var solutionTemplate = new DotNetSolution
            {
                Generator = this,
                Solution = Reader.Solution,
                Modules = Reader.Modules
            };

            string solutionText = solutionTemplate.TransformText();
            File.WriteAllText(Path.Combine(Reader.SolutionConfigDirectory, Reader.Solution.Name) + ".sln",
                solutionText);
        }
    }
}