using System;
using System.Collections.Generic;
using SolutionGen.Parsing.Model;
using Sprache;
using KeyValuePair = SolutionGen.Parsing.Model.KeyValuePair;

namespace SolutionGen.Parsing
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
        public static readonly Parser<ConfigObjectHeading> ObjectHeading =
            (from type in BasicParser.IdentifierToken
                from name in BasicParser.IdentifierToken
                from inherits in InheritedObject.Optional()
                select new ConfigObjectHeading(type, name, inherits.GetOrDefault())
            ).Token().Named("object-heading");

        public static readonly Parser<PropertyAction> PropertyAction =
            (from action in Parse.IgnoreCase("set").XOr(Parse.IgnoreCase("add")).Text()
                from space in Parse.WhiteSpace.AtLeastOnce().XOr(Parse.LineTerminator)
                select GetPropertyAction(action))
            .Token().Named("property-action");

        public static readonly Parser<string> ConditionalExpression =
            (from body in BasicParser.EnclosedText('(', ')')
                select body.Substring(1, body.Length - 2))
            .Token().Named("conditional-expression");

        public static readonly Parser<ValueElement> Value =
            (from arrayEnd in Parse.Char(']').Not()
                from value in BasicParser.QuotedText.Select(txt => new ValueElement(txt))
                    .Or(BasicParser.GlobValue)
                    .Or(BasicParser.NoneValue)
                    .Or(PairValue)
                    .Or(Parse.AnyChar.Except(Parse.String("]").Or(Parse.LineTerminator)).AtLeastOnce().Text()
                        .Select(txt => new ValueElement(txt)))
                select value)
            .Token().Named("value-element");

        public static readonly Parser<KeyValuePair> PairValue =
            (from key in BasicParser.IdentifierToken
                from delimiter in Parse.Char(':')
                from value in Value
                select new KeyValuePair(key, value))
            .Token().Named("pair-value");

        public static readonly Parser<PropertyElement> PropertySingleLine =
            (from action in PropertyAction
                from nameParts in BasicParser.Identifier.DelimitedBy(Parse.Char(' '))
                from conditional in ConditionalExpression.Optional()
                from colon in Parse.Char(':').Token()
                from value in Value
                select new PropertyElement(action, nameParts, value, conditional.GetOrElse("true")))
            .Token().Named("single-line-property");
        
        public static Parser<IEnumerable<T>> StronglyTypedArray<T>(Parser<T> valueParser)
            where T : ValueElement
        {
            return (from lbraket in Parse.Char('[')
                    from values in valueParser.Token().XMany().Token()
                    from rbracket in Parse.Char(']')
                    select values)
                .Token().Named("typed-array");
        }

        public static readonly Parser<IEnumerable<ValueElement>> Array =
            StronglyTypedArray(Value).Named("untyped-array");

        // Note: Property arrays are 1 dimensional and values cannot be another array.
        public static readonly Parser<PropertyElement> PropertyArray =
            (from action in PropertyAction
                from nameParts in BasicParser.Identifier.DelimitedBy(Parse.Char(' ')).Token()
                from conditional in ConditionalExpression.Optional()
                from values in Array
                select new PropertyElement(action, nameParts, new ArrayValue(values),
                    conditional.GetOrElse("true")))
            .Token().Named("array-property");

        public static readonly Parser<ConfigurationElement> Configuration =
            (from cmd in Parse.String("configuration").Token()
                from configName in BasicParser.IdentifierToken.Token()
                from values in StronglyTypedArray(PairValue)
                select new ConfigurationElement(configName, values))
            .Token().Named("configuration-element");

        public static readonly Parser<CommandElement> SimpleCommand =
            (from cmd in BasicParser.IdentifierToken
                from conditional in ConditionalExpression.XOr(Parse.LineTerminator)
                select new CommandElement(cmd, conditional.Length >= 3 ? conditional : "true"))
            .Token().Named("command-element");

        public static readonly Parser<ObjectElement> Object =
            (from heading in ObjectHeading
                from lbrace in Parse.Char('{').Token()
                from elements in ObjectElement.XMany()
                from rbrace in Parse.Char('}').Token()
                select new ObjectElement(heading, elements))
            .Token().Named("object");

        public static readonly Parser<ConfigElement> ObjectElement =
            (from element in PropertySingleLine
                    .Or((Parser<ConfigElement>) PropertyArray)
                    .Or(SimpleCommand)
                    .Or(Configuration)
                    .Or(Object)
                    .Or(BasicParser.CommentSingleLine.Select(c => new CommentElement(c)))
                select element)
            .Named("object-element");

        public static readonly Parser<ConfigDocument> Document =
            (from elements in ObjectElement.XMany()
                select new ConfigDocument(elements))
            .Token().End().Named("document");

        private static PropertyAction GetPropertyAction(string actionStr)
        {
            if (!Enum.TryParse(actionStr, true, out PropertyAction action))
            {
                action = Model.PropertyAction.Invalid;
            }

            return action;
        }
    }
}