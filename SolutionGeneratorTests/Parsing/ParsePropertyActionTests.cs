﻿using SolutionGenerator.Parsing.Model;
using SolutionGenerator.Parsing;
using Sprache;
using Xunit;

namespace SolutionGenerator.Tests.Parsing
{
    public class ParsePropertyActionTests
    {
        [Theory]
        [InlineData("add")]
        [InlineData("Add")]
        [InlineData("ADD")]
        [InlineData("set")]
        [InlineData("Set")]
        [InlineData("SET")]
        public void ValidActionCanBeParsed(string input)
        {
            PropertyAction action = DocumentParser.PropertyAction.Parse(input);
            Assert.NotEqual(PropertyAction.Invalid, action);
            Assert.Equal(input, action.ToString(), ignoreCase: true);
        }
    }
}