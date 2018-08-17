using System.Collections.Generic;
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
                FileUtil.GetFiles("Resources", new IPattern[] {new GlobPattern("**/*.module", false)}, null);
            
            Assert.NotEmpty(result);
            Assert.All(result, (p) => Assert.Equal(".module", Path.GetExtension(p)));
        }
        
        [Fact]
        public void RegexCanGetAllFilesWithExtensionRecursively()
        {
            HashSet<string> result =
                FileUtil.GetFiles("Resources",
                    new IPattern[] {new RegexPattern(@"\.module$", false)}, null);
            
            Assert.NotEmpty(result);
            Assert.All(result, (p) => Assert.Equal(".module", Path.GetExtension(p)));
        }

        [Fact]
        public void GlobCanExcludeFilesWithExtensionRecursively()
        {
            HashSet<string> result =
                FileUtil.GetFiles("Resources",
                    new IPattern[] {new GlobPattern("**/*", false)},
                    new IPattern[] {new GlobPattern("**/*.module", true)});
            
            Assert.NotEmpty(result);
            Assert.All(result, (p) => Assert.NotEqual(".module", Path.GetExtension(p)));
        }
        
        [Fact]
        public void RegexCanExcludeFilesWithExtensionRecursively()
        {
            HashSet<string> result =
                FileUtil.GetFiles("Resources",
                    new IPattern[] {new RegexPattern(".*", false)},
                    new IPattern[] {new RegexPattern(@"\.module",  false)});
            
            Assert.NotEmpty(result);
            Assert.All(result, (p) => Assert.NotEqual(".module", Path.GetExtension(p)));
        }
        

        [Fact]
        public void ExcludesHavePrecedenceOverIncludes()
        {
            HashSet<string> result =
                FileUtil.GetFiles("Resources",
                    new IPattern[] {new RegexPattern(".*", false)},
                    new IPattern[] {new RegexPattern(".*",  false)});
            
            Assert.Empty(result);
        }

        [Fact]
        public void LiteralMatchesFirstFileWhenAmbiguous()
        {
            HashSet<string> result =
                FileUtil.GetFiles("Resources", new IPattern[] {new LiteralPattern("Class.cs", false)}, null);

            Assert.Single(result);
            Assert.Equal("MyModule/Code/Class.cs", result.First());
        }
        
        [Fact]
        public void LiteralCanExcludeFileRecursively()
        {
            HashSet<string> result =
                FileUtil.GetFiles("Resources",
                    new IPattern[] {new GlobPattern("**/Class.cs", false)},
                    new IPattern[] {new LiteralPattern("Class.cs", false)});
            
            Assert.Empty(result);
        }

        [Fact]
        public void CanGetFilesInMultipleSearchPaths()
        {
            string[] searchPaths =
            {
                "Resources/MyModule",
                "Resources/MyOtherModule"
            };

            HashSet<string> result =
                FileUtil.GetFiles(searchPaths, new IPattern[] {new GlobPattern("**/Class.cs", false)}, null);
            
            Assert.Equal(2, result.Count);
        }
    }
}