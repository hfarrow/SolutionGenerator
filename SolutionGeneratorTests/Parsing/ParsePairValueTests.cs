using SolutionGenerator.Parsing;
using SolutionGenerator.Parsing.Model;
using Sprache;
using Xunit;

namespace SolutionGenerator.Tests.Parsing
{
    public class ParsePairValueTests
    {
        [Fact]
        public void SingleLineKeyValuePairWithWhiteSpaceCanBeParsed()
        {
            const string input = "MyKey: \"MyValue\"";

            KeyValuePair kvp = DocumentParser.PairValue.Parse(input);
            Assert.Equal("MyKey", kvp.PairKey);
            Assert.Equal("MyValue", kvp.PairValue.Value);
        }
        
        [Fact]
        public void SingleLineKeyValuePairWithoutWhiteSpaceCanBeParsed()
        {
            const string input = "MyKey:\"MyValue\"";

            KeyValuePair kvp = DocumentParser.PairValue.Parse(input);
            Assert.Equal("MyKey", kvp.PairKey);
            Assert.Equal("MyValue", kvp.PairValue.Value);
        }
    }
}