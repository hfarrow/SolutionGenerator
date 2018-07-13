using System.Collections.Generic;
using System.Linq;

namespace SolutionGen.Parser.Model
{
    public class ObjectElement : ConfigElement
    {
        public ObjectElementHeading ElementHeading { get; }
        public IEnumerable<ConfigElement> Elements { get; }
        
        public ObjectElement(ObjectElementHeading elementHeading, IEnumerable<ConfigElement> elements) : base("true")
        {
            ElementHeading = elementHeading;
            Elements = elements;
        }

        public IEnumerable<ConfigElement> EnumerateRecursively()
        {
            return Elements.SelectMany(EnumerateRecursively);
        }

        private static IEnumerable<ConfigElement> EnumerateRecursively(ConfigElement root)
        {
            yield return root;

            if (root is ObjectElement obj)
            {
                foreach (ConfigElement child in obj.Elements)
                {
                    foreach (ConfigElement element in EnumerateRecursively(child))
                    {
                        yield return element;
                    }
                }
            }
        }

        public override string ToString()
        {
            return "Object" + ElementHeading;
        }
    }
}