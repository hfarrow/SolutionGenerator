using SolutionGenerator.Parsing.Model;
using SolutionGenerator.Parsing;
using Sprache;
using Xunit;

namespace SolutionGenerator.Tests.Parsing
{
    public class ParseObjectHeadingTests
    {
        [Theory]
        [InlineData("myType MyObject:MyOtherObject")]
        [InlineData("myType MyObject: MyOtherObject")]
        [InlineData("myType MyObject :MyOtherObject")]
        [InlineData("myType MyObject : MyOtherObject")]
        public void HeadingWithInheritedObject(string input)
        {
            ConfigObjectHeading heading = DocumentParser.ObjectHeading.Parse(input);
            Assert.Equal("myType", heading.Type);
            Assert.Equal("MyObject", heading.Name);
            Assert.Equal("MyOtherObject", heading.InheritedObjectName);
        }

        [Fact]
        public void HeadingWithoutInheritedObject()
        {
            const string input = "myType MyObject";
            ConfigObjectHeading heading = DocumentParser.ObjectHeading.Parse(input);
            Assert.Equal("myType", heading.Type);
            Assert.Equal("MyObject", heading.Name);
            Assert.Null(heading.InheritedObjectName);
        }
    }
}