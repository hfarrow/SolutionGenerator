using System;
using System.Collections.Generic;
using System.Linq;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;

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
        
        public ObjectElement SolutionElement { get; private set; }

        public IReadOnlyList<ObjectElement> TemplateElements => templateElements;
        public IReadOnlyList<ObjectElement> ModuleElements => moduleElements;
        private readonly List<ObjectElement> templateElements = new List<ObjectElement>();
        private readonly List<ObjectElement> moduleElements = new List<ObjectElement>();
        
        public DocumentReader(ConfigDocument configDoc, string solutionConfigDir)
        {
            this.configDoc = configDoc;
            SolutionConfigDir = solutionConfigDir;
        }

        public void ParseSolution()
        {
            ParseElements();

            if (SolutionElement == null)
            {
                throw new MissingElementException(
                    $"The main solution document must contain a root level '{SectionType.SOLUTION}' object.");
            }
            
            solutionReader = new SolutionReader(SolutionElement, SolutionConfigDir, false);
            Solution = solutionReader.Solution;
        }

        public void ReadFullSolution(PropertyElement[] propertyOverrides = null,
            string masterConfiguration = null, 
            string[] configurationFilter = null)
        {
            if (SolutionElement == null)
            {
                ParseSolution();
            }

            
            Log.Heading("Reading full solution (templates and modules)");
            using (new CompositeDisposable(
                new Log.ScopedIndent(),
                new Log.ScopedTimer(Log.Level.Info, "Read Full Solution")))
            {
                if (!string.IsNullOrEmpty(masterConfiguration))
                {
                    Solution.FilterMasterConfigurations(new[] {masterConfiguration});
                }
                
                if (configurationFilter != null)
                {
                    Solution.FilterConfigurations(configurationFilter);
                }

                
                solutionReader.ApplyPropertyOverrides(propertyOverrides);
                solutionReader.LoadIncludes();
                ReadTemplates(TemplateElements.Concat(solutionReader.IncludedTemplates));
                ReadModules(ModuleElements.Concat(solutionReader.IncludedModules));
            }
        }
        
        public void ParseElements()
        {
            foreach (ConfigElement element in configDoc.RootElements)
            {
                if (element is ObjectElement obj)
                {
                    if (obj.ElementHeading.Type.Equals(SectionType.SOLUTION, StringComparison.OrdinalIgnoreCase))
                    {
                        SolutionElement = obj;
                    }
                    else if (obj.ElementHeading.Type.Equals(SectionType.MODULE, StringComparison.OrdinalIgnoreCase))
                    {
                        moduleElements.Add(obj);
                    }
                    else if (obj.ElementHeading.Type.Equals(SectionType.TEMPLATE, StringComparison.OrdinalIgnoreCase))
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
        }

        private void ReadTemplates(IEnumerable<ObjectElement> templateElements)
        {
            var parsedObjectsLookup = new Dictionary<string, ObjectElement>();

            var reader = new TemplateReader(Solution.ConfigurationGroups,
                solutionReader.TemplateDefaultSettings);
            foreach (ObjectElement templateElement in templateElements)
            {
                if (Templates.ContainsKey(templateElement.ElementHeading.Name))
                {
                    throw new DuplicateTemplateNameException(templateElement,
                        parsedObjectsLookup[templateElement.ElementHeading.Name]);
                }

                parsedObjectsLookup[templateElement.ElementHeading.Name] = templateElement;
                Templates[templateElement.ElementHeading.Name] = reader.Read(templateElement);
            }
        }
        
        private void ReadModules(IEnumerable<ObjectElement> moduleElements)
        {
            var parsedObjectsLookup = new Dictionary<string, ObjectElement>();
            var reader = new ModuleReader(Solution, Templates);
            foreach (ObjectElement moduleElement in moduleElements)
            {
                if (Modules.ContainsKey(moduleElement.ElementHeading.Name))
                {
                    throw new DuplicateModuleNameException(moduleElement,
                        parsedObjectsLookup[moduleElement.ElementHeading.Name]);
                }
                
                parsedObjectsLookup[moduleElement.ElementHeading.Name] = moduleElement;
                Modules[moduleElement.ElementHeading.Name] = reader.Read(moduleElement);
            }
        }
    }

    public sealed class InvalidObjectType : Exception
    {
        public InvalidObjectType(ObjectElement obj, params string[] expectedTypes)
            : base(string.Format("'{0}' is not one of the expected types: {1}",
                obj.ElementHeading.Type, string.Join(", ", expectedTypes)))
        {
            
        }
    }
   
    public class DuplicateObjectNameException : Exception
    {
        protected DuplicateObjectNameException(string objectType, ObjectElement newElement, ObjectElement existingElement)
            : base(string.Format("{0} object with name '{1}' has already been defined:\n" +
                                 "Existing object:\n{2}\n" +
                                 "Duplicate object:\n{3}",
                objectType, newElement.ElementHeading.Name, existingElement, newElement))
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

    public sealed class MissingElementException : Exception
    {
        public MissingElementException(string message)
            : base(message)
        {
            
        }
    }
}