using SolutionGenerator.Parsing.Model;

namespace SolutionGenerator.Model
{
    public class ConfigBuilder
    {
        private readonly ConfigDocument solutionDoc;

        public ConfigBuilder(ConfigDocument solutionDoc)
        {
            this.solutionDoc = solutionDoc;
            ProcessSolutionDoc();
        }

        private void ProcessSolutionDoc()
        {
            foreach (ObjectElement element in solutionDoc.RootElements)
            {
            }
        }
    }
}