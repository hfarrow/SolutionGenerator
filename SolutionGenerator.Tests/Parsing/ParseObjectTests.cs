using System.Linq;
using SolutionGen.Parsing;
using SolutionGen.Parsing.Model;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
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
            ObjectElement obj = DocumentParser.Object.Parse(input);
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
            ObjectElement obj = DocumentParser.Object.Parse(input);
            Assert.NotNull(obj);
            Assert.Equal("myType", obj.Heading.Type);
            Assert.Equal("MyObject", obj.Heading.Name);
            Assert.Equal("MyOtherType", obj.Heading.InheritedObjectName);
        }
        
        [Fact]
        public void CanParseNestedObjectWithoutInheritance()
        {
            const string input =
                "myType MyObject\n" +
                "{\n" +
                "    myNestedType MyNestedObject\n" +
                "    {\n" +
                "        set include paths: \"./\"\n" +
                "    }\n" +
                "}";

            ObjectElement obj = DocumentParser.Object.Parse(input);
            Assert.NotNull(obj);
            Assert.Single(obj.Elements);

            ConfigElement element = obj.Elements.FirstOrDefault();
            Assert.NotNull(element);
            Assert.IsType<ObjectElement>(element);

            var nestedObj = (ObjectElement) element;
            Assert.Equal("myNestedType", nestedObj.Heading.Type);
            Assert.Equal("MyNestedObject", nestedObj.Heading.Name);
            Assert.Single(nestedObj.Elements);
            
        }
        
        [Fact]
        public void CanParseNestedObjectWithInheritance()
        {
            const string input =
                "myType MyObject : InheritedObject\n" +
                "{\n" +
                "    myNestedType MyNestedObject : InheritedObject\n" +
                "    {\n" +
                "        set include paths: \"./\"\n" +
                "    }\n" +
                "}";

            ObjectElement obj = DocumentParser.Object.Parse(input);
            Assert.NotNull(obj);
            Assert.Single(obj.Elements);

            ConfigElement element = obj.Elements.FirstOrDefault();
            Assert.NotNull(element);
            Assert.IsType<ObjectElement>(element);

            var nestedObj = (ObjectElement) element;
            Assert.Equal("myNestedType", nestedObj.Heading.Type);
            Assert.Equal("MyNestedObject", nestedObj.Heading.Name);
            Assert.Single(nestedObj.Elements);
            
        }

        [Fact]
        public void CanParseNestedChildrenWithInheritance()
        {
            const string input =
                "myType MyObject : InheritedObject\n" +
                "{\n" +
                "    myNestedType My.NestedObject1 : InheritedObject\n" +
                "    {\n" +
                "    }\n" +
                "    myNestedType My.NestedObject2 : InheritedObject\n" +
                "    {\n" +
                "    }\n" +
                "}";

            ObjectElement root = DocumentParser.Object.Parse(input);
            Assert.NotNull(root);
            Assert.Equal(2, root.Elements.Count());
            
            for (int i = 1; i <= 2; i++)
            {
                string expectedName = $"My.NestedObject{i}";
                ConfigElement element = root.Elements.ElementAtOrDefault(i - 1);
                Assert.NotNull(element);
                Assert.IsType<ObjectElement>(element);

                var obj = (ObjectElement) element;
                Assert.Equal("myNestedType", obj.Heading.Type);
                Assert.Equal(expectedName, obj.Heading.Name);
                Assert.Equal("InheritedObject", obj.Heading.InheritedObjectName);
            }
        }
        
        [Fact]
        public void CanParseObjectWhereNameStartsWithPropertyAction()
        {
            // "settings" should not be interpreted as the start of a property.
            const string input =
                "settings MyObject : InheritedObject\n" +
                "{\n" +
                "    settings MyNestedObject : InheritedObject\n" +
                "    {\n" +
                "    }\n" +
                "}";

            ObjectElement root = DocumentParser.Object.Parse(input);
            Assert.NotNull(root);
        }

        [Fact]
        public void CanParseObjectWithSimpleCommands()
        {
            const string input =
                "myType MyObject : InheritedObject\n" +
                "{\n" +
                "    exclude (no-tests)\n" +
                "    skip (!test)\n" +
                "}";

            string[] expectedNames =
            {
                "exclude", "skip"
            };
            
            ObjectElement obj = DocumentParser.Object.Parse(input);
            Assert.NotNull(obj);
            Assert.Equal(2, obj.Elements.Count());
            for (int i = 0; i < 2; i++)
            {
                var cmd = obj.Elements.ElementAt(i) as CommandElement;
                Assert.NotNull(cmd);
                Assert.Equal(expectedNames[i], cmd.CommandName);
            }
        }
    }
}