using System.Linq;
using SolutionGenerator.Parsing.Model;
using Sprache;
using Xunit;

namespace SolutionGenerator.Parsing.Tests
{
    public class ParseDocumentTests
    {
        [Fact]
        public void ParseSingleEmptyObject()
        {
            const string input = "myType MyObject {}";
            ConfigDocument doc = DocumentParser.Document.Parse(input);
            Assert.Single(doc.Objects);
            Assert.True(doc.Objects.ContainsKey("MyObject"));
        }

        [Fact]
        public void ParseManyEmptyObjects()
        {
            const string input =
                "myType MyObject1 {}" +
                "myType MyObject2 {}" +
                "myType MyObject3 {}";

            ConfigDocument doc = DocumentParser.Document.Parse(input);

            Assert.Equal(3, doc.Objects.Count);
            for (int i = 1; i <= 3; i++)
            {
                string key = $"MyObject{i}";
                Assert.True(doc.Objects.ContainsKey(key));
                Assert.Equal(key, doc.Objects.Values.ElementAt(i-1).Heading.Name);
            }
        }
    }
}