using System.Linq;
using SolutionGenerator.Parsing;
using SolutionGenerator.Parsing.Model;
using Sprache;
using Xunit;

namespace SolutionGenerator.Tests.Parsing
{
    public class ParseObjectTests
    {
        [Theory]
        [InlineData("myType MyObject{}")]
        [InlineData("myType MyObject{ }")]
        [InlineData("myType MyObject { }")]
        [InlineData("myType MyObject\n" +
                    "{ }")]
        [InlineData("myType MyObject\n" +
                    "{\n" +
                    "}")]
        [InlineData("myType MyObject\n" +
                    "{\n" +
                    "\n" +
                    "}")]
        public void CanParseEmptyObjectWithoutInheritance(string input)
        {
            ConfigObject obj = DocumentParser.Object.Parse(input);
            Assert.NotNull(obj);
            Assert.Equal("myType", obj.Heading.Type);
            Assert.Equal("MyObject", obj.Heading.Name);
            Assert.Null(obj.Heading.InheritedObjectName);
        }
                
        [Theory]
        [InlineData("myType MyObject : MyOtherType{}")]
        [InlineData("myType MyObject : MyOtherType{ }")]
        [InlineData("myType MyObject : MyOtherType { }")]
        [InlineData("myType MyObject : MyOtherType\n" +
                    "{ }")]
        [InlineData("myType MyObject : MyOtherType\n" +
                    "{\n" +
                    "}")]
        [InlineData("myType MyObject : MyOtherType\n" +
                    "{\n" +
                    "\n" +
                    "}")]
        public void CanParseEmptyObjectWithInheritance(string input)
        {
            ConfigObject obj = DocumentParser.Object.Parse(input);
            Assert.NotNull(obj);
            Assert.Equal("myType", obj.Heading.Type);
            Assert.Equal("MyObject", obj.Heading.Name);
            Assert.Equal("MyOtherType", obj.Heading.InheritedObjectName);
        }
        
        [Fact]
        public void CanParseNestedObject()
        {
            const string input =
                "myType MyObject\n" +
                "{\n" +
                "    myNestedType MyNestedObject\n" +
                "    {\n" +
                "    }\n" +
                "}";

            ConfigObject obj = DocumentParser.Object.Parse(input);
            Assert.NotNull(obj);
            Assert.Single(obj.Elements);

            ObjectElement element = obj.Elements.FirstOrDefault();
            Assert.NotNull(element);
            Assert.IsType<ConfigObject>(element);

            var nestedObj = (ConfigObject) element;
            Assert.Equal("myNestedType", nestedObj.Heading.Type);
            Assert.Equal("MyNestedObject", nestedObj.Heading.Name);
            Assert.Empty(nestedObj.Elements);
            
        }
    }
}