using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Reader
{
    public class SolutionReader
    {
        private const string DEFAULT_TEMPLATE_SETTINGS = "template.defaults";
        
        private readonly ObjectElement solutionElement;
        public Solution Solution { get; }
        public Settings TemplateDefaultSettings { get; }

        public SolutionReader(ObjectElement solutionElement, string solutionConfigDir)
        {
            this.solutionElement = solutionElement;
            
            Log.WriteLine("Reading solution element: {0}", solutionElement);
            using (new Log.ScopedIndent(true))
            {
                ExpandableVar.SetExpandableVariable(ExpandableVar.VAR_SOLUTION_NAME,
                    solutionElement.ElementHeading.Name);
                
                var settingsReader = new SolutionSettingsReader(ExpandableVar.ExpandableVariables);
                Settings settings = settingsReader.Read(solutionElement);
                Solution = new Solution(solutionElement.ElementHeading.Name, settings, solutionConfigDir);

                ObjectElement settingsElement = solutionElement.Elements
                    .OfType<ObjectElement>()
                    .FirstOrDefault(obj => obj.ElementHeading.Type == SectionType.SETTINGS &&
                        obj.ElementHeading.Name == DEFAULT_TEMPLATE_SETTINGS);

                if (settingsElement == null)
                {
                    Log.WriteLine(
                        "No settings named '{0}' found in solution element. " +
                        "Templates will use hard coded defaults instead.",
                        DEFAULT_TEMPLATE_SETTINGS);
                    
                    TemplateDefaultSettings = settingsReader.GetDefaultSettings();
                }
                else
                {
                    Log.WriteLine(
                        "Settings named '{0}' found in solution element. " +
                        "Templates will default to the settings read below.",
                        DEFAULT_TEMPLATE_SETTINGS);

                    using (new Log.ScopedIndent())
                    {
                        TemplateDefaultSettings =
                            new ProjectSettingsReader(ExpandableVar.ExpandableVariables)
                                .Read(settingsElement);
                    }
                }
            }
        }
    }
}