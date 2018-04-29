using System.Collections.Generic;

namespace SolutionGenerator.Parsing.Model
{
    public class PropertyElement : CommandElement
    {
        public PropertyAction Action { get; }
        public IEnumerable<string> NameParts { get; }
        public string FullName { get; }
        public ValueElement Value { get; }

        public PropertyElement(PropertyAction action, IEnumerable<string> nameParts, ValueElement value,
            string conditionalExpression)
            : base(action.ToString(), conditionalExpression)
        {
            Action = action;
            NameParts = nameParts;
            Value = value;

            FullName = string.Join(' ', NameParts);
        }
    }
}