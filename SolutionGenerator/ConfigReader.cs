using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SolutionGenerator.Compiling.Model;
using SolutionGenerator.Parsing.Model;
using Module = SolutionGenerator.Compiling.Model.Module;

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
        public Solution Solution { get; private set; }
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
            // Currently solution is empty but in the future could contain template and module include paths
            Solution = new Solution(solutionElement);
        }

        private void ProcessModules(IEnumerable<ObjectElement> moduleElements)
        {
            foreach (ObjectElement moduleElement in moduleElements)
            {
                string templateName = moduleElement.Heading.InheritedObjectName;

                if (Modules.ContainsKey(moduleElement.Heading.Name))
                {
                    throw new DuplicateModuleNameException(moduleElement,
                        Modules[moduleElement.Heading.Name].ModuleElement);
                }
                
                if (string.IsNullOrEmpty(templateName))
                {
                    throw new ModuleMissingTemplateInheritanceException(moduleElement);
                }

                var module = new Module(moduleElement);
                Modules[moduleElement.Heading.Name] = module;
            }
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
                Templates[templateElement.Heading.Name] = template;
            }
        }
    }

    public sealed class InvalidObjectType : Exception
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
        protected DuplicateObjectNameException(string objectType, ObjectElement newElement, ObjectElement existingElement)
            : base(string.Format("A {0} object with name '{1}' has already been defined:\n" +
                                 "Existing object:\n{2}\n" +
                                 "Invalid object:\n{3}",
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

    public sealed class ModuleMissingTemplateInheritanceException : Exception
    {
        public ModuleMissingTemplateInheritanceException(ObjectElement module)
            : base($"Module '{module.Heading.Name}' does not specify a template to inherit from.")
        {
            
        }
    }
}