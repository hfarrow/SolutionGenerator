using System.Collections.Generic;
using SolutionGen.Parser.Model;
using Sprache;

namespace SolutionGen.Parser
{
    public static class DocumentParser
    {
        /// <summary>
        /// Parses object inheritance declaration
        /// </summary>
        /// <remarks>
        /// inherited-object = ":" *WSP identifier
        /// </remarks>
        public static readonly Parser<string> InheritedObject =
            (from colon in Parse.Char(':').Token()
                from inherits in BasicParser.IdentifierToken
                select inherits)
            .Token().Named("inherited-object");

        /// <summary>
        /// Parses an object heading
        /// </summary>
        /// <remarks>
        /// object-heading = identifier *WSP identifier [*WSP inherited-object]
        /// </remarks>
        public static readonly Parser<ObjectElementHeading> ObjectHeading =
            (from type in BasicParser.IdentifierToken
                from name in BasicParser.IdentifierToken
                from inherits in InheritedObject.Optional()
                select new ObjectElementHeading(type, name, inherits.GetOrDefault())
            ).Token().Named("object-heading");

        public static readonly Parser<PropertyAction> PropertyAction =
            (from action in Parse.IgnoreCase("=").XOr(Parse.IgnoreCase("+=")).Text()
                from space in Parse.WhiteSpace.AtLeastOnce().XOr(Parse.LineTerminator)
                select GetPropertyAction(action))
            .Token().Named("property-action");

        public static readonly Parser<string> ConditionalExpression =
            (from start in Parse.String("if").Text().Token()
                from body in BasicParser.EnclosedText('(', ')')
                select body.Substring(1, body.Length - 2))
            .Token().Named("conditional-expression");
        
        public static readonly Parser<ValueElement> Value =
            (from value in BasicParser.QuotedText.Select(txt => new ValueElement(txt))
                    .Or(BasicParser.GlobValue)
                    .Or(BasicParser.RegexValue)
                    .Or(BasicParser.XmlValue)
                    .Or(BasicParser.NoneValue)
                    // Unquoted Text
                    .Or(Parse.AnyChar.Except(
                            Parse.String(",").Or(Parse.String("]").Or(Parse.LineTerminator)))
                        .AtLeastOnce().Text().Select(txt => new ValueElement(txt)))
                select value)
            .Token().Named("value-element");


        public static readonly Parser<PropertyElement> PropertySingleLine =
            (from conditional in ConditionalExpression.Optional()
                from nameParts in BasicParser.Identifier.DelimitedBy(Parse.Char(' '))
                from action in PropertyAction
                from value in Value
                select new PropertyElement(action, nameParts, value, conditional.GetOrElse("true")))
            .Token().Named("single-line-property");

        public static readonly Parser<string> ArrayEnd =
            from comma in Parse.Char(',').Token().Optional()
            from bracket in Parse.Char(']')
            select string.Empty;
        
        public static Parser<IEnumerable<T>> StronglyTypedArray<T>(Parser<T> valueParser)
            where T : ValueElement
        {
            return (from lbraket in Parse.Char('[')
                    from values in valueParser.DelimitedBy(Parse.Char(',').Token()).Optional().Token()
                    from rbracket in ArrayEnd
                    select values.GetOrElse(new T[0]))
                .Token().Named("typed-array");
        }

        public static readonly Parser<IEnumerable<ValueElement>> Array =
            StronglyTypedArray(Value).Named("untyped-array");

        public static readonly Parser<PropertyElement> PropertyDictionary =
            (from conditional in ConditionalExpression.Optional()
                from nameParts in BasicParser.Identifier.DelimitedBy(Parse.Char(' ')).Token()
                from action in PropertyAction
                from obj in InlineObject
                select new PropertyElement(action, nameParts, new ValueElement(obj), conditional.GetOrElse("true"))
            )
            .Token().Named("dictionary-property");
            
        // Note: Property arrays are 1 dimensional and values cannot be another array.
        public static readonly Parser<PropertyElement> PropertyArray =
            (from conditional in ConditionalExpression.Optional()
                from nameParts in BasicParser.Identifier.DelimitedBy(Parse.Char(' ')).Token()
                from action in PropertyAction
                from values in Array
                select new PropertyElement(action, nameParts, new ArrayValue(values),
                    conditional.GetOrElse("true")))
            .Token().Named("array-property");
        
        public static readonly Parser<SimpleCommandElement> SimpleCommand =
            (from conditional in ConditionalExpression.Optional()
                from cmd in BasicParser.IdentifierToken
                from args in BasicParser.QuotedText.Optional()
                select new SimpleCommandElement(
                    cmd,
                    args.GetOrElse(string.Empty),
                    conditional.GetOrElse(string.Empty).Length >= 3 ? conditional.GetOrDefault() : "true"))
            .Token().Named("command-element");

        public static readonly Parser<ObjectElement> NamedObject =
            (from heading in ObjectHeading
                from lbrace in Parse.Char('{').Token()
                from elements in ObjectElement.XMany()
                from rbrace in Parse.Char('}').Token()
                select new ObjectElement(heading, elements))
            .Token().Named("object-with-heading");
        
        public static readonly Parser<ObjectElement> InlineObject =
            (from lbrace in Parse.Char('{').Token()
                from elements in ObjectElement.XMany()
                from rbrace in Parse.Char('}').Token()
                select new ObjectElement(new ObjectElementHeading("<inline>", "", null), elements))
            .Token().Named("object");
        
        public static readonly Parser<ConditionalBlockElement> ConditionalBlockElement =
            (from conditional in ConditionalExpression.Text()
                // TODO: block lines below are used three times. extract into field parser
                from lbrace in Parse.Char('{').Token()
                from elements in ObjectElement.XMany()
                from rbrace in Parse.Char('}').Token()
                select new ConditionalBlockElement(conditional, elements))
            .Token().Named("conditional-block");
        
        public static readonly Parser<ConfigElement> ObjectElement =
            (from element in PropertyDictionary
                    .Or((Parser<ConfigElement>)PropertyArray)
                    .Or(PropertySingleLine)
                    .Or(ConditionalBlockElement)
                    .Or(NamedObject)
                    .Or(SimpleCommand)
                    .Or(BasicParser.CommentSingleLine.Select(c => new CommentElement(c)))
                select element)
            .Named("object-element");

        public static readonly Parser<ConfigDocument> Document =
            (from elements in ObjectElement.XMany()
                select new ConfigDocument(elements))
            .Token().End().Named("document");

        private static PropertyAction GetPropertyAction(string actionStr)
        {
            switch (actionStr)
            {
                case "=":
                    return Model.PropertyAction.Set;
                case "+=":
                    return Model.PropertyAction.Add;
                default:
                    return Model.PropertyAction.Invalid;
            }
        }
    }
}