using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using SolutionGenerator.Parsing;
using SolutionGenerator.Parsing.Model;
using Sprache;
using Xunit;

namespace SolutionGenerator.Tests.Parsing
{
    // ReSharper disable once ClassNeverInstantiated.Global
    // Created by XUnit at runtime
    public class ExampleTemplateFixture : IDisposable
    {
        public readonly string TemplateText;
        public readonly ConfigDocument Config;

        public ExampleTemplateFixture()
        {
            Assembly assembly = typeof(ExampleTemplateFixture).GetTypeInfo().Assembly;

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

    public class ParseExampleTemplateTests : IClassFixture<ExampleTemplateFixture>
    {
        private readonly ExampleTemplateFixture fixture;

        public ParseExampleTemplateTests(ExampleTemplateFixture fixture)
        {
            this.fixture = fixture;
        }

        
        // TODO: Identifier must allow variable expansion -- $(MODULE_NAME).Tests OR Tests.$(MODULE_NAME)
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
            Action<ObjectElement>[] validators =
            {
                ValidateComment("// Test Comment"),
                ValidateConfigObject("template", "TestTemplate", null, false),
                ValidateConfiguration("everything", new Dictionary<string, HashSet<string>>
                {
                    ["Debug"] = new HashSet<string> {"debug", "test"},
                    ["Release"] = new HashSet<string> {"release", "test"},
                }),
                ValidateConfiguration("no-tests", new Dictionary<string, HashSet<string>>
                {
                    ["Debug"] = new HashSet<string> {"debug"},
                    ["Release"] = new HashSet<string> {"release"},
                }),
                ValidateProperty(PropertyAction.Add, "project $(MODULE_NAME)", "true", ValidatePropertyValue("project")),
                ValidateProperty(PropertyAction.Add, "project $(MODULE_NAME)", "true", ValidatePropertyValue("project.tests")),
                ValidateConfigObject("settings", "project", null, false),
                ValidateProperty(PropertyAction.Set, "include paths", "true", ValidatePropertyValue("./")),
                ValidateProperty(PropertyAction.Set, "exclude paths", "true", ValidatePropertyValue("**/Tests/")),
                ValidateProperty(PropertyAction.Set, "include files", "true", ValidatePropertyValue("*.{cs,txt,json,xml,md}")),
                ValidateProperty(PropertyAction.Set, "target framework", "true", ValidatePropertyValue("net4.6")),
                ValidateProperty(PropertyAction.Set, "language version", "true", ValidatePropertyValue("6")),
                ValidateProperty(PropertyAction.Add, "lib refs", "true", ValidatePropertyArrayValues(new[]{"Lib1.dll", "Lib2.dll"})),
                ValidateProperty(PropertyAction.Add, "define constants", "true", ValidatePropertyArrayValues(new[]{"DEFINE_A", "DEFINE_B"})),
                ValidateProperty(PropertyAction.Add, "define constants", "debug", ValidatePropertyArrayValues(new[]{"DEBUG", "TRACE"})),
                ValidateProperty(PropertyAction.Add, "define constants", "release", ValidatePropertyArrayValues(new[]{"RELEASE"})),
                ValidateConfigObject("settings", "project.tests", "project", false),
                ValidateSimpleCommand("exclude", "no-tests"),
                ValidateSimpleCommand("skip", "!test"),
                ValidateProperty(PropertyAction.Set, "include paths", "true", ValidatePropertyValue("**/Tests/")),
                ValidateProperty(PropertyAction.Set, "exclude paths", "true", ValidatePropertyValue("empty")),
                ValidateProperty(PropertyAction.Add, "project refs", "true", ValidatePropertyArrayValues(new[]{"$(MODULE_NAME)"})),
            };

            Dictionary<ObjectElement, Action<ObjectElement>> zipped = fixture.Config.EnumerateRecursively()
                .Zip(validators, (k, v) => new {k, v})
                .ToDictionary(x => x.k, x => x.v);
            
            Assert.Equal(validators.Length, zipped.Count);

            foreach (KeyValuePair<ObjectElement, Action<ObjectElement>> pair in zipped)
            {
                ObjectElement element = pair.Key;
                Action<ObjectElement> validator = pair.Value;
                Assert.NotNull(element);
                Assert.NotNull(validator);
                validator(element);
            }
        }

        private static Action<ObjectElement> ValidateComment(string value)
        {
            return (element) =>
            {
                Assert.IsType<CommentElement>(element);
                var comment = (CommentElement) element;
                Assert.Equal(value, comment.Comment);
            };
        }

        private static Action<ObjectElement> ValidateConfigObject(string type, string name, string inherits, bool isEmpty)
        {
            return (element) =>
            {
                Assert.IsType<ConfigObject>(element);
                var obj = (ConfigObject) element;
                Assert.Equal(type, obj.Heading.Type);
                Assert.Equal(name, obj.Heading.Name);
                Assert.Equal(inherits, obj.Heading.InheritedObjectName);
                Assert.True(isEmpty && !obj.Elements.Any());
            };
        }
        
        private static Action<ObjectElement> ValidateConfiguration(string name, Dictionary<string, HashSet<string>> configurations)
        {
            return (element) =>
            {
                Assert.IsType<ConfigurationElement>(element);
                var configuration = (ConfigurationElement) element;
                Assert.Equal(configurations, configuration.Configurations);
            };
        }
        
        private static Action<ObjectElement> ValidateProperty(PropertyAction action, string fullName,
            string conditionalExpr, Action<ValueElement> valueValidator)
        {
            return (element) =>
            {
                Assert.IsType<PropertyElement>(element);
                var property = (PropertyElement) element;
                Assert.Equal(action, property.Action);
                Assert.Equal(fullName, property.FullName);
                Assert.Equal(conditionalExpr, property.ConditionalExpression);
                
                valueValidator?.Invoke(property.Value);
            };
        }

        private static Action<ValueElement> ValidatePropertyValue(string value)
        {
            return (element) =>
            {
                Assert.IsType<ValueElement>(element);
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
        
        private static Action<ObjectElement> ValidateSimpleCommand(string name, string conditionalExpr)
        {
            return (element) =>
            {
                Assert.IsType<CommandElement>(element);
                var cmd = (CommandElement) element;
                Assert.Equal(name, cmd.CommandName);
                Assert.Equal(conditionalExpr, cmd.ConditionalExpression);
            };
        }
    }
}