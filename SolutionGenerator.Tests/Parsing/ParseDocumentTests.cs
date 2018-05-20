using System.Linq;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParseDocumentTests
    {
        [Fact]
        public void CanParseSingleEmptySingleLineObjectAtRoot()
        {
            const string input = "myType MyObject {}";
            ConfigDocument doc = DocumentParser.Document.Parse(input);
            Assert.Single(doc.RootElements);
            ConfigElement element = doc.RootElements.FirstOrDefault();
            Assert.NotNull(element);
            Assert.IsType<ObjectElement>(element);
            
            var obj = (ObjectElement) element;
            Assert.Equal("myType", obj.Heading.Type);
            Assert.Equal("MyObject", obj.Heading.Name);
        }

        [Fact]
        public void CanParseManyEmptySingleLineObjectsAtRoot()
        {
            const string input =
                "myType MyObject1 {}" +
                "myType MyObject2 {}" +
                "myType MyObject3 {}";

            ConfigDocument doc = DocumentParser.Document.Parse(input);

            Assert.Equal(3, doc.RootElements.Count());
            for (int i = 1; i <= 3; i++)
            {
                string expectedName = $"MyObject{i}";
                ConfigElement element = doc.RootElements.ElementAtOrDefault(i - 1);
                Assert.NotNull(element);
                Assert.IsType<ObjectElement>(element);

                var obj = (ObjectElement) element;
                Assert.Equal("myType", obj.Heading.Type);
                Assert.Equal(expectedName, obj.Heading.Name);
            }
        }
        
        [Fact]
        public void CanParseManyEmptySingleLineObjectsWithInheritanceAtRoot()
        {
            const string input =
                "myType MyObject1 : InheritedObject {}" +
                "myType MyObject2 : InheritedObject {}" +
                "myType MyObject3 : InheritedObject {}";

            ConfigDocument doc = DocumentParser.Document.Parse(input);

            Assert.Equal(3, doc.RootElements.Count());
            for (int i = 1; i <= 3; i++)
            {
                string expectedName = $"MyObject{i}";
                ConfigElement element = doc.RootElements.ElementAtOrDefault(i - 1);
                Assert.NotNull(element);
                Assert.IsType<ObjectElement>(element);

                var obj = (ObjectElement) element;
                Assert.Equal("myType", obj.Heading.Type);
                Assert.Equal(expectedName, obj.Heading.Name);
                Assert.Equal("InheritedObject", obj.Heading.InheritedObjectName);
            }
        }
        
        [Fact]
        public void CanParseManyEmptyMultiLineObjectsAtRoot()
        {
            const string input =
                "myType MyObject1\n" +
                "{\n" +
                "}" +
                "myType MyObject2\n" +
                "{\n" +
                "}";

            ConfigDocument doc = DocumentParser.Document.Parse(input);
            Assert.Equal(2, doc.RootElements.Count());
            
            for (int i = 1; i <= 2; i++)
            {
                string expectedName = $"MyObject{i}";
                ConfigElement element = doc.RootElements.ElementAtOrDefault(i - 1);
                Assert.NotNull(element);
                Assert.IsType<ObjectElement>(element);

                var obj = (ObjectElement) element;
                Assert.Equal("myType", obj.Heading.Type);
                Assert.Equal(expectedName, obj.Heading.Name);
            }
        }
        
        [Fact]
        public void CanParseManyEmptyMultiLineObjectsWithInheritanceAtRoot()
        {
            const string input =
                "myType MyObject1 : InheritedObject\n" +
                "{\n" +
                "}" +
                "myType MyObject2 : InheritedObject\n" +
                "{\n" +
                "}";

            ConfigDocument doc = DocumentParser.Document.Parse(input);
            Assert.Equal(2, doc.RootElements.Count());
            
            for (int i = 1; i <= 2; i++)
            {
                string expectedName = $"MyObject{i}";
                ConfigElement element = doc.RootElements.ElementAtOrDefault(i - 1);
                Assert.NotNull(element);
                Assert.IsType<ObjectElement>(element);

                var obj = (ObjectElement) element;
                Assert.Equal("myType", obj.Heading.Type);
                Assert.Equal(expectedName, obj.Heading.Name);
                Assert.Equal("InheritedObject", obj.Heading.InheritedObjectName);
            }
        }
    }
}