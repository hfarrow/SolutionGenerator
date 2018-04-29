using SolutionGenerator.Parsing;
using SolutionGenerator.Parsing.Model;
using Sprache;
using Xunit;

namespace SolutionGenerator.Tests.Parsing
{
    public class ParseGlobValueTests
    {
        [Fact]
        public void GlobWithoutWhiteSpaceIsValid()
        {
            const string input = "glob\"**/*.txt\"";
            GlobValue value = BasicParser.GlobValue.Parse(input);
            Assert.Equal("**/*.txt", value.GlobStr);
        }
        
        [Fact]
        public void GlobWithWhiteSpaceIsValid()
        {
            const string input = "glob \"**/*.txt\"";
            GlobValue value = BasicParser.GlobValue.Parse(input);
            Assert.Equal("**/*.txt", value.GlobStr);
        }
    }
}