using System.Linq;
using SolutionGenerator.Parsing.Model;
using Sprache;
using Xunit;

namespace SolutionGenerator.Parsing.Tests
{
    public class ParsePropertyArrayTests
    {
        [Fact]
        public void MultiLineArrayCanBeParsed()
        {
            const string input =
                "add lib refs\n" +
                "[\n" +
                "    Test/Path/A\n" +
                "    Test/Path/B\n" +
                "    Test/Path/C\n" +
                "]";

            PropertyElement propertyElement = DocumentParser.PropertyArray.Parse(input);
            var array = propertyElement.Value as ArrayValue;
            Assert.NotNull(array);
            Assert.Equal(3, array.Values.Count());
            Assert.Equal("Test/Path/A", array.Values.ElementAt(0).Value);
            Assert.Equal("Test/Path/C", array.Values.ElementAt(2).Value);
        }

        [Fact]
        public void EmptySingleLineArrayCanBeParsed()
        {
            const string input = "add lib refs [ ]";
            
            PropertyElement propertyElement = DocumentParser.PropertyArray.Parse(input);
            var array = propertyElement.Value as ArrayValue;
            Assert.NotNull(array);
            Assert.Empty(array.Values);
        }
        
        [Fact]
        public void EmptyMultiLineArrayCanBeParsed()
        {
            const string input = 
                "add lib refs\n" +
                "[\n" +
                "]";
            
            PropertyElement propertyElement = DocumentParser.PropertyArray.Parse(input);
            var array = propertyElement.Value as ArrayValue;
            Assert.NotNull(array);
            Assert.Empty(array.Values);
        }
    }
}