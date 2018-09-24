using System.Collections.Generic;
using System.Linq;

namespace SolutionGen.Parser.Model
{
    public sealed class ObjectElement : ContainerElement
    {
        public ObjectElementHeading Heading { get; }
        
        public ObjectElement(ObjectElementHeading heading, IEnumerable<ConfigElement> children) 
            : base(children, "true")
        {
            Heading = heading;
        }

        public override string ToString()
        {
            return "Object" + Heading;
        }
    }
}