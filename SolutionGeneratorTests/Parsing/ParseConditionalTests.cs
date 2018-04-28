using Sprache;
using Xunit;

namespace SolutionGenerator.Parsing.Tests
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