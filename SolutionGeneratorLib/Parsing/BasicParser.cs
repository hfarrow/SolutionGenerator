using System.Linq;
using SolutionGenerator.Parsing.Model;
using Sprache;

namespace SolutionGenerator.Parsing
{
    public static class BasicParser
    {
        public static readonly Parser<string> CommentSingleLine =
            (from first in Parse.String("//").Text()
                from rest in Parse.AnyChar.Until(Parse.LineTerminator).Text()
                select first + rest)
            .Token();
        
        /// <summary>
        /// Parse an identifier without consuming trailing white space.
        /// </summary>
        /// <remarks>
        /// identifier = ALPHA *(ALPHA / "-" / "_")
        /// </remarks>
        public static readonly Parser<string> IdentifierWord =
            from first in Parse.Letter.Once()
            from rest in Parse.LetterOrDigit.XOr(Parse.Char('-')).XOr(Parse.Char('_')).Many()
            select new string(first.Concat(rest).ToArray());

        /// <summary>
        /// Parse an identifier and consume trailing white space.
        /// </summary>
        public static readonly Parser<string> Identifier =
            from word in IdentifierWord.Token()
            select word;

        /// <summary>
        /// Parse text enclosed within double quotations. Does not allow for escaped quotes within the text.
        /// <remarks>
        /// quoted-text = """ *CHAR """
        /// </remarks>
        /// </summary>
        public static readonly Parser<string> QuotedText =
            (from open in Parse.Char('"')
                from content in Parse.CharExcept('"').Many().Text()
                from close in Parse.Char('"')
                select content)
            .Token();
        
        /// <summary>
        /// Parses a glob pattern returning only the pattern string
        /// </summary>
        /// <remarks>
        /// glob = "glob" *WSP quoted-text
        /// </remarks>
        public static readonly Parser<GlobValue> GlobValue =
            (from keyword in Parse.String("glob").Token()
                from value in QuotedText
                select new GlobValue(value))
            .Token();

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
                .Token();
        }
    }
}