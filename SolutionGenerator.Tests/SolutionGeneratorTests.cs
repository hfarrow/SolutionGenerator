using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SolutionGen.Generator.Model;
using SolutionGen.Generator.Reader;
using Xunit;
using Module = SolutionGen.Generator.Model.Module;

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
            return generator.Reader.Solution.Settings.ConfigurationGroups[configurationGroup].Configurations.Values
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
            
            Assert.True(sol.Modules.ContainsKey("MyModule"));
            Assert.True(sol.Modules.ContainsKey("MyOtherModule"));
            Assert.True(sol.Templates.ContainsKey("MyTemplate"));

            Module module = sol.Modules["MyModule"];
            ICollection<Project> moduleProjects = GetIncludedProjects(configurationGroup, module);
            Assert.Equal("MyModule", module.Name);
            Assert.Equal(configurationGroup == "everything" ? 2 : 1, moduleProjects.Count);
            Assert.Equal("MyModule", moduleProjects.ElementAt(0).Name);
            if (configurationGroup == "everything")
            {
                Assert.Equal("MyModule.Tests", moduleProjects.ElementAt(1).Name);
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
        }

//        [Fact]
//        public void CanGenerateSolutionWithExternalDefineConstants()
//        {
//            DocumentReader sol = generator.reader;
//            generator.GenerateSolution("everything", "EXTERNAL");
//            Module module = sol.Modules["MyModule"];
//            Project firstProject = module.Projects.First();
//            Project.Configuration debug = firstProject.GetConfiguration("Debug");
//            Project.Configuration release = firstProject.GetConfiguration("Release");
//            Assert.Contains("EXTERNAL", debug.DefineConstants);
//            Assert.Contains("EXTERNAL", release.DefineConstants);
//        }
    }
}