using System.Linq;
using SolutionGenerator.Parsing;
using SolutionGenerator.Parsing.Model;
using Sprache;
using Xunit;

namespace SolutionGenerator.Tests.Parsing
{
    public class ParseDocumentTests
    {
        [Fact]
        public void ParseSingleEmptyObject()
        {
            const string input = "myType MyObject {}";
            ConfigDocument doc = DocumentParser.Document.Parse(input);
            Assert.Single(doc.RootElements);
            ObjectElement element = doc.RootElements.FirstOrDefault();
            Assert.NotNull(element);
            Assert.IsType<ConfigObject>(element);
            
            var obj = (ConfigObject) element;
            Assert.Equal("myType", obj.Heading.Type);
            Assert.Equal("MyObject", obj.Heading.Name);
        }

        [Fact]
        public void ParseManyEmptyObjects()
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
                ObjectElement element = doc.RootElements.ElementAtOrDefault(i - 1);
                Assert.NotNull(element);
                Assert.IsType<ConfigObject>(element);

                var obj = (ConfigObject) element;
                Assert.Equal("myType", obj.Heading.Type);
                Assert.Equal(expectedName, obj.Heading.Name);
            }
        }
    }
}