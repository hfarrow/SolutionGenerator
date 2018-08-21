using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParseXmlValueTests
    {
        [Fact]
        public void CanParseSingleLineXmlWithoutWhiteSpace()
        {
            const string xmlData = "<node>a value</node>";
            const string input = "xml\"\"\"" + xmlData + "\"\"\"";
            ValueElement result = BasicParser.XmlValue.Parse(input);
            Assert.Equal(xmlData, result.Value);
        }
        
        [Fact]
        public void CanParseSingleLineXmlWithWhiteSpace()
        {
            const string xmlData = "<node>a value</node>";
            const string input = "xml \"\"\"" + xmlData + "\"\"\"";
            ValueElement result = BasicParser.XmlValue.Parse(input);
            Assert.Equal(xmlData, result.Value);
        }

        [Fact]
        public void CanParseMultilineXml()
        {
            const string xmlData =
                "<node>\n" +
                "  a value\n" +
                "</node>";
            
            const string input =
                "xml\n" +
                "\"\"\"\n" +
                xmlData + "\n" +
                "\"\"\"";

            ValueElement result = BasicParser.XmlValue.Parse(input);
            Assert.Equal(xmlData, result.Value);
        }
    }
}