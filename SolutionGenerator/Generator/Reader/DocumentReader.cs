using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using SolutionGen.Generator.Model;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;

namespace SolutionGen.Generator.Reader
{
    public class DocumentReader
    {
        private readonly ConfigDocument configDoc;
        private SolutionReader solutionReader;
        private TemplateReader templateReader;
        
        public string SolutionConfigDir { get; }
        public Solution Solution { get; private set; }
        public Dictionary<string, Template> Templates { get; } = new Dictionary<string, Template>();
        public Dictionary<string, Module> Modules { get; } = new Dictionary<string, Module>();
        public IReadOnlyCollection<string> ExcludedProjects => excludedProjects;
        
        public ObjectElement SolutionElement { get; private set; }

        public IReadOnlyList<ObjectElement> TemplateElements => templateElements;
        public IReadOnlyList<ObjectElement> ModuleElements => moduleElements;
        private readonly List<ObjectElement> templateElements = new List<ObjectElement>();
        private readonly List<ObjectElement> moduleElements = new List<ObjectElement>();
        private HashSet<string> excludedProjects = new HashSet<string>();
        
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
                
                templateReader = new TemplateReader(Solution.ConfigurationGroups,
                    solutionReader.TemplateDefaultSettings);
                
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

        private void ReadTemplates(IEnumerable<ObjectElement> allTemplateElements)
        {
            List<ObjectElement> templateList =
                allTemplateElements as List<ObjectElement> ?? allTemplateElements.ToList();

            IEnumerable<IGrouping<string, ObjectElement>> groups =
                templateList.GroupBy(t => t.ElementHeading.Name);

            IGrouping<string, ObjectElement>[] duplicates = groups
                .Where(g => g.Count() > 1)
                .ToArray();

            foreach (IGrouping<string,ObjectElement> duplicate in duplicates)
            {
                Log.Error(
                    "Duplicate template name '{0}' detected. Template names must be unique. See the template headings below:",
                    duplicate.First());
                Log.IndentedCollection(duplicate, Log.Error);
            }

            if (duplicates.Length > 0)
            {
                ObjectElement[] duplicate = duplicates.First().ToArray();
                throw new DuplicateTemplateNameException(duplicate[0], duplicate[1]);
            }

            // Basic technique for processing templates in dependency order and catch cyclic dependency
            // A template can only be processed after the template it inherits has been processed.
            while (templateList.Count > 0)
            {
                var readTemplates = new List<ObjectElement>();
                foreach (ObjectElement template in templateList)
                {
                    if (string.IsNullOrEmpty(template.ElementHeading.InheritedObjectName) ||
                        Templates.ContainsKey(template.ElementHeading.InheritedObjectName))
                    {
                        Templates[template.ElementHeading.Name] = templateReader.Read(template);
                        readTemplates.Add(template);
                    }
                }

                if (readTemplates.Count > 0)
                {
                    templateList.RemoveAll(t => readTemplates.Contains(t));
                }
                else if (templateList.Count > 0)
                {
                    throw new InvalidOperationException(
                        "A cyclic dependency was detected in template inheritance. These templates could not be read: " +
                        string.Join(", ", templateList));
                }
            }
        }
        
        private void ReadModules(IEnumerable<ObjectElement> allElements)
        {
            allElements = allElements as ObjectElement[] ?? allElements.ToArray();
            
            IEnumerable<IGrouping<string, ObjectElement>> groups =
                allElements.GroupBy(t => t.ElementHeading.Name);

            IGrouping<string, ObjectElement>[] duplicates = groups
                .Where(g => g.Count() > 1)
                .ToArray();

            foreach (IGrouping<string,ObjectElement> duplicate in duplicates)
            {
                Log.Error(
                    "Duplicate module name '{0}' detected. Module names must be unique. See the module headings below:",
                    duplicate.First());
                Log.IndentedCollection(duplicate, Log.Error);
            }

            if (duplicates.Length > 0)
            {
                ObjectElement[] duplicate = duplicates.First().ToArray();
                throw new DuplicateModuleNameException(duplicate[0], duplicate[1]);
            }

            
            // One module at a time for debugging if needed.
//            foreach (ObjectElement element in allElements)
//            {
//                Task<ModuleReader.Result> task = ReadModuleAsync(element);
//                task.Wait();
//                excludedProjects.UnionWith(task.Result.ExcludedProjects);
//                Modules[task.Result.Module.Name] = task.Result.Module;
//            }

            Task<ModuleReader.Result>[] tasks = allElements.Select(ReadModuleAsync).ToArray();
            try
            {
                Task.WaitAll(tasks.Cast<Task>().ToArray());
            }
            catch (AggregateException ae)
            {
                Log.Error("One or more exceptions occured while reading modules asynchronously:");
                foreach (Exception ex in ae.Flatten().InnerExceptions)
                {
                    Log.Error(ex.Message);
                }

                throw;
            }
            
            foreach (Task<ModuleReader.Result> taskResult in tasks)
            {
                ModuleReader.Result result = taskResult.Result;
                excludedProjects.UnionWith(result.ExcludedProjects);
                Modules[result.Module.Name] = result.Module;
            }
        }

        private Task<ModuleReader.Result> ReadModuleAsync(ObjectElement moduleElement)
        {
            var reader = new ModuleReader(Solution, Templates, templateReader);
            var baseVars = new Dictionary<string, string>(ExpandableVars.Instance.Variables);
            return Task.Run(() =>
            {
                Thread.CurrentThread.Name = "RM-" + moduleElement.ElementHeading.Name;
                Log.InitBufferedLog();
                ExpandableVars.Init(baseVars);
                ModuleReader.Result module = reader.Read(moduleElement);
                lock (this)
                {
                    Log.FlushBufferedLog();
                }
                return module;
            });
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