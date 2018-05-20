using SolutionGen.Parser;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParseConditionalTests
    {
        [Fact]
        public void EnclosedConditionalDoesNotIncludeParen()
        {
            string result = DocumentParser.ConditionalExpression.Parse("(true)");
            Assert.Equal("true", result);
        }
    }
}