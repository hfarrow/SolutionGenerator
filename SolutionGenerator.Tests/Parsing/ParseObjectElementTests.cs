using System.Linq;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParseObjectElementTests
    {
        [Fact]
        public void CanParseSingleLineProperty()
        {
            const string input = "if (false) set include files: value";
            ConfigElement element = DocumentParser.ObjectElement.Parse(input);
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

            ConfigElement element = DocumentParser.ObjectElement.Parse(input);
            var property = element as PropertyElement;
            Assert.NotNull(property);
            var array = property.ValueElement as ArrayValue;
            Assert.NotNull(array);
            Assert.Equal(3, array.Values.Count());
            Assert.Equal("Test/Path/A", array.Values.ElementAt(0).Value);
            Assert.Equal("Test/Path/C", array.Values.ElementAt(2).Value);
        }
        
        [Fact]
        public void CanParseComment()
        {
            const string input = "// My Comment";
            ConfigElement element = DocumentParser.ObjectElement.Parse(input);
            Assert.NotNull(element);
            Assert.IsType<CommentElement>(element);

            var comment = (CommentElement) element;
            Assert.Equal("// My Comment", comment.Comment);
        }
    }
}