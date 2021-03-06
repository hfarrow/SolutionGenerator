﻿using SolutionGen.Parser;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParseEnclosedTextTests
    {
        [Fact]
        public void SingleDepthEnclosedTextCanBeParsed()
        {
            const string input = "(test)";
            string result = BasicParser.EnclosedText('(', ')').Parse(input);
            Assert.Equal(input, result);
        }
        
        [Fact]
        public void EmptyEnclosedTextCanBeParsed()
        {
            const string input = "()";
            string result = BasicParser.EnclosedText('(', ')').Parse(input);
            Assert.Equal(input, result);
        }
        
        [Theory]
        [InlineData("(t(es)t)")]
        [InlineData("(t((e()s))t)")]
        [InlineData("((()()))")]
        public void NestedEnclosedTextCanBeParsed(string input)
        {
            string result = BasicParser.EnclosedText('(', ')').Parse(input);
            Assert.Equal(input, result);
        }

        [Fact]
        public void InputMustStartWithOpenChar()
        {
            const string input = "fail(test)";
            IResult<string> result = BasicParser.EnclosedText('(', ')').TryParse(input);
            Assert.False(result.WasSuccessful);
        }
        
        [Fact]
        public void InputMustHaveMatchingNumberOfClosingChars()
        {
            const string input = "(()";
            IResult<string> result = BasicParser.EnclosedText('(', ')').TryParse(input);
            Assert.False(result.WasSuccessful);
        }
    }
}