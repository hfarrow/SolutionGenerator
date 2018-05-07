using SolutionGen.Parsing;
using SolutionGen.Parsing.Model;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParseSimpleCommandTests
    {
        [Fact]
        public void CanParseSimpleCommandWithoutConditional()
        {
            const string input = "skip";
            CommandElement cmd = DocumentParser.SimpleCommand.Parse(input);
            Assert.NotNull(cmd);
            Assert.Equal("skip", cmd.CommandName);
            Assert.Equal("true", cmd.ConditionalExpression);
        }

        [Fact]
        public void CanParseSimpleCommandWithConditional()
        {
            const string input = "skip (my-define)";
            CommandElement cmd = DocumentParser.SimpleCommand.Parse(input);
            Assert.NotNull(cmd);
            Assert.Equal("skip", cmd.CommandName);
            Assert.Equal("my-define", cmd.ConditionalExpression);
        }
    }
}