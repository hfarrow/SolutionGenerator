using System.Linq;
using SolutionGenerator.Parsing;
using SolutionGenerator.Parsing.Model;
using Sprache;
using Xunit;

namespace SolutionGenerator.Tests.Parsing
{
    public class ParseCommentTests
    {
        [Fact]
        public void CanParseCommentToEndOfLineAtEndOfDocument()
        {
            const string input = "// My Comment";
            string comment = BasicParser.CommentSingleLine.Parse(input);
            Assert.Equal(input, comment);
        }
        
        [Fact]
        public void CanParseCommentToEndOfLineNotAtEndOfDocument()
        {
            const string input = "// My Comment\nnext line";
            string comment = BasicParser.CommentSingleLine.Parse(input);
            Assert.Equal("// My Comment", comment);
        }

        [Fact]
        public void CanParseMultiLineComment()
        {
            const string input = "// My Comment\n// My Other Comment";
            string[] comments = BasicParser.CommentSingleLine.Many().Parse(input).ToArray();
            Assert.Equal(2, comments.Length);
            Assert.Equal("// My Comment", comments[0]);
            Assert.Equal("// My Other Comment", comments[1]);
        }
    }
}