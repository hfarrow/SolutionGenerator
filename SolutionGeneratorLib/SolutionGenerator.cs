using System;
using System.Data;
using System.IO;
using SolutionGenerator.Parsing;
using SolutionGenerator.Parsing.Model;
using Sprache;

namespace SolutionGenerator
{
    public class SolutionGenerator
    {
        private ConfigDocument solutionDoc;
        
        public void GenerateSolution(string solutionConfigPath)
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
                throw new DataException("Solution config could not be parsed because: " + result);
            }

            solutionDoc = result.Value;
        }
    }
}