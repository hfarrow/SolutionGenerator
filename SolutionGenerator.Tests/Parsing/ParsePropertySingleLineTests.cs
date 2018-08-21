using SolutionGen.Parser.Model;
using SolutionGen.Parser;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParsePropertySingleLineTests
    {
        private readonly BooleanExpressionParser parser = new BooleanExpressionParser();
        
        [Theory]
        [InlineData("templates = glob \"**/*.txt\"\n")]
        [InlineData("templates += glob \"**/*.txt\"")]
        public void ParseValidSingleWordSingleLineProperty(string input)
        {
            PropertyElement p = DocumentParser.PropertySingleLine.Parse(input);
            Assert.NotEqual(PropertyAction.Invalid, p.Action);
            Assert.Equal("templates", p.FullName);
            Assert.Equal("**/*.txt", ((GlobValue) p.ValueElement).GlobStr);
            Assert.True(parser.InvokeExpression(p.ConditionalExpression));
        }
        
        [Theory]
        [InlineData("include files = glob \"**/*.txt\"\n")]
        [InlineData("include files += glob \"**/*.txt\"")]
        public void ParseValidManyWordSingleLineProperty(string input)
        {
            PropertyElement p = DocumentParser.PropertySingleLine.Parse(input);
            Assert.NotEqual(PropertyAction.Invalid, p.Action);
            Assert.Equal("include files", p.FullName);
            Assert.Equal("**/*.txt", ((GlobValue) p.ValueElement).GlobStr);
            Assert.True(parser.InvokeExpression(p.ConditionalExpression));
        }

        [Fact]
        public void ColonDelimiterCanBeSurrounedByWhiteSpace()
        {
            PropertyElement p = DocumentParser.PropertySingleLine.Parse("include files = value");
        }

        [Fact]
        public void ParseSingleLinePropertyWithTrueConditional()
        {
            PropertyElement p = DocumentParser.PropertySingleLine.Parse("if (true) include files = value");
            Assert.True(parser.InvokeExpression(p.ConditionalExpression));
        }
        
        [Fact]
        public void ParseSingleLinePropertyWithFalseConditional()
        {
            PropertyElement p = DocumentParser.PropertySingleLine.Parse("if (false) include files = value");
            Assert.False(parser.InvokeExpression(p.ConditionalExpression));
        }
        
        [Fact]
        public void ParseSingleLinePropertyWithNestedParenConditional()
        {
            PropertyElement p =
                DocumentParser.PropertySingleLine.Parse("if (true&&(false||true)) include files = value");
            Assert.True(parser.InvokeExpression(p.ConditionalExpression));
        } 
        
        [Fact]
        public void XmlValueSingleLineCanBeParsed()
        {
            const string xmlData = "<node><value>v</value></node>";
            const string input = "xml = xml \"\"\"" + xmlData + "\"\"\"";
            PropertyElement propertyElement = DocumentParser.PropertySingleLine.Parse(input);
            Assert.Equal(XmlValue.FormatXml(xmlData), propertyElement.ValueElement.Value);
        }
    }
}