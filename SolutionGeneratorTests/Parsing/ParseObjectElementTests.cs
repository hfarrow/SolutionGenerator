using System.Linq;
using SolutionGenerator.Parsing.Model;
using Sprache;
using Xunit;

namespace SolutionGenerator.Parsing.Tests
{
    public class ParseObjectElementTests
    {
        [Fact]
        public void CanParseSingleLineProperty()
        {
            const string input = "set include files (false): value";
            ObjectElement element = DocumentParser.ObjectElement.Parse(input);
            Assert.NotNull(element);
            Assert.IsType<PropertyElement>(element);
        }

        [Fact]
        public void CanParsePropertyArray()
        {
            const string input =
                "add lib refs\n" +
                "[\n" +
                "    Test/Path/A\n" +
                "    Test/Path/B\n" +
                "    Test/Path/C\n" +
                "]";

            ObjectElement element = DocumentParser.ObjectElement.Parse(input);
            var property = element as PropertyElement;
            Assert.NotNull(property);
            var array = property.Value as ArrayValue;
            Assert.NotNull(array);
            Assert.Equal(3, array.Values.Count());
            Assert.Equal("Test/Path/A", array.Values.ElementAt(0).Value);
            Assert.Equal("Test/Path/C", array.Values.ElementAt(2).Value);
        }
    }
}