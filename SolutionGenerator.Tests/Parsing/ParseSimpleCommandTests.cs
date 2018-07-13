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
        public void CanParseSimpleCommandWithoutConditionalWithoutArgs()
        {
            const string input = Settings.CMD_SKIP;
            SimpleCommandElement cmd = DocumentParser.SimpleCommand.Parse(input);
            Assert.NotNull(cmd);
            Assert.Equal(Settings.CMD_SKIP, cmd.CommandName);
            Assert.Equal("true", cmd.ConditionalExpression);
            Assert.Equal(string.Empty, cmd.ArgumentStr);
        }

        [Fact]
        public void CanParseSimpleCommandWithConditionalWithoutArgs()
        {
            string input = $"if (my-define) {Settings.CMD_SKIP}";
            SimpleCommandElement cmd = DocumentParser.SimpleCommand.Parse(input);
            Assert.NotNull(cmd);
            Assert.Equal(Settings.CMD_SKIP, cmd.CommandName);
            Assert.Equal("my-define", cmd.ConditionalExpression);
            Assert.Equal(string.Empty, cmd.ArgumentStr);
        }

        [Fact]
        public void CanParseSimpleCommandWithoutConditionalWithArgs()
        {
            const string commandArgs = "ProjectName : SettingsName";
            string input = $"{Settings.CMD_DECLARE_PROJECT} \"{commandArgs}\"";
            SimpleCommandElement cmd = DocumentParser.SimpleCommand.Parse(input);
            Assert.Equal(Settings.CMD_DECLARE_PROJECT, cmd.CommandName);
            Assert.Equal("true", cmd.ConditionalExpression);
            Assert.Equal(commandArgs, cmd.ArgumentStr);
        }
        
        [Fact]
        public void CanParseSimpleCommandWithConditionalWithArgs()
        {
            const string commandArgs = "ProjectName : SettingsName";
            string input = $"if (my-define) {Settings.CMD_DECLARE_PROJECT} \"{commandArgs}\"";
            SimpleCommandElement cmd = DocumentParser.SimpleCommand.Parse(input);
            Assert.Equal(Settings.CMD_DECLARE_PROJECT, cmd.CommandName);
            Assert.Equal("my-define", cmd.ConditionalExpression);
            Assert.Equal(commandArgs, cmd.ArgumentStr);
        }
    }
}