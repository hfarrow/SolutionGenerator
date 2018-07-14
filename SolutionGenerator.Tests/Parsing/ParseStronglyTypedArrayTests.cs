using System.Collections.Generic;
using System.Linq;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParseStronglyTypedArrayTests
    {
        [Fact]
        public void CanParseBasePropertyValue()
        {
            IEnumerable<ValueElement> values = DocumentParser.StronglyTypedArray(DocumentParser.Value)
                .Parse("[myValue]");
            
            IEnumerable<ValueElement> propertyValues = values as ValueElement[] ?? values.ToArray();
            Assert.Single(propertyValues);
            Assert.Equal("myValue", propertyValues.First().Value);
        }
    }
}