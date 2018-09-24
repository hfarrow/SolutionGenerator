using System.Collections.Generic;

namespace SolutionGen.Parser.Model
{
    public abstract class ContainerElement : ConfigElement
    {
        public IEnumerable<ConfigElement> Children { get; }

        protected ContainerElement(IEnumerable<ConfigElement> children, string conditionalExpression)
            : base(conditionalExpression)
        {
            Children = children;
        }

        public IEnumerable<(ConfigElement parent, ConfigElement child)> EnumerateDecendants()
        {
            foreach (ConfigElement child in Children)
            {
                yield return (this, child);
                if (child is ContainerElement container)
                {
                    foreach ((ConfigElement parent, ConfigElement child) decendant in container.EnumerateDecendants())
                    {
                        yield return (decendant.parent, decendant.child);
                    }
                }
            }
        }
    }
}