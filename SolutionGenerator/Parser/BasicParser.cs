using System;
using System.Linq;
using SolutionGen.Parser.Model;
using Sprache;

namespace SolutionGen.Parser
{
    public static class BasicParser
    {
        /// <summary>
        /// Parse a single line comment where "//" denotes the beginning of the comment.
        /// </summary>
        /// <remarks>
        /// comment = "//" *CHAR (EOL / EOF)
        /// </remarks>
        public static readonly Parser<string> CommentSingleLine =
            (from first in Parse.String("//").Text()
                from rest in Parse.AnyChar.Until(Parse.LineTerminator).Text()
                select first + rest)
            .Token().Named("comment");

        /// <summary>
        /// Parse an expandable variable in the format of $(VARIABLE_NAME)
        /// </summary>
        /// <remarks>
        /// variable-expansion = "$(" identifier ")"
        /// </remarks>
        public static readonly Parser<string> VariableExpansion =
            (from start in Parse.String("$(").Text()
                from id in IdentifierLiteral
                from close in Parse.String(")").Text()
                select start + id + close)
            .Named("variable-expansion");

        /// <summary>
        /// Parse the inner part of an identifier. Unlike Identifier, this does not require the first char to be a letter.
        /// </summary>
        /// <remarks>
        /// identifier-literal-inner = *(ALPHA / "-" / "_" / ".")
        /// </remarks>
        public static readonly Parser<string> IdentifierLiteralInner =
            (from id in Parse.LetterOrDigit.XOr(Parse.Chars('-', '_', '.')).Many().Text()
                select id)
            .Named("identifier-literal-inner");

        /// <summary>
        /// Parse an identifier literal that must start with a letter. Use IdentifierLiteralInner if to remove the first char
        /// is letter requirement
        /// </summary>
        /// <remarks>
        /// identifier-literal = ALPHA *identifier-literal-inner
        /// </remarks>
        public static readonly Parser<string> IdentifierLiteral =
            (from first in Parse.Letter.Once()
                from rest in IdentifierLiteralInner
                select new string(first.Concat(rest).ToArray()))
            .Named("identifier-literal");

        /// <summary>
        /// Parse an identifier without consuming trailing white space.
        /// </summary>
        /// <remarks>
        /// identifier = (variable-expansion / identifier-literal) *(variable-expansion / identifier-literal-inner)
        /// </remarks>
        public static readonly Parser<string> Identifier =
            (from first in VariableExpansion.XOr(IdentifierLiteral)
                from rest in VariableExpansion.XOr(IdentifierLiteralInner).XMany()
                select first + string.Concat(rest))
            .Named("identifier");

        /// <summary>
        /// Parse an identifier and consume trailing white space.
        /// </summary>
        /// <remarks>
        /// identifier-token = *WSP identifier *WSP
        /// </remarks>
        public static readonly Parser<string> IdentifierToken =
            (from word in Identifier.Token()
                select word)
            .Named("identifier-token");

        /// <summary>
        /// Parse text enclosed within double quotations. Does not allow for escaped quotes within the text.
        /// <remarks>
        /// quoted-text = """ *(CHAR / "\") """
        /// </remarks>
        /// </summary>
        public static readonly Parser<string> QuotedText =
            (from open in Parse.Char('"')
                from content in
                    Parse.String("\\\"").Return('"').Or(Parse.CharExcept('"'))
                        .Many().Text()
                from close in Parse.Char('"')
                select content)
            .Token().Named("quoted-text");
        
        /// <summary>
        /// Parses a glob pattern returning only the pattern string
        /// </summary>
        /// <remarks>
        /// glob = "glob" *WSP quoted-text
        /// </remarks>
        public static readonly Parser<GlobValue> GlobValue =
            (from negated in Parse.Char('!').Optional().Token()
                from keyword in Parse.String("glob").Token()
                from value in QuotedText
                select new GlobValue(value, negated.IsDefined))
            .Token().Named("glob");
        
        /// <summary>
        /// Parses a regular expression pattern returning only the pattern string
        /// </summary>
        /// <remarks>
        /// glob = "glob" *WSP quoted-text
        /// </remarks>
        public static readonly Parser<RegexValue> RegexValue =
            (from negated in Parse.Char('!').Optional().Token()
                from keyword in Parse.String("regex").Token()
                from value in QuotedText
                select new RegexValue(value, negated.IsDefined))
            .Token().Named("regex");

        /// <summary>
        /// Parse the none keyword which generally repreasents nothing or an empty collection.
        /// </summary>
        /// <remarks>
        /// none = "none"
        /// </remarks>
        public static readonly Parser<ValueElement> NoneValue =
            (from none in Parse.String("none")
                select new ValueElement(null))
            .Token().Named("none");
            

        /// <summary>
        /// Parses text enclosed within opening and closing char allowing for nested inner matches.
        /// The nexted sets are parsed recursively.
        /// For example, if <paramref name="openChar"/> is a left parenthesis and <paramref name="closeChar"/> is a
        /// right parenthesis, then the parser will match this entire string: "(t(e()s)t)".
        /// The open and close char are included in the parsed text.
        /// </summary>
        /// <remarks>
        /// enclosed-text = openChar *CHAR [enclosed-text] *CHAR closeChar
        /// </remarks>
        /// <param name="openChar">The opening character. Usually something like an opening bracket.</param>
        /// <param name="closeChar">The closing character. Usually something like an closing bracket.</param>
        /// <returns></returns>
        public static Parser<string> EnclosedText(char openChar, char closeChar)
        {
            return
                (from left in Parse.Char(openChar).Once().Text()
                    from leftBody in Parse.AnyChar.Except(Parse.Chars(openChar, closeChar)).Many().Text()
                    from innerBody in EnclosedText(openChar, closeChar).Optional()
                    from rightBody in Parse.AnyChar.Except(Parse.Char(closeChar)).Many().Text()
                    from right in Parse.Char(closeChar).Once().Text()
                    select left + leftBody + innerBody.GetOrElse(string.Empty) + rightBody + right)
                .Token().Named("enclosed-text");
        }
    }
}