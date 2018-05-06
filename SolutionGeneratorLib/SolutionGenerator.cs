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
        internal ConfigDocument configDoc;
        internal ConfigReader reader;
        
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
            generator.ParseSolutionConfig(configText);
            return generator;
        }

        public static SolutionGenerator FromText(string configText)
        {
            var generator = new SolutionGenerator();
            generator.ParseSolutionConfig(configText);
            return generator;
        }

        public void GenerateSolution(string configurationGroup, params string[] externalDefineConstants)
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

                template.ApplyTo(configurationGroup, module, externalDefineConstants);
            }
        }
        
        private void ParseSolutionConfig(string configText)
        {
            IResult<ConfigDocument> result = DocumentParser.Document.TryParse(configText);
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