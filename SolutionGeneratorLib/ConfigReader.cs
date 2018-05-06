using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGenerator.Compiling.Model;
using SolutionGenerator.Parsing.Model;

namespace SolutionGenerator
{
    public enum SectionType
    {
        Solution,
        Module,
        Template,
        Settings,
    }

    public class ConfigReader
    {
        private readonly ConfigDocument solutionDoc;
        public Dictionary<string, Template> Templates { get; } = new Dictionary<string, Template>();
        public Solution Solution { get; }
        public Dictionary<string, Module> Modules { get; } = new Dictionary<string, Module>();
        
        public ConfigReader(ConfigDocument solutionDoc)
        {
            this.solutionDoc = solutionDoc;
            ProcessSolutionDoc();
        }

        private void ProcessSolutionDoc()
        {
            ObjectElement solutionElement = null;
            var moduleElements = new List<ObjectElement>();
            var templateElements = new List<ObjectElement>();
            
            foreach (ConfigElement element in solutionDoc.RootElements)
            {
                if (element is ObjectElement obj)
                {
                    if (obj.Heading.Type == SectionType.Solution.ToString().ToLower())
                    {
                        solutionElement = obj;
                    }
                    else if (obj.Heading.Type == SectionType.Module.ToString().ToLower())
                    {
                        moduleElements.Add(obj);
                    }
                    else if (obj.Heading.Type == SectionType.Template.ToString().ToLower())
                    {
                        templateElements.Add(obj);
                    }
                    else
                    {
                        throw new InvalidObjectType(obj,
                            SectionType.Solution, SectionType.Module, SectionType.Template);
                    }
                }
            }
            
            ProcessTemplates(templateElements);
            ProcessSolution(solutionElement);
            ProcessModules(moduleElements);
        }

        private void ProcessSolution(ObjectElement solutionElement)
        {
            
        }

        private void ProcessModules(IEnumerable<ObjectElement> moduleElements)
        {
            
        }
        
        private void ProcessTemplates(IEnumerable<ObjectElement> templateElements)
        {
            foreach (ObjectElement templateElement in templateElements)
            {
                if (Templates.ContainsKey(templateElement.Heading.Name))
                {
                    throw new DuplicateTemplateNameException(templateElement,
                        Templates[templateElement.Heading.Name].TemplateObject);
                }

                var template = new Template(templateElement);
                Templates.Add(templateElement.Heading.Name, template);
            }
        }
    }

    public class InvalidObjectType : Exception
    {
        public InvalidObjectType(ObjectElement obj, params SectionType[] expectedTypes)
            : base(string.Format("'{0}' is not one of the expected types: {1}",
                obj.Heading.Type,
                string.Join(", ", expectedTypes.Select(t => t.ToString().ToLower()))))
        {
            
        }
    }
   
    public class DuplicateObjectNameException : Exception
    {
        public DuplicateObjectNameException(ObjectElement newElement, ObjectElement existingElement)
            : base(string.Format("A configuration with name '{0}' has already been defined:\n" +
                                 "Existing Configuration:\n{1}\n" +
                                 "Invalid Configuration:\n{2}",
                newElement.Heading.Name, existingElement, newElement))
        {
            
        }
    }

    public class DuplicateTemplateNameException : DuplicateObjectNameException
    {
        public DuplicateTemplateNameException(ObjectElement newElement, ObjectElement existingElement)
            : base(newElement, existingElement)
        {
            
        }
    }
}