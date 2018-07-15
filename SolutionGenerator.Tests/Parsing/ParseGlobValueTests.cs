using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParseGlobValueTests
    {
        [Fact]
        public void GlobWithoutWhiteSpaceIsValid()
        {
            const string input = "glob\"**/*.txt\"";
            GlobValue value = BasicParser.GlobValue.Parse(input);
            Assert.Equal("**/*.txt", value.GlobStr);
            Assert.False(value.Negated);
        }
        
        [Fact]
        public void GlobWithWhiteSpaceIsValid()
        {
            const string input = "glob \"**/*.txt\"";
            GlobValue value = BasicParser.GlobValue.Parse(input);
            Assert.Equal("**/*.txt", value.GlobStr);
            Assert.False(value.Negated);
        }

        [Fact]
        public void GlobCanBeNegated()
        {
            const string input = "!glob \"**/*.txt\"";
            GlobValue value = BasicParser.GlobValue.Parse(input);
            Assert.Equal("**/*.txt", value.GlobStr);
            Assert.True(value.Negated);
        }
        
        [Fact]
        public void GlobCanBeNegatedWithSpaceBeforeKeyword()
        {
            const string input = "! glob \"**/*.txt\"";
            GlobValue value = BasicParser.GlobValue.Parse(input);
            Assert.Equal("**/*.txt", value.GlobStr);
            Assert.True(value.Negated);
        }
    }
}