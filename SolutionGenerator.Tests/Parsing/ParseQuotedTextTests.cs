using SolutionGen.Parser;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParseQuotedTextTests
    {
        [Theory]
        [InlineData("\"Hello World\"")]
        [InlineData("\"HelloWorld\"")]
        [InlineData("\"Hello\nWorld\"")]
        [InlineData(" \"HelloWorld\" ")]
        public void QuotedTextReturnsValueBetweenQuotes(string input)
        {
            string text = BasicParser.QuotedText.Parse(input);
            Assert.Equal(input.Trim(' ', '"'), text);
        }
        
        [Fact]
        public void QuotedTextCanReturnEmptyString()
        {
            string text = BasicParser.QuotedText.Parse("\"\"");
            Assert.Equal(string.Empty, text);
        }

        [Fact]
        public void QuotedTextCanContainEscapedQuotations()
        {
            const string input = "\"Hello \\\"World\\\"\"";
            const string expected = "Hello \"World\"";
            string text = BasicParser.QuotedText.Parse(input);
            Assert.Equal(expected, text);
        }
    }
}