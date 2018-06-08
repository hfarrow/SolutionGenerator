using System.Collections.Generic;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Reader
{
    public class TemplateReader
    {
        private readonly IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups;

        public TemplateReader(IReadOnlyDictionary<string, ConfigurationGroup> configurationGroups)
        {
            this.configurationGroups = configurationGroups;
        }
        
        public Template Read(ObjectElement templateElement)
        {
            var template = new Template();
            
            foreach (ConfigurationGroup group in configurationGroups.Values)
            {
                foreach (Configuration configuration in group.Configurations.Values)
                {
                    ReadForConfiguration(templateElement, group, configuration, template);
                }
            }

            return new Template();
        }

        private void ReadForConfiguration(ObjectElement templateElement, ConfigurationGroup group, 
            Configuration configuration, Template template)
        {
            // WIP HERE
            var rootReader = new SettingsReader(configuration, null);
            Settings rootSettings = rootReader.Read(templateElement);
            // set project declarations into template
        }
    }
}