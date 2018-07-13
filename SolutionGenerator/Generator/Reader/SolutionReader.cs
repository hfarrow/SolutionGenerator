using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Reader
{
    public class SolutionReader
    {
        private readonly ObjectElement solutionElement;
        public  Solution Solution { get; }

        public SolutionReader(ObjectElement solutionElement, string solutionConfigDir)
        {
            this.solutionElement = solutionElement;
            
            Log.WriteLine("Reading solution element: {0}", solutionElement);
            using (new Log.ScopedIndent(true))
            {
                ExpandableVar.SetExpandableVariable(ExpandableVar.VAR_SOLUTION_NAME, solutionElement.Heading.Name);
                var settingsReader = new SettingsReader(ExpandableVar.ExpandableVariables);
                Settings settings = settingsReader.Read(solutionElement);
                Solution = new Solution(solutionElement.Heading.Name, settings, solutionConfigDir);
            }
        }
    }
}