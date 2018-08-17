using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SolutionGen.Generator.Model;
using SolutionGen.Generator.Reader;
using Xunit;
using Module = SolutionGen.Generator.Model.Module;
using Path = System.IO.Path;

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
        }

        private Configuration GetFirstActiveConfiguration(string configurationGroup)
        {
            return generator.Reader.Solution.ConfigurationGroups[configurationGroup].Configurations.Values
                .First();
        }

        private ICollection<Project> GetIncludedProjects(string configurationGroup)
        {
            return generator.Reader.Modules.Values.SelectMany(m =>
                m.Configurations[GetFirstActiveConfiguration(configurationGroup)].Projects.Values)
                .ToArray();
        }
        
        private ICollection<Project> GetIncludedProjects(string configurationGroup, Module module)
        {
            return GetIncludedProjects(configurationGroup)
                .Where(p => module.ProjectIdLookup.ContainsKey(p.Name))
                .ToArray();
        }
        
        [Fact]
        public void CanLoadSolutionConfig()
        {
            Assert.NotNull(generator);
        }

        [Theory]
        [InlineData("everything")]
        [InlineData("no-tests")]
        public void CanGeneratorSolution(string configurationGroup)
        {
            DocumentReader sol = generator.Reader;
            generator.GenerateSolution(configurationGroup);
            Assert.Single(sol.Templates);
            Assert.Equal(2, sol.Modules.Count);
            Assert.NotNull(sol.Solution);
            
            Assert.True(sol.Solution.CanIncludeProject("MyModule"));
            Assert.True(sol.Solution.CanIncludeProject("MyOtherModule"));
            Assert.True(sol.Solution.CanIncludeProject("MyModule.Tests"));
            Assert.True(sol.Solution.CanIncludeProject("MyOtherModule.Tests"));
            
            Assert.True(sol.Modules.ContainsKey("MyModule"));
            Assert.True(sol.Modules.ContainsKey("MyOtherModule"));
            Assert.True(sol.Templates.ContainsKey("MyTemplate"));

            Module module = sol.Modules["MyModule"];
            ICollection<Project> moduleProjects = GetIncludedProjects(configurationGroup, module);
            Assert.Equal("MyModule", module.Name);
            Assert.Equal(configurationGroup == "everything" ? 2 : 1, moduleProjects.Count);
            Project mainProject = moduleProjects.ElementAt(0);
            Assert.Equal("MyModule", mainProject.Name);
            Assert.Equal("8CA3145D-B47F-4355-808A-2C08F48EB061", mainProject.Guid.ToString().ToUpper());
            if (configurationGroup == "everything")
            {
                Project testsProject = moduleProjects.ElementAt(1);
                Assert.Equal("MyModule.Tests", testsProject.Name);
                Assert.Equal("3B8C3242-A267-4E63-9AE2-7099ACB9F730", testsProject.Guid.ToString().ToUpper());
            }

            Assert.Equal(1,
                module.Configurations.Keys.Count(cfg => cfg.Name == "Debug" && cfg.GroupName == configurationGroup));

            Assert.Equal(1,
                module.Configurations.Keys.Count(cfg => cfg.Name == "Release" && cfg.GroupName == configurationGroup));
            
            Configuration debug = module.Configurations.Keys.First(cfg => cfg.Name == "Debug");
            Configuration release = module.Configurations.Keys.First(cfg => cfg.Name == "Release");
            Project firstDebugProject = module.Configurations[debug].Projects.Values.First();
            Project firstReleaseProject = module.Configurations[release].Projects.Values.First();
            var debugDefines = firstDebugProject.Settings.GetProperty<HashSet<string>>(Settings.PROP_DEFINE_CONSTANTS);
            var releaseDefines = firstReleaseProject.Settings.GetProperty<HashSet<string>>(Settings.PROP_DEFINE_CONSTANTS);
            Assert.Contains("DEBUG", debugDefines);
            Assert.Contains("RELEASE", releaseDefines);
            Assert.DoesNotContain("DEBUG", releaseDefines);

            if (configurationGroup == "everything")
            {
                Assert.Contains("TEST", debugDefines);
                Assert.DoesNotContain("TEST", releaseDefines);
            }
            
            Assert.Contains("Sprache.dll", firstDebugProject.LibRefs.First());
            Assert.Contains("Sprache.dll", firstReleaseProject.LibRefs.First());
        }

        [Fact]
        public void CanGenerateSolutionWithExternalDefineConstants()
        {
            DocumentReader sol = generator.Reader;
            const string constantName = "MY_EXTERNAL_DEFINE_CONSTANT";
            generator.GenerateSolution("everything", constantName);

            string projectPath =
                Path.Combine(sol.SolutionConfigDir, "Resources", "MyModule", "MyModule") + ".csproj";
            Assert.Contains(constantName, File.ReadAllText(projectPath));
        }

        [Fact]
        public void CanGenerateAndBuildSolution()
        {
            
        }

        [Fact]
        public void CanGenerateSolutionWithIncludeProjectProperty()
        {
            
        }
    }
}