using System.Collections.Generic;
using SolutionGenerator.Parsing.Model;
using Sprache;
using Xunit;

namespace SolutionGenerator.Parsing.Tests
{
    public class ParseConfigurationTests
    {
        [Fact]
        public void CanParseConfigurationWithMultiplePairValues()
        {
            const string input = "configuration myConfig\n" +
                                 "[\n" +
                                 "Name1: value1,value2\n" +
                                 "Name2: value1,value2\n" +
                                 "]";

            ConfigurationElement config = DocumentParser.Configuration.Parse(input);
            Assert.NotNull(config);
            Assert.Equal(2, config.Configurations.Count);
            Assert.Equal("myConfig", config.ConfigurationName);
            Assert.True(config.Configurations.ContainsKey("Name1"));
            Assert.True(config.Configurations.ContainsKey("Name2"));

            HashSet<string> name1 = config.Configurations["Name1"];
            Assert.True(name1.Contains("value1"));
            Assert.True(name1.Contains("value2"));
            
            HashSet<string> name2 = config.Configurations["Name2"];
            Assert.True(name2.Contains("value1"));
            Assert.True(name2.Contains("value2"));
        }
    }
}