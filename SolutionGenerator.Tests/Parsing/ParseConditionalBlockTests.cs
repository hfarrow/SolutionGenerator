using System.Linq;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParseConditionalBlockTests
    {
        [Fact]
        public void BlockCanBeEmpty()
        {
            const string input =
                "if(true)\n" +
                "{\n" +
                "\n" +
                "}";

            BlockElement block = DocumentParser.ConditionalBlockElement.Parse(input);
            
            Assert.NotNull(block);
            Assert.Empty(block.Children);
            Assert.Equal("true", block.ConditionalExpression);
        }
        
        [Fact]
        public void CanHaveWhiteSpaceAfterIfLiteral()
        {
            const string input =
                "if (true)\n" +
                "{\n" +
                "\n" +
                "}";

            BlockElement block = DocumentParser.ConditionalBlockElement.Parse(input);
        }
        
        [Fact]
        public void BlockCanContainObject()
        {
            const string input =
                "if(true)\n" +
                "{\n" +
                "  myType MyObject\n" +
                "  {\n" +
                "  }\n" +
                "}";

            BlockElement block = DocumentParser.ConditionalBlockElement.Parse(input);
            
            Assert.Single(block.Children);
            ConfigElement element = block.Children.ElementAt(0);
            Assert.IsType<ObjectElement>(element);
            var obj = (ObjectElement) element;
            Assert.Equal("myType", obj.Heading.Type);
            Assert.Equal("MyObject", obj.Heading.Name);
        }
        
        [Fact]
        public void BlockCanContainNestedConditionalBlock()
        {
            const string input =
                "if(true)\n" +
                "{\n" +
                "  if(true)\n" +
                "  {\n" +
                "  }\n" +
                "}";

            BlockElement block = DocumentParser.ConditionalBlockElement.Parse(input);
            
            Assert.Single(block.Children);
            ConfigElement element = block.Children.ElementAt(0);
            Assert.IsType<BlockElement>(element);
        }
    }
}