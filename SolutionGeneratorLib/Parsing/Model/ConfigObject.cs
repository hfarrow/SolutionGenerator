using System.Collections.Generic;
using System.Linq;

namespace SolutionGenerator.Parsing.Model
{
    public class ConfigObject : ObjectElement
    {
        public ConfigObjectHeading Heading { get; }
        public IEnumerable<ObjectElement> Elements { get; }
        
        public ConfigObject(ConfigObjectHeading heading, IEnumerable<ObjectElement> elements) : base("true")
        {
            Heading = heading;
            Elements = elements;
        }

        public IEnumerable<ObjectElement> EnumerateRecursively()
        {
            return Elements.SelectMany(EnumerateRecursively);
        }

        private static IEnumerable<ObjectElement> EnumerateRecursively(ObjectElement root)
        {
            yield return root;

            if (root is ConfigObject obj)
            {
                foreach (ObjectElement child in obj.Elements)
                {
                    foreach (ObjectElement element in EnumerateRecursively(child))
                    {
                        yield return element;
                    }
                }
            }
        }
    }
}