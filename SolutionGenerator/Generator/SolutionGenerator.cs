using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
        internal ConfigDocument configDoc;
        internal DocumentReader reader;

        public string ActiveConfigurationGroup { get; }
        
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

            configDoc = result.Value;
            reader = new DocumentReader(configDoc, rootPath);
        }

        public void GenerateSolution(string configurationGroup, params string[] externalDefineConstants)
        {
//            reader.Solution.ActiveConfigurationGroup = configurationGroup;
//            
//            foreach (Template template in reader.Templates.Values)
//            {
//                template.Compile(reader.Solution, externalDefineConstants);
//            }
//            
//            foreach (Module module in reader.Modules.Values)
//            {
//                reader.Solution.AddModule(module);
//                string templateName = module.ModuleElement.Heading.InheritedObjectName;
//                if (!reader.Templates.TryGetValue(templateName, out Template template))
//                {
//                    throw new UndefinedTemplateException(templateName);
//                }
//
//                template.ApplyToModule(configurationGroup, reader.Solution, module, externalDefineConstants);
//                foreach (Project project in module.Projects)
//                {
//                    var projectTemplate = new DotNetProject
//                    {
//                        Solution = reader.Solution,
//                        Module = module,
//                        Project = project
//                    };
//
//                    File.WriteAllText(Path.Combine(module.RootPath, project.Name) + ".csproj", projectTemplate.TransformText());
//                }
//            }
//
//            var solutionTemplate = new DotNetSolution()
//            {
//                Solution = reader.Solution
//            };
//            
//            File.WriteAllText(Path.Combine(reader.RootPath, reader.Solution.Name) + ".sln", solutionTemplate.TransformText());
        }
    }

    public sealed class UndefinedTemplateException : Exception
    {
        public UndefinedTemplateException(string templateName)
            : base($"A template named '{templateName}' could not be found. Was it included by the solution config?")
        {
            
        }
    }
}