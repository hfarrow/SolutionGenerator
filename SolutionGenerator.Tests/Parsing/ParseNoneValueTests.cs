using SolutionGen.Parsing;
using SolutionGen.Parsing.Model;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParseNoneValueTests
    {
        [Fact]
        public void GlobWithoutWhiteSpaceIsValid()
        {
            const string input = "none";
            ValueElement value = BasicParser.NoneValue.Parse(input);
            Assert.Null(value.Value);
        }
    }
}