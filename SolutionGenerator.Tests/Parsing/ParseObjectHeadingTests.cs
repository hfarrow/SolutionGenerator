using SolutionGen.Parser.Model;
using SolutionGen.Parser;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
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
            ObjectElementHeading elementHeading = DocumentParser.ObjectHeading.Parse(input);
            Assert.Equal("myType", elementHeading.Type);
            Assert.Equal("MyObject", elementHeading.Name);
            Assert.Equal("MyOtherObject", elementHeading.InheritedObjectName);
        }

        [Fact]
        public void HeadingWithoutInheritedObject()
        {
            const string input = "myType MyObject";
            ObjectElementHeading elementHeading = DocumentParser.ObjectHeading.Parse(input);
            Assert.Equal("myType", elementHeading.Type);
            Assert.Equal("MyObject", elementHeading.Name);
            Assert.Null(elementHeading.InheritedObjectName);
        }
    }
}