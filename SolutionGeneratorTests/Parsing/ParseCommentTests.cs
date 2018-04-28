using SolutionGenerator.Parsing;
using Sprache;
using Xunit;

namespace SolutionGenerator.Parsing.Tests
{
    public class ParseCommentTests
    {
        [Fact]
        public void ParseCommentToEndOfLineAtEndOfDocument()
        {
            const string input = "// My Comment";
            string comment = BasicParser.CommentSingleLine.Parse(input);
            Assert.Equal(input, comment);
        }
        
        [Fact]
        public void ParseCommentToEndOfLineNotAtEndOfDocument()
        {
            const string input = "// My Comment\nnext line";
            string comment = BasicParser.CommentSingleLine.Parse(input);
            Assert.Equal("// My Comment", comment);
        }
    }
}