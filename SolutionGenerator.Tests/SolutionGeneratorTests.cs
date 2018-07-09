using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace SolutionGen.Tests
{
    // ReSharper disable once ClassNeverInstantiated.Global
        // Created by XUnit at runtime
    public class SolutionGeneratorFixture : IDisposable
    {
        public readonly string ConfigText;
        
        public SolutionGeneratorFixture()
        {
            Assembly assembly = typeof(SolutionGeneratorFixture).GetTypeInfo().Assembly;

            using (Stream stream =
                assembly.GetManifestResourceStream("SolutionGenerator.Tests.Resources.TestSolution.txt"))
            {
                using (var reader = new StreamReader(stream))
                {
                    ConfigText = reader.ReadToEnd();
                }
            }
        }
        public void Dispose()
        {
        }
    }
    
    public class SolutionGeneratorTests : IClassFixture<SolutionGeneratorFixture>
    {
        private readonly SolutionGenerator generator;

        public SolutionGeneratorTests(SolutionGeneratorFixture fixture)
        {
            generator = SolutionGenerator.FromText(fixture.ConfigText, Directory.GetCurrentDirectory());
            
            // Temp (Make the individual tests call this
            generator.GenerateSolution("everything");
        }
        
        [Fact]
        public void CanLoadSolutionConfig()
        {
            Assert.NotNull(generator);
        }

//        [Theory]
//        [InlineData("everything")]
//        [InlineData("no-tests")]
//        public void CanGeneratorSolution(string configurationGroup)
//        {
//            DocumentReader sol = generator.reader;
//            generator.GenerateSolution(configurationGroup);
//            Assert.Single(sol.Templates);
//            Assert.Equal(2, sol.Modules.Count);
//            Assert.NotNull(sol.Solution);
//            
//            Assert.True(sol.Modules.ContainsKey("TestModule"));
//            Assert.True(sol.Modules.ContainsKey("OtherTestModule"));
//            Assert.True(sol.Templates.ContainsKey("TestTemplate"));
//
//            Module module = sol.Modules["TestModule"];
//            Assert.Equal("TestModule", module.ModuleElement.Heading.Name);
//            Assert.Equal(configurationGroup == "everything" ? 2 : 1, module.Projects.Count);
//            Assert.Equal("TestModule", module.Projects.ElementAt(0).Name);
//            if (configurationGroup == "everything")
//            {
//                Assert.Equal("TestModule.Tests", module.Projects.ElementAt(1).Name);
//            }
//
//            Project firstProject = module.Projects.First();
//            Assert.True(firstProject.HasConfiguration("Debug"));
//            Assert.True(firstProject.HasConfiguration("Release"));
//            Project.Configuration debug = firstProject.GetConfiguration("Debug");
//            Project.Configuration release = firstProject.GetConfiguration("Release");
//            Assert.Contains("DEBUG", debug.DefineConstants);
//            Assert.Contains("RELEASE", release.DefineConstants);
//            Assert.DoesNotContain("DEBUG", release.DefineConstants);
//
//            if (configurationGroup == "everything")
//            {
//                Assert.Contains("TEST", debug.DefineConstants);
//                Assert.DoesNotContain("TEST", release.DefineConstants);
//            }
//
//            Assert.True(sol.Solution.ConfigurationGroups.ContainsKey("everything"));
//        }
//
//        [Fact]
//        public void CanGenerateSolutionWithExternalDefineConstants()
//        {
//            DocumentReader sol = generator.reader;
//            generator.GenerateSolution("everything", "EXTERNAL");
//            Module module = sol.Modules["TestModule"];
//            Project firstProject = module.Projects.First();
//            Project.Configuration debug = firstProject.GetConfiguration("Debug");
//            Project.Configuration release = firstProject.GetConfiguration("Release");
//            Assert.Contains("EXTERNAL", debug.DefineConstants);
//            Assert.Contains("EXTERNAL", release.DefineConstants);
//        }
    }
}