using System.Linq;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    public class ParsePropertyDictionaryTests
    {
        [Fact]
        public void EmptyDictionaryCanBeParsed()
        {
            const string input = "dictionary = { }";
            PropertyElement property = DocumentParser.PropertyDictionary.Parse(input);
            Assert.NotNull(property);
            Assert.Equal("dictionary", property.FullName);
            Assert.NotNull(property.ValueElement);
            Assert.IsType<ObjectElement>(property.ValueElement.Value);
            
            var obj = (ObjectElement) property.ValueElement.Value;
            Assert.Equal("<inline>", obj.ElementHeading.Type);
            Assert.Empty(obj.ElementHeading.Name);
            Assert.Null(obj.ElementHeading.InheritedObjectName);
            Assert.Empty(obj.Elements);
        }
        
        [Fact]
        public void EmptyNestedDictionaryCanBeParsed()
        {
            const string input =
                "dictionary =\n" +
                "{\n" +
                "    dictionary = { }\n" +
                "}";
            
            PropertyElement property = DocumentParser.PropertyDictionary.Parse(input);
            Assert.NotNull(property);
            Assert.Equal("dictionary", property.FullName);
            Assert.NotNull(property.ValueElement);
            Assert.IsType<ObjectElement>(property.ValueElement.Value);
            
            var obj = (ObjectElement) property.ValueElement.Value;
            Assert.Equal("<inline>", obj.ElementHeading.Type);
            Assert.Empty(obj.ElementHeading.Name);
            Assert.Null(obj.ElementHeading.InheritedObjectName);
            Assert.Equal(1, obj.Elements.Count());

            var innerProperty = obj.Elements.First() as PropertyElement;
            Assert.NotNull(innerProperty);
            Assert.NotNull(innerProperty);
            Assert.Equal("dictionary", property.FullName);
            Assert.NotNull(innerProperty.ValueElement);
            Assert.IsType<ObjectElement>(innerProperty.ValueElement.Value);
            
            var innerObj = (ObjectElement) innerProperty.ValueElement.Value;
            Assert.Equal("<inline>", innerObj.ElementHeading.Type);
            Assert.Empty(innerObj.ElementHeading.Name);
            Assert.Null(innerObj.ElementHeading.InheritedObjectName);
            Assert.Empty(innerObj.Elements);
        }
        
        [Fact]
        public void DictionaryWithValuesCanBeParsed()
        {
            const string input =
                "dictionary =\n" +
                "{\n" +
                "    property = value\n" +
                "}";
            
            PropertyElement property = DocumentParser.PropertyDictionary.Parse(input);
            Assert.NotNull(property);
            Assert.Equal("dictionary", property.FullName);
            Assert.NotNull(property.ValueElement);
            Assert.IsType<ObjectElement>(property.ValueElement.Value);
            
            var obj = (ObjectElement) property.ValueElement.Value;
            Assert.Equal("<inline>", obj.ElementHeading.Type);
            Assert.Empty(obj.ElementHeading.Name);
            Assert.Null(obj.ElementHeading.InheritedObjectName);
            Assert.Equal(1, obj.Elements.Count());

            var innerProperty = obj.Elements.First() as PropertyElement;
            Assert.NotNull(innerProperty);
            Assert.Equal("property", innerProperty.FullName);
            Assert.Equal("value", innerProperty.ValueElement.Value.ToString());
        }
        
        [Fact]
        public void DictionaryWithNestedValuesCanBeParsed()
        {
            const string input =
                "dictionary =\n" +
                "{\n" +
                "    dictionary =\n" +
                "    {\n" +
                "        property = value\n" +
                "    }\n" +
                "}";
            
            PropertyElement property = DocumentParser.PropertyDictionary.Parse(input);
            Assert.NotNull(property);
            Assert.Equal("dictionary", property.FullName);
            Assert.NotNull(property.ValueElement);
            Assert.IsType<ObjectElement>(property.ValueElement.Value);
            
            var obj = (ObjectElement) property.ValueElement.Value;
            Assert.Equal("<inline>", obj.ElementHeading.Type);
            Assert.Empty(obj.ElementHeading.Name);
            Assert.Null(obj.ElementHeading.InheritedObjectName);
            Assert.Equal(1, obj.Elements.Count());
            
            var innerProperty = obj.Elements.First() as PropertyElement;
            Assert.NotNull(innerProperty);
            Assert.NotNull(innerProperty);
            Assert.Equal("dictionary", property.FullName);
            Assert.NotNull(innerProperty.ValueElement);
            Assert.IsType<ObjectElement>(innerProperty.ValueElement.Value);
            
            var innerObj = (ObjectElement) innerProperty.ValueElement.Value;
            Assert.Equal("<inline>", innerObj.ElementHeading.Type);
            Assert.Empty(innerObj.ElementHeading.Name);
            Assert.Null(innerObj.ElementHeading.InheritedObjectName);
            Assert.Equal(1, innerObj.Elements.Count());

            var innerInnerProperty = innerObj.Elements.First() as PropertyElement;
            Assert.NotNull(innerInnerProperty);
            Assert.Equal("property", innerInnerProperty.FullName);
            Assert.Equal("value", innerInnerProperty.ValueElement.Value.ToString());
        }
    }
}