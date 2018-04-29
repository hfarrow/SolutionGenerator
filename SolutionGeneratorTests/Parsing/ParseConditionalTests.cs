using SolutionGenerator.Parsing;
using Sprache;
using Xunit;

namespace SolutionGenerator.Tests.Parsing
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