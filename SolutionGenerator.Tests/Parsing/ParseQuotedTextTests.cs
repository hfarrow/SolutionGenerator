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
    }
}