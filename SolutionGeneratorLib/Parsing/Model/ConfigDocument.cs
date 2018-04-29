using System.Collections.Generic;
using System.Linq;

namespace SolutionGenerator.Parsing.Model
{
    public class ConfigDocument
    {
        public IEnumerable<ObjectElement> RootElements { get; }

        public ConfigDocument(IEnumerable<ObjectElement> rootElements)
        {
            RootElements = rootElements;
        }

        public IEnumerable<ObjectElement> EnumerateRecursively()
        {
            foreach (ObjectElement root in RootElements)
            {
                if (root is ConfigObject obj)
                {
                    foreach (ObjectElement child in obj.EnumerateRecursively())
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