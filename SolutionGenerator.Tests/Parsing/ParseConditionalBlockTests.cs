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

            GroupElement group = DocumentParser.ConditionalBlockElement.Parse(input);
            
            Assert.NotNull(group);
            Assert.Empty(group.Elements);
            Assert.Equal("true", group.ConditionalExpression);
        }
        
        [Fact]
        public void CanHaveWhiteSpaceAfterIfLiteral()
        {
            const string input =
                "if (true)\n" +
                "{\n" +
                "\n" +
                "}";

            GroupElement group = DocumentParser.ConditionalBlockElement.Parse(input);
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

            GroupElement group = DocumentParser.ConditionalBlockElement.Parse(input);
            
            Assert.Single(group.Elements);
            ConfigElement element = group.Elements.ElementAt(0);
            Assert.IsType<ObjectElement>(element);
            var obj = (ObjectElement) element;
            Assert.Equal("myType", obj.ElementHeading.Type);
            Assert.Equal("MyObject", obj.ElementHeading.Name);
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

            GroupElement block = DocumentParser.ConditionalBlockElement.Parse(input);
            
            Assert.Single(block.Elements);
            ConfigElement element = block.Elements.ElementAt(0);
            Assert.IsType<GroupElement>(element);
        }
    }
}