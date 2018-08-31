using System;
using System.Collections.Generic;
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
            (from first in Parse.Letter.Or(Parse.Char('_')).Once()
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
        /// Parse a formatted text block enclosed in triple quotations. The quotations should begin on a new line
        /// because the leading white space will be stripped from each line in the body.
        /// Example:
        /// formatted text =
        /// """
        /// text
        /// goes
        /// here
        /// """
        /// </summary>
        /// <remarks>
        /// formatted-text = *EOL *WSP 3""" *CHAR 3"""
        /// </remarks>
        public static readonly Parser<FormattedText> FormattedText =
            (from lineLead in Parse.LineEnd.Optional()
                from lead in Parse.WhiteSpace.Many()
                from open in Parse.Char('"').Repeat(3)
                from eol in Parse.LineEnd.Optional()
                from body in Parse.AnyChar.Except(Parse.Char('"').Repeat(3)).Many().Text()
                from close in Parse.Char('"').Repeat(3)
                select new FormattedText(lead, body.TrimEnd()))
            .Named("formatted-text");
        
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
        /// Parses a blob of xml data enclosed in an xml { ... } element
        /// </summary>
        /// <remarks>
        /// xml = "xml" *WSP "{" *(CHAR / WSP) "}"
        /// </remarks>
        public static readonly Parser<ValueElement> XmlValue =
            (from heading in Parse.String("xml")
                from xml in FormattedText
                select new XmlValue(xml))
            .Token().Named("xml");

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
            return i =>
            {
                if (i.AtEnd)
                {
                    return Result.Failure<string>(i,
                        "Unexpected end of input reached",
                        new[] {$"open char '{openChar}'"});
                }

                if (i.Current != openChar)
                {
                    return Result.Failure<string>(i,
                        $"unexpected '{i.Current}'",
                        new[] {$"open char '{openChar}'"});
                }

                int count = 0;
                int startPos = i.Position;
                while (!i.AtEnd)
                {
                    if (i.Current == openChar)
                    {
                        ++count;
                    }
                    else if (i.Current == closeChar)
                    {
                        --count;
                    }

                    i = i.Advance();

                    if (count == 0)
                    {
                        return Result.Success(i.Source.Substring(startPos, i.Position - startPos), i);
                    }
                }

                return Result.Failure<string>(i,
                    "unexpected end of input reached",
                    new[] {$"close char '{closeChar}'"});

            };
        }
    }
}