using System;
using System.Data;
using System.IO;
using SolutionGenerator.Compiling.Model;
using SolutionGenerator.Parsing;
using SolutionGenerator.Parsing.Model;
using Sprache;

namespace SolutionGenerator
{
    public class SolutionGenerator
    {
        private ConfigDocument configDoc;
        private ConfigReader reader;
        
        public SolutionGenerator(string solutionConfigPath)
        {
            LoadSolutionConfig(solutionConfigPath);
        }

        public void GenerateSolution(string[] externalDefineConstants)
        {
            foreach (Template template in reader.Templates.Values)
            {
                template.Compile(externalDefineConstants);
            }
            
            foreach (Module module in reader.Modules.Values)
            {
                string templateName = module.ModuleElement.Heading.InheritedObjectName;
                if (!reader.Templates.TryGetValue(templateName, out Template template))
                {
                    throw new UndefinedTemplateException(templateName);
                }

                template.Apply(module);
            }
        }
        
        private void LoadSolutionConfig(string solutionConfigPath)
        {
            string solutionConfigStr;
            try
            {
                solutionConfigStr = File.ReadAllText(solutionConfigPath);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Solution config could not be loaded from path '{solutionConfigPath}'",
                    nameof(solutionConfigPath), ex);
            }

            IResult<ConfigDocument> result = DocumentParser.Document.TryParse(solutionConfigStr);
            if (!result.WasSuccessful)
            {
                throw new DataException($"Solution config could not be parsed: {result}");
            }

            configDoc = result.Value;
            reader = new ConfigReader(configDoc);
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