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
            Assert.Equal("<inline>", obj.Heading.Type);
            Assert.Empty(obj.Heading.Name);
            Assert.Null(obj.Heading.InheritedObjectName);
            Assert.Empty(obj.Children);
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
            Assert.Equal("<inline>", obj.Heading.Type);
            Assert.Empty(obj.Heading.Name);
            Assert.Null(obj.Heading.InheritedObjectName);
            Assert.Single(obj.Children);

            var innerProperty = obj.Children.First() as PropertyElement;
            Assert.NotNull(innerProperty);
            Assert.NotNull(innerProperty);
            Assert.Equal("dictionary", property.FullName);
            Assert.NotNull(innerProperty.ValueElement);
            Assert.IsType<ObjectElement>(innerProperty.ValueElement.Value);
            
            var innerObj = (ObjectElement) innerProperty.ValueElement.Value;
            Assert.Equal("<inline>", innerObj.Heading.Type);
            Assert.Empty(innerObj.Heading.Name);
            Assert.Null(innerObj.Heading.InheritedObjectName);
            Assert.Empty(innerObj.Children);
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
            Assert.Equal("<inline>", obj.Heading.Type);
            Assert.Empty(obj.Heading.Name);
            Assert.Null(obj.Heading.InheritedObjectName);
            Assert.Single(obj.Children);

            var innerProperty = obj.Children.First() as PropertyElement;
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
            Assert.Equal("<inline>", obj.Heading.Type);
            Assert.Empty(obj.Heading.Name);
            Assert.Null(obj.Heading.InheritedObjectName);
            Assert.Single(obj.Children);
            
            var innerProperty = obj.Children.First() as PropertyElement;
            Assert.NotNull(innerProperty);
            Assert.NotNull(innerProperty);
            Assert.Equal("dictionary", property.FullName);
            Assert.NotNull(innerProperty.ValueElement);
            Assert.IsType<ObjectElement>(innerProperty.ValueElement.Value);
            
            var innerObj = (ObjectElement) innerProperty.ValueElement.Value;
            Assert.Equal("<inline>", innerObj.Heading.Type);
            Assert.Empty(innerObj.Heading.Name);
            Assert.Null(innerObj.Heading.InheritedObjectName);
            Assert.Single(innerObj.Children);

            var innerInnerProperty = innerObj.Children.First() as PropertyElement;
            Assert.NotNull(innerInnerProperty);
            Assert.Equal("property", innerInnerProperty.FullName);
            Assert.Equal("value", innerInnerProperty.ValueElement.Value.ToString());
        }
    }
}