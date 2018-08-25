using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolutionGen.Utils;
using Xunit;
using Path = System.IO.Path;
using GLOB = Glob.Glob;

namespace SolutionGen.Tests.Utils
{
    public class FileUtilTests
    {
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

        [Fact]
        public void GlobCanGetAllFilesWithExtensionRecursively()
        {
            HashSet<string> result =
                FileUtil.GetFiles(Directory.GetCurrentDirectory(),
                    new IPattern[] {new GlobPattern("**/*.module", false)}, null);
            
            Assert.NotEmpty(result);
            Assert.All(result, p => Assert.Equal(".module", Path.GetExtension(p)));
            Assert.All(result, p => Assert.True(File.Exists(p)));
        }
        
        [Fact]
        public void RegexCanGetAllFilesWithExtensionRecursively()
        {
            HashSet<string> result =
                FileUtil.GetFiles("./",
                    new IPattern[] {new RegexPattern(@"\.module$", false)}, null);
            
            Assert.NotEmpty(result);
            Assert.All(result, p => Assert.Equal(".module", Path.GetExtension(p)));
            Assert.All(result, p => Assert.True(File.Exists(p)));
        }

        [Fact]
        public void GlobCanExcludeFilesWithExtensionRecursively()
        {
            HashSet<string> result =
                FileUtil.GetFiles("./",
                    new IPattern[] {new GlobPattern("**/*", false)},
                    new IPattern[] {new GlobPattern("**/*.module", true)});
            
            Assert.NotEmpty(result);
            Assert.All(result, (p) => Assert.NotEqual(".module", Path.GetExtension(p)));
        }
        
        [Fact]
        public void RegexCanExcludeFilesWithExtensionRecursively()
        {
            HashSet<string> result =
                FileUtil.GetFiles("./",
                    new IPattern[] {new RegexPattern(".*", false)},
                    new IPattern[] {new RegexPattern(@"\.module",  false)});
            
            Assert.NotEmpty(result);
            Assert.All(result, (p) => Assert.NotEqual(".module", Path.GetExtension(p)));
        }

        [Fact]
        public void RegexMatchesReturnsCorrectPathNotCurrentDir()
        {
            HashSet<string> result =
                FileUtil.GetFiles("Libs",
                    new IPattern[] {new RegexPattern("Sprache.dll", false),},
                    new IPattern[] {new RegexPattern("/bin/", false),});
            
            Assert.NotEmpty(result);
            Assert.Equal("Sprache.dll", result.First());
        }
        
        [Fact]
        public void RegexMatchesReturnsCorrectPathCurrentDir()
        {
            HashSet<string> result =
                FileUtil.GetFiles("Libs",
                    new IPattern[] {new RegexPattern("Sprache.dll", false),},
                    new IPattern[] {new RegexPattern("/bin/", false),},
                    "./");
            
            Assert.NotEmpty(result);
            Assert.Equal("Libs/Sprache.dll", result.First());
        }

        [Fact]
        public void ExcludesHavePrecedenceOverIncludes()
        {
            HashSet<string> result =
                FileUtil.GetFiles("./",
                    new IPattern[] {new RegexPattern(".*", false)},
                    new IPattern[] {new RegexPattern(".*",  false)});
            
            Assert.Empty(result);
        }

        [Fact]
        public void LiteralMatchesFirstFileWhenAmbiguous()
        {
            HashSet<string> result =
                FileUtil.GetFiles("./", new IPattern[] {new LiteralPattern("Class.cs", false)}, null);

            Assert.Single(result);
            Assert.Equal("MyModule/Code/Class.cs", result.First());
            Assert.True(File.Exists(result.First()));
        }
        
        [Fact]
        public void LiteralCanExcludeFileRecursively()
        {
            HashSet<string> result =
                FileUtil.GetFiles("./",
                    new IPattern[] {new GlobPattern("**/Class.cs", false)},
                    new IPattern[] {new LiteralPattern("Class.cs", false)});
            
            Assert.Empty(result);
        }

        [Fact]
        public void LiteralMatchesFileWhenInMultipleSearchPaths()
        {
            HashSet<string> result =
                FileUtil.GetFiles(new []{"./", "Libs"}, new IPattern[] {new LiteralPattern("Sprache.dll", false)}, null);

            Assert.Single(result);
        }

        [Fact]
        public void GetFilesInMultipleSearchPathsOnlyReturnsFirst()
        {
            string[] searchPaths =
            {
                "MyModule",
                "MyOtherModule"
            };

            HashSet<string> result =
                FileUtil.GetFiles(searchPaths, new IPattern[] {new GlobPattern("**/Class.cs", false)}, null);
            
            Assert.Single(result);
        }
    }
}