using SolutionGen.Generator.ModelOld;
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
            const string input = Settings.PROP_SKIP;
            CommandElement cmd = DocumentParser.SimpleCommand.Parse(input);
            Assert.NotNull(cmd);
            Assert.Equal(Settings.PROP_SKIP, cmd.CommandName);
            Assert.Equal("true", cmd.ConditionalExpression);
        }

        [Fact]
        public void CanParseSimpleCommandWithConditional()
        {
            string input = $"{Settings.PROP_SKIP} (my-define)";
            CommandElement cmd = DocumentParser.SimpleCommand.Parse(input);
            Assert.NotNull(cmd);
            Assert.Equal(Settings.PROP_SKIP, cmd.CommandName);
            Assert.Equal("my-define", cmd.ConditionalExpression);
        }
    }
}