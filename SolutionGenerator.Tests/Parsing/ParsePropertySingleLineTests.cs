using SolutionGen.Parser.Model;
using SolutionGen.Parser;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParsePropertySingleLineTests
    {
        private BooleanExpressionParser parser = new BooleanExpressionParser();
        
        [Theory]
        [InlineData("set templates: glob \"**/*.txt\"\n")]
        [InlineData("add templates: glob \"**/*.txt\"")]
        public void ParseValidSingleWordSingleLineProperty(string input)
        {
            PropertyElement p = DocumentParser.PropertySingleLine.Parse(input);
            Assert.NotEqual(PropertyAction.Invalid, p.Action);
            Assert.Equal("templates", p.FullName);
            Assert.Equal("**/*.txt", ((GlobValue) p.ValueElement).GlobStr);
            Assert.True(parser.InvokeExpression(p.ConditionalExpression));
        }
        
        [Theory]
        [InlineData("set include files: glob \"**/*.txt\"\n")]
        [InlineData("add include files: glob \"**/*.txt\"")]
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
            PropertyElement p = DocumentParser.PropertySingleLine.Parse("set include files : value");
        }

        [Fact]
        public void ParseSingleLinePropertyWithTrueConditional()
        {
            PropertyElement p = DocumentParser.PropertySingleLine.Parse("set include files (true): value");
            Assert.True(parser.InvokeExpression(p.ConditionalExpression));
        }
        
        [Fact]
        public void ParseSingleLinePropertyWithFalseConditional()
        {
            PropertyElement p = DocumentParser.PropertySingleLine.Parse("set include files (false): value");
            Assert.False(parser.InvokeExpression(p.ConditionalExpression));
        }
        
        [Fact]
        public void ParseSingleLinePropertyWithNestedParenConditional()
        {
            PropertyElement p = DocumentParser.PropertySingleLine.Parse("set include files (true&&(false||true)): value");
            Assert.True(parser.InvokeExpression(p.ConditionalExpression));
        }
    }
}