using Xunit;

namespace SolutionGenerator.Parsing.Tests
{
    public class ParseObjectTests
    {
        [Theory]
        [InlineData("myType MyObject{}")]
        [InlineData("myType MyObject{ }")]
        [InlineData("myType MyObject { }")]
        [InlineData("myType MyObject" +
                    "{ }")]
        [InlineData("myType MyObject" +
                    "{" +
                    "}")]
        [InlineData("myType MyObject" +
                    "{" +
                    "" +
                    "}")]
        public void ParseEmptyObjectWithoutInheritance(string input)
        {
        }
                
        [Theory]
        [InlineData("myType MyObject : MyOtherType{}")]
        [InlineData("myType MyObject : MyOtherType{ }")]
        [InlineData("myType MyObject : MyOtherType { }")]
        [InlineData("myType MyObject : MyOtherType" +
                    "{ }")]
        [InlineData("myType MyObject : MyOtherType" +
                    "{" +
                    "}")]
        [InlineData("myType MyObject : MyOtherType" +
                    "{" +
                    "" +
                    "}")]
        public void ParseEmptyObjectWithInheritance(string input)
        {
        }
    }
}