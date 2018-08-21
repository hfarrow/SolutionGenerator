using System.Linq;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParsePropertyArrayTests
    {
        [Fact]
        public void SingleLineArrayCanBeParsed()
        {
            const string input =
                "lib refs += [Test/Path/A, Test/Path/B, Test/Path/C]";

            PropertyElement propertyElement = DocumentParser.PropertyArray.Parse(input);
            var array = propertyElement.ValueElement as ArrayValue;
            Assert.NotNull(array);
            Assert.Equal(3, array.Values.Count());
            Assert.Equal("Test/Path/A", array.Values.ElementAt(0).Value);
            Assert.Equal("Test/Path/C", array.Values.ElementAt(2).Value);
        }
        
        [Fact]
        public void MultiLineArrayCanBeParsed()
        {
            const string input =
                "lib refs +=\n" +
                "[\n" +
                "    Test/Path/A,\n" +
                "    Test/Path/B,\n" +
                "    Test/Path/C\n" +
                "]";

            PropertyElement propertyElement = DocumentParser.PropertyArray.Parse(input);
            var array = propertyElement.ValueElement as ArrayValue;
            Assert.NotNull(array);
            Assert.Equal(3, array.Values.Count());
            Assert.Equal("Test/Path/A", array.Values.ElementAt(0).Value);
            Assert.Equal("Test/Path/C", array.Values.ElementAt(2).Value);
        }

        [Fact]
        public void TrailingCommaAfterLastElementSingleLineCanBeParsed()
        {
            const string input =
                "lib refs += [Test/Path/A, Test/Path/B,]";
            
            PropertyElement propertyElement = DocumentParser.PropertyArray.Parse(input);
            var array = propertyElement.ValueElement as ArrayValue;
            Assert.NotNull(array);
            Assert.Equal(2, array.Values.Count());
            Assert.Equal("Test/Path/A", array.Values.First().Value);
        }
        
        [Fact]
        public void TrailingCommaAfterLastElementMultiLineCanBeParsed()
        {
            const string input =
                "lib refs +=\n" +
                "[\n" +
                "    Test/Path/A,\n" +
                "    Test/Path/B,\n" +
                "]";
            
            PropertyElement propertyElement = DocumentParser.PropertyArray.Parse(input);
            var array = propertyElement.ValueElement as ArrayValue;
            Assert.NotNull(array);
            Assert.Equal(2, array.Values.Count());
            Assert.Equal("Test/Path/A", array.Values.First().Value);
        }

        [Fact]
        public void EmptySingleLineArrayCanBeParsed()
        {
            const string input = "lib refs += [ ]";

            PropertyElement propertyElement = DocumentParser.PropertyArray.Parse(input);
            var array = propertyElement.ValueElement as ArrayValue;
            Assert.NotNull(array);
            Assert.Empty(array.Values);
        }
        
        [Fact]
        public void EmptyMultiLineArrayCanBeParsed()
        {
            const string input = 
                "lib refs +=\n" +
                "[\n" +
                "]";
            
            PropertyElement propertyElement = DocumentParser.PropertyArray.Parse(input);
            var array = propertyElement.ValueElement as ArrayValue;
            Assert.NotNull(array);
            Assert.Empty(array.Values);
        }
       
        [Fact]
        public void XmlValueSingleLineArrayCanBeParsed()
        {
            const string xmlData = "<node><value>v</value></node>";
            const string input =
                "xml =\n" +
                "[\n" +
                "  xml \"\"\"" + xmlData + "\"\"\"\n" +
                "]";
            
            PropertyElement propertyElement = DocumentParser.PropertyArray.Parse(input);
            var array = propertyElement.ValueElement as ArrayValue;
            Assert.NotNull(array);
            Assert.Single(array.Values);
            Assert.Equal(XmlValue.FormatXml(xmlData), array.Values.First().Value.ToString());
        }
        
        [Fact]
        public void XmlValueMultiLineArrayCanBeParsed()
        {
            const string xmlData = "<node><value>v</value></node>";
            const string input =
                "xml =\n" +
                "[\n" +
                "  xml \"\"\"" + xmlData + "\"\"\",\n" +
                "  xml \"\"\"" + xmlData + "\"\"\"\n" +
                "]";
            
            PropertyElement propertyElement = DocumentParser.PropertyArray.Parse(input);
            var array = propertyElement.ValueElement as ArrayValue;
            Assert.NotNull(array);
            Assert.Equal(2, array.Values.Count());
            Assert.Equal(XmlValue.FormatXml(xmlData), array.Values.First().Value.ToString());
        }
        
        [Fact]
        public void XmlValueMultiLineNodesInArrayCanBeParsed()
        {
            const string xmlData = 
                "  <node>\n" +
                "    <value>v</value>\n" +
                "  </node>";
            
            const string xmlDataExpected = 
                "<node>\n" +
                "    <value>v</value>\n" +
                "</node>";
            
            const string input =
                "xml +=\n" +
                "[\n" +
                "  xml\n" +
                "  \"\"\"\n" + xmlData + "\n\"\"\",\n" +
                "  xml\n" +
                "  \"\"\"\n" + xmlData + "\n\"\"\"\n" +
                "]";
            
            PropertyElement propertyElement = DocumentParser.PropertyArray.Parse(input);
            var array = propertyElement.ValueElement as ArrayValue;
            Assert.NotNull(array);
            Assert.Equal(2, array.Values.Count());
            Assert.Equal(xmlDataExpected, array.Values.First().Value);
        }
    }
}