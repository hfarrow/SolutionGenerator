using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolutionGen.Generator.Reader;
using SolutionGen.Parser;
using SolutionGen.Parser.Model;
using SolutionGen.Utils;
using Sprache;
using Xunit;
using GLOB = Glob.Glob;

namespace SolutionGen.Tests.Utils
{
    public class FileUtilTests
    {
        private static IPattern GetPattern(string input)
        {
            ValueElement patternValue = DocumentParser.Value.Parse(input);
            switch (patternValue)
            {
                case GlobValue glob: return new GlobPattern(glob.GlobStr, glob.Negated);
                case RegexValue regex: return new RegexPattern(regex.RegexPattern, regex.Negated);
                case ValueElement value: return PatternPropertyReader.MakeLiteralPattern(value.Value.ToString());
            }
            throw new InvalidOperationException("Input string was not a parsable pattern.");
        }
        
        [Fact]
        public void CanProcessAllPathTypes()
        {
            (HashSet<string> files, HashSet<GLOB> globs, HashSet<RegexPattern> regexes) result =
                FileUtil.ProcessFileValues(new IPattern[]
                {
                    new GlobPattern("**/*", false),
                    new RegexPattern(".*", false),
                    new LiteralPattern("./", false)
                });

            Assert.Single(result.files);
            Assert.Single(result.globs);
            Assert.Single(result.regexes);
        }

        [Theory]
        [InlineData("glob \"**/*.*\"")]
        [InlineData("regex \".*\"")]
        [InlineData("Class.cs")]
        public void AbsoluteBaseDirReturnsAbsoluteResults(string input)
        {
            IPattern pattern = GetPattern(input);
            string currentDir = new DirectoryInfo(Directory.GetCurrentDirectory()).FullName;

            HashSet<string> result = FileUtil.GetFiles("./",
                new[] {pattern},
                null,
                currentDir);
            
            Assert.True(result.Count > 0);
            Assert.All(result, f =>
            {
                Assert.True(Path.IsPathRooted(f));
                Assert.True(File.Exists(f));
            });
            
            // sub dir for search path and current dir for base path
            result = FileUtil.GetFiles("MyModule",
                new[] {pattern},
                null,
                currentDir);
            
            Assert.True(result.Count > 0);
            Assert.All(result, f =>
            {
                Assert.True(Path.IsPathRooted(f));
                Assert.True(File.Exists(f));
            });
            
            // sub dir for search path and sub dir for base path
            result = FileUtil.GetFiles("MyModule",
                new[] {pattern},
                null,
                Path.Combine(currentDir, "MyModule"));
            
            Assert.True(result.Count > 0);
            Assert.All(result, f =>
            {
                Assert.True(Path.IsPathRooted(f));
                Assert.True(File.Exists(f));
            });
        }

        [Theory]
        [InlineData("glob \"**/*.*\"")]
        [InlineData("regex \".*\"")]
        [InlineData("Class.cs")]
        public void RelativeBaseDirReturnsRelativeResults(string input)
        {
            TestRelativeResults(input, "./");
        }

        [Theory]
        [InlineData("glob \"**/*.*\"")]
        [InlineData("regex \".*\"")]
        [InlineData("Class.cs")]
        public void DefaultBaseDirReturnsRelativeResultsToCurrentDir(string input)
        {
            TestRelativeResults(input, null);
        }

        private void TestRelativeResults(string input, string currentDir)
        {
            IPattern pattern = GetPattern(input);

            // current dir for both search path and base path
            HashSet<string> result = FileUtil.GetFiles("./",
                new[] {pattern},
                null,
                currentDir);
            
            Assert.True(result.Count > 0);
            Assert.All(result, f =>
            {
                Assert.False(Path.IsPathRooted(f));
                Assert.True(File.Exists(f));
            });
            
            // sub dir for search path and current dir for base path
            result = FileUtil.GetFiles("MyModule",
                new[] {pattern},
                null,
                currentDir);
            
            Assert.True(result.Count > 0);
            Assert.All(result, f =>
            {
                Assert.False(Path.IsPathRooted(f));
                Assert.True(File.Exists(f));
            });
            
            // sub dir for search path and sub dir for base path
            result = FileUtil.GetFiles("MyModule",
                new[] {pattern},
                null,
                "MyModule");
            
            Assert.True(result.Count > 0);
            Assert.All(result, f =>
            {
                Assert.False(Path.IsPathRooted(f));
                Assert.True(File.Exists(Path.Combine("MyModule", f)));
            });
            
        }

        [Fact]
        public void GlobReturnsManyFiles()
        {
            HashSet<string> results = FileUtil.GetFiles("./",
                new[]
                {
                    new GlobPattern("**/*.*", false),
                },
                null);
            
            Assert.True(results.Count > 1);
        }

        [Fact]
        public void RegexReturnsManyFiles()
        {
            HashSet<string> results = FileUtil.GetFiles("./",
                new[]
                {
                    new RegexPattern(".*", false),
                },
                null);
            
            Assert.True(results.Count > 1);
        }

        [Fact]
        public void LiteralReturnsSingleFile()
        {
            HashSet<string> results = FileUtil.GetFiles("./",
                new[]
                {
                    new LiteralPattern("Class.cs", false), 
                },
                null);

            Assert.Single(results);
        }

        [Fact]
        public void LiteralMatchesFirstWhenManyMatches()
        {
            HashSet<string> result =
                FileUtil.GetFiles("./",
                    new IPattern[] {new LiteralPattern("Class.cs", false)},
                    null);

            Assert.Equal("MyModule/Code/Class.cs", result.First());
        }
        
        [Theory]
        [InlineData("glob \"**/*.*\"")]
        [InlineData("regex \".*\"")]
        [InlineData("Class.cs")]
        public void SearchPathIsRecursivelySearched(string input)
        {
            IPattern pattern = GetPattern(input);
            
            HashSet<string> result =
                FileUtil.GetFiles("./",
                    new[] {pattern},
                    null);
            
            Assert.True(result.Count > 0);
        }
        
        [Theory]
        [InlineData("glob \"**/Class.cs\"")]
        [InlineData("regex \"Class.cs\"")]
        public void MultipleSearchPathsOnlyReturnsFromFirst(string input)
        {
            IPattern pattern = GetPattern(input);
            
            string[] searchPaths =
            {
                "MyModule",
                "MyOtherModule"
            };

            HashSet<string> result =
                FileUtil.GetFiles(searchPaths,
                    new[] {pattern},
                    null);

            Assert.Single(result);
            Assert.DoesNotContain("MyOtherModule", result.First());
        }
        
        [Theory]
        [InlineData("glob \"**/*.*\"")]
        [InlineData("regex \".*\"")]
        [InlineData("Class.cs")]
        public void ExcludesHavePrecedenceOverIncludes(string input)
        {
            IPattern pattern = GetPattern(input);
            
            HashSet<string> result =
                FileUtil.GetFiles("./",
                    new[] {pattern},
                    new[] {pattern});
            
            Assert.Empty(result);
        }       
    }
}