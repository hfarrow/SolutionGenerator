using System;
using System.Collections.Generic;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;

namespace SolutionGen.Generator.Reader
{
    public class DocumentReader
    {
        private readonly ConfigDocument configDoc;
        private SolutionReader solutionReader;
        
        public string SolutionConfigDir { get; }
        public Solution Solution { get; private set; }
        public Dictionary<string, Template> Templates { get; } = new Dictionary<string, Template>();
        public Dictionary<string, Module> Modules { get; } = new Dictionary<string, Module>();
        
        public DocumentReader(ConfigDocument configDoc, string solutionConfigDir)
        {
            this.configDoc = configDoc;
            SolutionConfigDir = solutionConfigDir;
            ProcessSolutionDocument();
        }

        private void ProcessSolutionDocument()
        {
            var moduleElements = new List<ObjectElement>();
            var templateElements = new List<ObjectElement>();
            
            foreach (ConfigElement element in configDoc.RootElements)
            {
                if (element is ObjectElement obj)
                {
                    if (obj.Heading.Type.Equals(SectionType.SOLUTION, StringComparison.OrdinalIgnoreCase))
                    {
                        solutionReader = new SolutionReader(obj, SolutionConfigDir);
                        Solution = solutionReader.Solution;
                    }
                    else if (obj.Heading.Type.Equals(SectionType.MODULE, StringComparison.OrdinalIgnoreCase))
                    {
                        moduleElements.Add(obj);
                    }
                    else if (obj.Heading.Type.Equals(SectionType.TEMPLATE, StringComparison.OrdinalIgnoreCase))
                    {
                        templateElements.Add(obj);
                    }
                    else
                    {
                        throw new InvalidObjectType(obj,
                            SectionType.SOLUTION, SectionType.MODULE, SectionType.TEMPLATE);
                    }
                }
            }
            
            ProcessTemplates(templateElements);
            ProcessModules(moduleElements);
        }

        private void ProcessTemplates(IEnumerable<ObjectElement> templateElements)
        {
            var parsedObjectsLookup = new Dictionary<string, ObjectElement>();
            
            var reader = new TemplateReader(Solution.Settings.ConfigurationGroups);
            foreach (ObjectElement templateElement in templateElements)
            {
                if (Templates.ContainsKey(templateElement.Heading.Name))
                {
                    throw new DuplicateTemplateNameException(templateElement,
                        parsedObjectsLookup[templateElement.Heading.Name]);
                }

                parsedObjectsLookup[templateElement.Heading.Name] = templateElement;
                Templates[templateElement.Heading.Name] = reader.Read(templateElement);
            }
        }
        
        private void ProcessModules(IEnumerable<ObjectElement> moduleElements)
        {
            var parsedObjectsLookup = new Dictionary<string, ObjectElement>();
            var reader = new ModuleReader(Solution, Templates);
            foreach (ObjectElement moduleElement in moduleElements)
            {
                if (Modules.ContainsKey(moduleElement.Heading.Name))
                {
                    throw new DuplicateModuleNameException(moduleElement,
                        parsedObjectsLookup[moduleElement.Heading.Name]);
                }
                
                parsedObjectsLookup[moduleElement.Heading.Name] = moduleElement;
                Modules[moduleElement.Heading.Name] = reader.Read(moduleElement);
            }
        }
    }

    public sealed class InvalidObjectType : Exception
    {
        public InvalidObjectType(ObjectElement obj, params string[] expectedTypes)
            : base(string.Format("'{0}' is not one of the expected types: {1}",
                obj.Heading.Type, string.Join(", ", expectedTypes)))
        {
            
        }
    }
   
    public class DuplicateObjectNameException : Exception
    {
        protected DuplicateObjectNameException(string objectType, ObjectElement newElement, ObjectElement existingElement)
            : base(string.Format("{0} object with name '{1}' has already been defined:\n" +
                                 "Existing object:\n{2}\n" +
                                 "Duplicate object:\n{3}",
                objectType, newElement.Heading.Name, existingElement, newElement))
        {
            
        }
    }

    public sealed class DuplicateTemplateNameException : DuplicateObjectNameException
    {
        public DuplicateTemplateNameException(ObjectElement newElement, ObjectElement existingElement)
            : base("template", newElement, existingElement)
        {
            
        }
    }
    
    public sealed class DuplicateModuleNameException : DuplicateObjectNameException
    {
        public DuplicateModuleNameException(ObjectElement newElement, ObjectElement existingElement)
            : base("module", newElement, existingElement)
        {
            
        }
    }
}