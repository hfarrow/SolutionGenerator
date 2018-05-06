using System;
using SolutionGenerator.Parsing;
using Sprache;
using Xunit;

namespace SolutionGenerator.Tests.Parsing
{
    public class ParseIdentifierTests
    {
        [Theory]
        [InlineData("myID")]
        [InlineData("my-ID")]
        [InlineData("my_ID")]
        [InlineData("my.ID")]
        [InlineData("myID_")]
        [InlineData("myID-")]
        [InlineData("myID.")]
        [InlineData("my1ID")]
        [InlineData("myID1")]
        public void IdentifierIsValid(string input)
        {
            string id = BasicParser.IdentifierToken.Parse(input);
            Assert.Equal(input, id);
        }
        
        [Theory]
        [InlineData("1myID")]
        [InlineData("_myID")]
        [InlineData("-myID")]
        [InlineData("@myID")]
        [InlineData("\"@myID")]
        public void IdentifierMustStartWithLetter(string input)
        {
            Exception ex = Record.Exception(() => BasicParser.IdentifierToken.Parse(input));
            Assert.NotNull(ex);
        }

        [Theory]
        [InlineData("my ID")]
        [InlineData("my'ID")]
        [InlineData("my\"ID")]
        [InlineData("my*ID")]
        [InlineData("my[ID")]
        [InlineData("my{ID")]
        public void IdentifierMustNotIncludeInvalidChar(string input)
        {
            string id = BasicParser.IdentifierToken.Parse(input);
            Assert.Equal("my", id);
        }
        
        [Theory]
        [InlineData("$(VAR)")]
        [InlineData("$(VAR)ID")]
        [InlineData("$(VAR).ID")]
        [InlineData("ID$(VAR)")]
        [InlineData("ID.$(VAR)")]
        [InlineData("ID.$(VAR).ID")]
        [InlineData("$(VAR).ID.$(VAR)")]
        public void IdentifierCanIncludeVariableExpansion(string input)
        {
            string id = BasicParser.IdentifierToken.Parse(input);
            Assert.Equal(input, id);
        }
    }
}