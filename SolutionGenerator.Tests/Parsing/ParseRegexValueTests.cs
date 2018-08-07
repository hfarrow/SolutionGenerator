using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParseRegexValueTests
    {
        
        [Fact]
        public void RegexWithoutWhiteSpaceIsValid()
        {
            const string input = "regex\".*\\.txt\"";
            RegexValue value = BasicParser.RegexValue.Parse(input);
            Assert.Equal(".*\\.txt", value.RegexStr);
            Assert.Matches(value.Regex, "match.txt");
        }
        
        [Fact]
        public void RegexWithWhiteSpaceIsValid()
        {
            const string input = "regex \".*\\.txt\"";
            RegexValue value = BasicParser.RegexValue.Parse(input);
            Assert.Equal(".*\\.txt", value.RegexStr);
            Assert.Matches(value.Regex, "match.txt");
        }

        [Fact]
        public void RegexCanBeNegated()
        {
            const string input = "!regex\".*\\.txt\"";
            RegexValue value = BasicParser.RegexValue.Parse(input);
            Assert.Equal(".*\\.txt", value.RegexStr);
            Assert.DoesNotMatch(value.Regex, "match.txt");
        }
        
        [Fact]
        public void RegexCanBeNegatedWithSpaceBeforeKeyword()
        {
            const string input = "! regex\".*\\.txt\"";
            RegexValue value = BasicParser.RegexValue.Parse(input);
            Assert.Equal(".*\\.txt", value.RegexStr);
            Assert.DoesNotMatch(value.Regex, "match.txt");
        }
    }
}