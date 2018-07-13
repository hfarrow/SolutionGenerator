using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using Sprache;
using Xunit;

namespace SolutionGen.Tests.Parsing
{
    // ReSharper disable once ClassNeverInstantiated.Global
        // Created by XUnit at runtime
    public class ParseExampleTemplateFixture : IDisposable
    {
        public readonly string TemplateText;
        public readonly ConfigDocument Config;

        public ParseExampleTemplateFixture()
        {
            Assembly assembly = typeof(ParseExampleTemplateFixture).GetTypeInfo().Assembly;

            using (Stream stream =
                assembly.GetManifestResourceStream("SolutionGenerator.Tests.Resources.TestTemplate.txt"))
            {
                using (var reader = new StreamReader(stream))
                {
                    TemplateText = reader.ReadToEnd();
                }
            }

            IResult<ConfigDocument> result = DocumentParser.Document.TryParse(TemplateText);
            if (!result.WasSuccessful)
            {
                Debug.WriteLine("Failed to parse example template config: " + result);
            }
            else
            {
                Config = result.Value;
            }
        }

        public void Dispose()
        {
        }
    }

    public class ParseExampleTemplateTests : IClassFixture<ParseExampleTemplateFixture>
    {
        private readonly ParseExampleTemplateFixture fixture;

        public ParseExampleTemplateTests(ParseExampleTemplateFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void CanLoadTemplateFileForTests()
        {
            Assert.NotNull(fixture.TemplateText);
            Assert.NotEmpty(fixture.TemplateText);
            Assert.NotNull(fixture.Config);
        }

        [Fact]
        public void OrderOfElementsAndValuesMatchFile()
        {
            Action<ConfigElement>[] validators =
            {
                ValidateComment("// Test Comment"),
                ValidateConfigObject("template", "TestTemplate", null, false),
                ValidateSimpleCommand("project", "true", "$(MODULE_NAME) : project"),
                ValidateSimpleCommand("project", "true", "$(MODULE_NAME).Tests : project.tests"),
                ValidateConfigObject("settings", "project", null, false),
                ValidateProperty(PropertyAction.Set, "include files", "true", ValidatePropertyValue("*.{cs,txt,json,xml,md}")),
                ValidateProperty(PropertyAction.Set, "exclude files", "true", ValidatePropertyValue("**/Tests/")),
                ValidateProperty(PropertyAction.Set, "target framework", "true", ValidatePropertyValue("v4.6")),
                ValidateProperty(PropertyAction.Set, "language version", "true", ValidatePropertyValue("6")),
                ValidateProperty(PropertyAction.Add, "lib refs", "true", ValidatePropertyArrayValues(new[]{"Lib1.dll", "Lib2.dll"})),
                ValidateProperty(PropertyAction.Add, "define constants", "true", ValidatePropertyArrayValues(new[]{"DEFINE_A", "DEFINE_B"})),
                ValidateProperty(PropertyAction.Add, "define constants", "debug", ValidatePropertyArrayValues(new[]{"DEBUG", "TRACE"})),
                ValidateProperty(PropertyAction.Add, "define constants", "release", ValidatePropertyArrayValues(new[]{"RELEASE"})),
                ValidateConfigObject("settings", "project.tests", "project", false),
                ValidateSimpleCommand("exclude", "no-tests", string.Empty),
                ValidateSimpleCommand("skip", "!test", string.Empty),
                ValidateProperty(PropertyAction.Set, "include files", "true", ValidatePropertyValue("**/Tests/**/*.{cs,txt,json,xml,md}")),
                ValidateProperty(PropertyAction.Set, "exclude files", "true", ValidatePropertyValue("empty")),
                ValidateProperty(PropertyAction.Add, "project refs", "true", ValidatePropertyArrayValues(new[]{"$(MODULE_NAME)"})),
            };

            ConfigElement[] allElements = fixture.Config.EnumerateRecursively().ToArray();

            Dictionary<ConfigElement, Action<ConfigElement>> zipped = 
                allElements.Zip(validators, (k, v) => new {k, v})
                .ToDictionary(x => x.k, x => x.v);
            
            Assert.Equal(validators.Length, zipped.Count);

            foreach (KeyValuePair<ConfigElement, Action<ConfigElement>> pair in zipped)
            {
                ConfigElement element = pair.Key;
                Action<ConfigElement> validator = pair.Value;
                Assert.NotNull(element);
                Assert.NotNull(validator);
                Debug.WriteLine("Validate Element: {0}", element);
                validator(element);
            }
        }

        private static Action<ConfigElement> ValidateComment(string value)
        {
            return (element) =>
            {
                Assert.IsType<CommentElement>(element);
                var comment = (CommentElement) element;
                Assert.Equal(value, comment.Comment);
            };
        }

        private static Action<ConfigElement> ValidateConfigObject(string type, string name, string inherits, bool isEmpty)
        {
            return (element) =>
            {
                Assert.IsType<ObjectElement>(element);
                var obj = (ObjectElement) element;
                Assert.Equal(type, obj.ElementHeading.Type);
                Assert.Equal(name, obj.ElementHeading.Name);
                Assert.Equal(inherits, obj.ElementHeading.InheritedObjectName);
                if (isEmpty)
                {
                    Assert.False(obj.Elements.Any());
                }
                else
                {
                    Assert.True(obj.Elements.Any());
                }
            };
        }
        
        private static Action<ConfigElement> ValidateProperty(PropertyAction action, string fullName,
            string conditionalExpr, Action<ValueElement> valueValidator)
        {
            return (element) =>
            {
                Assert.IsType<PropertyElement>(element);
                var property = (PropertyElement) element;
                Assert.Equal(action, property.Action);
                Assert.Equal(fullName, property.FullName);
                Assert.Equal(conditionalExpr, property.ConditionalExpression);
                
                Debug.WriteLine("Validate Property Value: {0}", property.ValueElement);
                valueValidator?.Invoke(property.ValueElement);
            };
        }

        private static Action<ValueElement> ValidatePropertyValue(string value)
        {
            return (element) =>
            {
                Assert.IsAssignableFrom<ValueElement>(element);
                Assert.Equal(value, element.Value);
            };
        }
        
        private static Action<ValueElement> ValidatePropertyArrayValues(string[] expectedValues)
        {
            return (element) =>
            {
                Assert.IsType<ArrayValue>(element);
                var arrayElement = (ArrayValue) element;
                ValueElement[] values = arrayElement.Values.ToArray();
                Assert.Equal(expectedValues.Length, values.Length);
                for (int i = 0; i < values.Length; i++)
                {
                    Assert.Equal(expectedValues[i], values[i].Value);
                }
            };
        }
        
        private static Action<ConfigElement> ValidateSimpleCommand(string name, string conditionalExpr,
            string argumentStr)
        {
            return (element) =>
            {
                Assert.IsType<SimpleCommandElement>(element);
                var cmd = (SimpleCommandElement) element;
                Assert.Equal(name, cmd.CommandName);
                Assert.Equal(conditionalExpr, cmd.ConditionalExpression);
                Assert.Equal(argumentStr, cmd.ArgumentStr);
            };
        }
    }
}