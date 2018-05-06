using System.Collections.Generic;

namespace SolutionGenerator.Parsing.Model
{
    public class ConfigDocument
    {
        public IEnumerable<ConfigElement> RootElements { get; }

        public ConfigDocument(IEnumerable<ConfigElement> rootElements)
        {
            RootElements = rootElements;
        }

        public IEnumerable<ConfigElement> EnumerateRecursively()
        {
            foreach (ConfigElement root in RootElements)
            {
                if (root is ObjectElement obj)
                {
                    yield return obj;
                    foreach (ConfigElement child in obj.EnumerateRecursively())
                    {
                        yield return child;
                    }
                }
                else
                {
                    yield return root;
                }
                
            }
        }
    }
}