using SolutionGen.Utils;
using Xunit;

namespace SolutionGen.Tests.Utils
{
    public class ExpandableVarTests
    {
        [Fact]
        public void CanExpandInputWithNoOccurences()
        {
            const string input = "text with no occurences";
            string result = ExpandableVar.ReplaceOccurences("VAR", "EXP", input);
            Assert.Equal(input, result);
        }
        
        [Fact]
        public void CanExpandInputWithSingleOccurence()
        {
            const string input = "text with $(VAR) occurence";
            string result = ExpandableVar.ReplaceOccurences("VAR", "EXP", input);
            Assert.Equal(input.Replace("$(VAR)", "EXP"), result);
        }
        
        [Fact]
        public void CanExpandInputWithManyOccurences()
        {
            const string input = "text with $(VAR) - ${VAR) occurences";
            string result = ExpandableVar.ReplaceOccurences("VAR", "EXP", input);
            Assert.Equal(input.Replace("$(VAR)", "EXP"), result);
        }

        [Fact]
        public void CannotExpandInputWithEscapedOccurence()
        {
            const string input = "text with escaped \\$(VAR) occurence";
            string result = ExpandableVar.ReplaceOccurences("VAR", "EXP", input);
            Assert.DoesNotContain("EXP", result);
        }
        
        [Fact]
        public void CanStripEscapedVariables()
        {
            const string input = "text with escaped \\$(VAR) occurence";
            bool didStrip = ExpandableVar.StripEscapedVariablesInCopy(input, out object result);
            Assert.True(didStrip);
            Assert.DoesNotContain("\\$", result.ToString());
        }
    }
}