using SolutionGen.Parser.Model;
using SolutionGen.Parser;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParsePropertyActionTests
    {
        [Theory]
        [InlineData("=")]
        [InlineData("+=")]
        public void ValidActionCanBeParsed(string input)
        {
            PropertyAction action = DocumentParser.PropertyAction.Parse(input);
            Assert.NotEqual(PropertyAction.Invalid, action);
            Assert.Equal(input, action == PropertyAction.Add ? "+=" : "=");
        }
    }
}