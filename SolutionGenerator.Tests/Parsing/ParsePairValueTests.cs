using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
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