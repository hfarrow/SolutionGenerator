using SolutionGen.Generator.Model;
using SolutionGen.Generator.Reader;
using SolutionGen.Parser.Model;

namespace SolutionGen
{
    public class SolutionReader
    {
        private readonly ObjectElement solutionObject;
        public  Solution Solution { get; }

        public SolutionReader(ObjectElement solutionObject)
        {
            this.solutionObject = solutionObject;
            
            var settingsReader = new SettingsReader();
            Settings settings = settingsReader.Read(solutionObject);
            Solution = new Solution(solutionObject.Heading.Name, settings);
        }
    }
}