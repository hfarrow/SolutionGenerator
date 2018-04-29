using System;
using System.Collections.Generic;
using SolutionGenerator.Parsing.Model;
using Sprache;
using KeyValuePair = SolutionGenerator.Parsing.Model.KeyValuePair;

namespace SolutionGenerator.Parsing
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
                from inherits in BasicParser.Identifier
                select inherits)
            .Token();

        /// <summary>
        /// Parses an object heading
        /// </summary>
        /// <remarks>
        /// object-heading = identifier *WSP identifier [*WSP inherited-object]
        /// </remarks>
        public static readonly Parser<ConfigObjectHeading> ObjectHeading =
            (from type in BasicParser.Identifier
                from name in BasicParser.Identifier
                from inherits in InheritedObject.Optional()
                select new ConfigObjectHeading(type, name, inherits.GetOrDefault())
            ).Token();

        public static readonly Parser<PropertyAction> PropertyAction =
            (from action in Parse.IgnoreCase("set").XOr(Parse.IgnoreCase("add")).Text()
                select GetPropertyAction(action))
            .Token();

        public static readonly Parser<string> ConditionalExpression =
            (from body in BasicParser.EnclosedText('(', ')')
                select body.Substring(1, body.Length - 2))
            .Token();

        public static readonly Parser<ValueElement> Value =
            (from arrayEnd in Parse.Char(']').Not()
                from value in BasicParser.QuotedText.Select(txt => new ValueElement(txt))
                    .Or(BasicParser.GlobValue)
                    .Or(PairValue)
                    .Or(Parse.AnyChar.Except(Parse.String("]").Or(Parse.LineTerminator)).AtLeastOnce().Text()
                        .Select(txt => new ValueElement(txt)))
                select value)
            .Token();

        public static readonly Parser<KeyValuePair> PairValue =
            (from key in BasicParser.Identifier
                from delimiter in Parse.Char(':')
                from value in Value
                select new KeyValuePair(key, value))
            .Token();

        public static readonly Parser<PropertyElement> PropertySingleLine =
            (from action in PropertyAction
                from nameParts in BasicParser.IdentifierWord.DelimitedBy(Parse.Char(' '))
                from conditional in ConditionalExpression.Optional()
                from colon in Parse.Char(':').Token()
                from value in Value
                select new PropertyElement(action, nameParts, value, conditional.GetOrElse("true")))
            .Token();
        
        public static Parser<IEnumerable<T>> StronglyTypedArray<T>(Parser<T> valueParser)
            where T : ValueElement
        {
            return (from lbraket in Parse.Char('[')
                    from values in valueParser.Token().Many().Token()
                    from rbracket in Parse.Char(']')
                    select values)
                .Token();
        }

        public static readonly Parser<IEnumerable<ValueElement>> Array = StronglyTypedArray(Value);

        // Note: Property arrays are 1 dimensional and values cannot be another array.
        public static readonly Parser<PropertyElement> PropertyArray =
            (from action in PropertyAction
                from nameParts in BasicParser.IdentifierWord.DelimitedBy(Parse.Char(' ')).Token()
                from conditional in ConditionalExpression.Optional()
                from values in Array
                select new PropertyElement(action, nameParts, new ArrayValue(values),
                    conditional.GetOrElse("true")))
            .Token();

        public static readonly Parser<ConfigurationElement> Configuration =
            (from cmd in Parse.String("configuration").Token()
                from configName in BasicParser.Identifier.Token()
                from values in StronglyTypedArray(PairValue)
                select new ConfigurationElement(configName, values))
            .Token();

        public static readonly Parser<CommandElement> SimpleCommand =
            (from cmd in BasicParser.Identifier
                from conditional in ConditionalExpression.Optional()
                select new CommandElement(cmd, conditional.GetOrElse("true")))
            .Token();

        public static readonly Parser<ConfigObject> Object =
            (from heading in ObjectHeading
                from lbrace in Parse.Char('{').Token()
                from elements in ObjectElement.Many()
                from rbrace in Parse.Char('}').Token()
                select new ConfigObject(heading, elements))
            .Token();
        
        public static readonly Parser<ObjectElement> ObjectElement =
            from element in
                PropertySingleLine
                    .Or((Parser<ObjectElement>)PropertyArray)
                    .Or(Configuration)
                    .Or(Object)
                    .Or(BasicParser.CommentSingleLine.Select(c => new CommentElement(c)))
            select element;

        public static readonly Parser<ConfigDocument> Document =
            (from elements in ObjectElement.Many()
                select new ConfigDocument(elements))
            .Token().End();

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