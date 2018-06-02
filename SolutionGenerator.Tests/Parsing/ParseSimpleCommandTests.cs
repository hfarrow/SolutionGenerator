using SolutionGen.Generator.Model;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParseSimpleCommandTests
    {
        [Fact]
        public void CanParseSimpleCommandWithoutConditional()
        {
            const string input = Settings.CMD_SKIP;
            CommandElement cmd = DocumentParser.SimpleCommand.Parse(input);
            Assert.NotNull(cmd);
            Assert.Equal(Settings.CMD_SKIP, cmd.CommandName);
            Assert.Equal("true", cmd.ConditionalExpression);
        }

        [Fact]
        public void CanParseSimpleCommandWithConditional()
        {
            string input = $"{Settings.CMD_SKIP} (my-define)";
            CommandElement cmd = DocumentParser.SimpleCommand.Parse(input);
            Assert.NotNull(cmd);
            Assert.Equal(Settings.CMD_SKIP, cmd.CommandName);
            Assert.Equal("my-define", cmd.ConditionalExpression);
        }
    }
}