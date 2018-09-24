using System.Collections.Generic;

namespace SolutionGen.Parser.Model
{
    public sealed class ConfigDocument : ContainerElement
    {
        public ConfigDocument(IEnumerable<ConfigElement> rootElements)
            : base(rootElements, "true")
        {
            foreach ((ConfigElement parent, ConfigElement child) tuple in EnumerateDecendants())
            {
                tuple.child.ParentElement = tuple.parent;
            }
        }
    }
}