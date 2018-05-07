using System.Collections.Generic;
using System.Linq;
using SolutionGen.Parsing;
using SolutionGen.Parsing.Model;
using Sprache;
using Xunit;
using KeyValuePair = SolutionGen.Parsing.Model.KeyValuePair;

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

        [Fact]
        public void CanParseKeyValuePair()
        {
            IEnumerable<KeyValuePair> pairs = DocumentParser.StronglyTypedArray(DocumentParser.PairValue)
                .Parse("[myKey:myValue]");

            IEnumerable<KeyValuePair> pairValues = pairs as KeyValuePair[] ?? pairs.ToArray();
            Assert.Single(pairValues);
            KeyValuePair pair = pairValues.FirstOrDefault();
            Assert.NotNull(pair);
            Assert.Equal("myKey", pair.PairKey);
            Assert.Equal("myValue", pair.PairValue.Value);
        }
    }
}