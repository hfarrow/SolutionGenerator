using System.Collections.Generic;
using System.Linq;

namespace SolutionGen.Parser.Model
{
    public class PropertyElement : CommandElement
    {
        public PropertyAction Action { get; }
        public IEnumerable<string> NameParts { get; }
        public string FullName { get; }
        public ValueElement ValueElement { get; }

        public PropertyElement(PropertyAction action, IEnumerable<string> nameParts, ValueElement valueElement,
            string conditionalExpression)
            : base(action.ToString(), conditionalExpression)
        {
            Action = action;
            NameParts = nameParts.ToArray();
            ValueElement = valueElement;

            FullName = string.Join(' ', NameParts);
        }

        public override string ToString()
        {
            return $"Property{{{FullName} {(Action == PropertyAction.Add ? "+=" : "=")} {ValueElement}}}";
        }
    }
}